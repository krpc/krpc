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
                                       const std::shared_ptr<std::atomic_bool>& stop) {
  Client * client = stream_manager->client;

  while (!stop->load()) {
    size_t size = 0;
    std::string data;
    while (!stop->load()) {
      try {
        data += connection->partial_receive(1);
        size = decoder::decode_size_and_position(data).first;
        break;
      } catch (decoder::DecodeFailed&) {
      }
    }
    if (stop->load())
      break;

    data = connection->receive(size);
    schema::StreamMessage message;
    decoder::decode(message, data, client);

    for (auto response : message.responses())
      stream_manager->update(response.id(), response.response());
  }
}

StreamManager::StreamManager() {}

StreamManager::StreamManager(Client * client, const std::shared_ptr<Connection>& connection)
  : client(client), connection(connection),
    data_lock(new std::mutex),
    stop(new std::atomic_bool(false)),
    update_thread(new std::thread(update_thread_main, this, connection, stop)) {
}

StreamManager::~StreamManager() {
  stop->store(true);
  update_thread->join();
}

google::protobuf::uint64 StreamManager::add_stream(const schema::Request& request) {
  std::lock_guard<std::mutex> guard(*data_lock);
  schema::Stream stream = services::KRPC(client).add_stream(request);
  data[stream.id()] = client->invoke(request);
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
  return it->second;
}

void StreamManager::update(google::protobuf::uint64 id, const schema::Response& response) {
  std::lock_guard<std::mutex> guard(*data_lock);
  auto it = data.find(id);
  if (it == data.end())
    return;
  it->second = response.return_value();
}

}  // namespace krpc
