#include "krpc/stream_manager.hpp"

#include <condition_variable>  // NOLINT(build/c++11)
#include <cstddef>
#include <exception>
#include <functional>
#include <string>
#include <utility>

#include "krpc/client.hpp"
#include "krpc/connection.hpp"
#include "krpc/decoder.hpp"
#include "krpc/error.hpp"
#include "krpc/krpc.pb.hpp"
#include "krpc/services/krpc.hpp"
#include "krpc/stream_impl.hpp"

namespace krpc {

StreamManager::StreamManager(Client * client, const std::shared_ptr<Connection>& connection)
  : client(client), connection(connection),
    update_lock(new std::recursive_mutex),
    stop(new std::atomic_bool(false)),
    should_freeze(new std::atomic_bool(false)),
    frozen(new std::atomic_bool(false)),
    update_thread(new std::thread(update_thread_main, this, connection,
                                  stop, should_freeze, frozen)),
    condition_lock(condition_mutex, std::defer_lock),
    next_callback_tag(0) {
}

StreamManager::~StreamManager() {
  stop->store(true);
  update_thread->join();
  update_lock.reset();
}

std::shared_ptr<StreamImpl> StreamManager::add_stream(const schema::ProcedureCall& call) {
  schema::Stream stream = services::KRPC(client).add_stream(call, false);
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  auto it = streams.find(stream.id());
  if (it != streams.end())
    if (auto stream = it->second.lock())
      return stream;
  auto stream_impl = std::make_shared<StreamImpl>(client, stream.id(), update_lock.get());
  streams[stream.id()] = stream_impl;
  return stream_impl;
}

std::shared_ptr<StreamImpl> StreamManager::get_stream(google::protobuf::uint64 id) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  auto it = streams.find(id);
  if (it != streams.end())
    if (auto stream = it->second.lock())
      return stream;
  auto stream_impl = std::make_shared<StreamImpl>(client, id, update_lock.get());
  streams[id] = stream_impl;
  return stream_impl;
}

void StreamManager::remove_stream(google::protobuf::uint64 id) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  if (streams.find(id) == streams.end())
    return;
  services::KRPC(client).remove_stream(id);
  streams.erase(id);
}

void StreamManager::update(google::protobuf::uint64 id, const schema::ProcedureResult& result) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  auto it = streams.find(id);
  if (it == streams.end())
    return;
  auto stream = it->second.lock();
  if (!stream)
    return;
  if (!result.has_error()) {
    stream->update(result.value(), nullptr);
  } else {
    try {
      client->throw_exception(result.error());
    } catch (...) {
      stream->update("", std::current_exception());
    }
  }
  stream->get_condition().notify_all();
  for (auto callback : stream->get_callbacks())
    callback.second(stream->get_data());
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

std::condition_variable& StreamManager::get_update_condition() {
  return condition;
}

std::unique_lock<std::mutex>& StreamManager::get_update_condition_lock() {
  return condition_lock;
}

int StreamManager::add_update_callback(const Callback& callback) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  auto tag = next_callback_tag;
  next_callback_tag++;
  callbacks[tag] = callback;
  return tag;
}

void StreamManager::remove_update_callback(int tag) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  callbacks.erase(tag);
}

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
    stream_manager->condition.notify_all();
    for (auto callback : stream_manager->callbacks)
      callback.second();
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

}  // namespace krpc
