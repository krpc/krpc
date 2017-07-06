#include "krpc/stream_manager.hpp"

#include <string>
#include <vector>

#include "krpc/decoder.hpp"
#include "krpc/encoder.hpp"
#include "krpc/error.hpp"
#include "krpc/services/krpc.hpp"

namespace krpc {

void StreamManager::update_thread_main(StreamManager* stream_manager,
                                       const std::shared_ptr<Connection>& connection,
                                       const std::shared_ptr<std::atomic_bool>& stop,
                                       const std::shared_ptr<std::atomic_bool>& should_freeze,
                                       const std::shared_ptr<std::atomic_bool>& frozen) {
  Client * client = stream_manager->client;

  auto apply_update = [stream_manager, client] (const std::string& data) {
    schema::StreamUpdate update;
    decoder::decode(update, data, client);
    for (auto result : update.results())
      stream_manager->update(result.id(), result.result());
  };

  while (!stop->load()) {
    // Wait for next update message
    size_t size = 0;
    std::string data;
    while (!stop->load()) {
      try {
        data += connection->partial_receive(1);
        size = decoder::decode_size(data);
        break;
      } catch (EncodingError&) {
      }
    }
    if (stop->load())
      break;

    // Decode and apply the update
    apply_update(connection->receive(size));

    // Check if updates should freeze
    if (should_freeze->load()) {
      frozen->store(true);

      // While frozen, read and skip update messages
      std::string last_update;
      while (should_freeze->load()) {
        size_t size = 0;
        std::string data;
        while (data.size() > 0 || should_freeze->load()) {
          try {
            data += connection->partial_receive(1);
            size = decoder::decode_size(data);
            break;
          } catch (EncodingError&) {
          }
          // Stop if requested
          if (stop->load())
            return;
        }
        if (size > 0)
          last_update = connection->receive(size);
      }

      // Apply the last received update when thawing
      if (!last_update.empty())
        apply_update(last_update);

      frozen->store(false);
    }
  }
}

StreamManager::StreamManager(Client * client, const std::shared_ptr<Connection>& connection)
  : client(client), connection(connection),
    data_lock(new std::mutex),
    stop(new std::atomic_bool(false)),
    should_freeze(new std::atomic_bool(false)),
    frozen(new std::atomic_bool(false)),
    update_thread(new std::thread(update_thread_main, this, connection,
                                  stop, should_freeze, frozen)) {
}

StreamManager::~StreamManager() {
  stop->store(true);
  update_thread->join();
}

google::protobuf::uint64 StreamManager::add_stream(const schema::ProcedureCall& call) {
  std::lock_guard<std::mutex> guard(*data_lock);
  schema::Stream stream = services::KRPC(client).add_stream(call);
  std::string value;
  std::exception_ptr error;
  try {
    value = client->invoke(call);
  } catch (...) {
    error = std::current_exception();
  }
  data[stream.id()] = std::make_pair(value, error);
  return stream.id();
}

void StreamManager::remove_stream(google::protobuf::uint64 id) {
  std::lock_guard<std::mutex> guard(*data_lock);
  services::KRPC(client).remove_stream(id);
  data.erase(id);
}

std::string StreamManager::get(google::protobuf::uint64 id) {
  std::lock_guard<std::mutex> guard(*data_lock);
  auto it = data.find(id);
  if (it == data.end())
    throw StreamError("Stream does not exist or was removed");
  if (it->second.second)
    std::rethrow_exception(it->second.second);
  return it->second.first;
}

void StreamManager::update(google::protobuf::uint64 id, const schema::ProcedureResult& result) {
  std::lock_guard<std::mutex> guard(*data_lock);
  auto it = data.find(id);
  if (it == data.end())
    return;
  if (!result.has_error()) {
    it->second.first = result.value();
  } else {
    it->second.first = "";
    try {
      client->throw_exception(result.error());
    } catch (...) {
      it->second.second = std::current_exception();
    }
  }
}

void StreamManager::freeze() {
  should_freeze->store(true);
  while (!frozen->load()) {
  }
}

void StreamManager::thaw() {
  should_freeze->store(false);
  while (frozen->load()) {
  }
}

}  // namespace krpc
