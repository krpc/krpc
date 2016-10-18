#include "krpc/client.hpp"

#include <google/protobuf/io/coded_stream.h>

#include <string>
#include <vector>

#include "krpc/encoder.hpp"
#include "krpc/decoder.hpp"
#include "krpc/error.hpp"

namespace krpc {

Client::Client(): lock(new std::mutex) {}

Client::Client(const std::shared_ptr<Connection>& rpc_connection,
               const std::shared_ptr<Connection>& stream_connection):
  rpc_connection(rpc_connection),
  stream_manager(this, stream_connection),
  lock(new std::mutex) {}

schema::Request Client::request(const std::string& service,
                                const std::string& procedure,
                                const std::vector<std::string>& args) {
  schema::Request request;
  request.set_service(service);
  request.set_procedure(procedure);
  for (unsigned int i = 0; i < args.size(); i++) {
    schema::Argument* arg = request.add_arguments();
    arg->set_position(i);
    arg->set_value(args[i]);
  }
  return request;
}

std::string Client::invoke(const schema::Request& request) {
  std::string data;
  {
    std::lock_guard<std::mutex> lock_guard(*lock);

    rpc_connection->send(encoder::encode_delimited(request));

    size_t size = 0;
    while (true) {
      try {
        data += rpc_connection->receive(1);
        size = decoder::decode_size_and_position(data).first;
        break;
      } catch (decoder::DecodeFailed&) {
      }
    }

    data = rpc_connection->receive(size);
  }

  schema::Response response;
  decoder::decode(response, data, this);

  if (response.has_error())
    throw RPCError(response.error());

  return response.return_value();
}

std::string Client::invoke(const std::string& service,
                           const std::string& procedure,
                           const std::vector<std::string>& args) {
  return this->invoke (this->request(service, procedure, args));
}

google::protobuf::uint64 Client::add_stream(const schema::Request& request) {
  return stream_manager.add_stream(request);
}

void Client::remove_stream(google::protobuf::uint64 id) {
  stream_manager.remove_stream(id);
}

std::string Client::get_stream(google::protobuf::uint64 id) {
  return stream_manager.get(id);
}

void Client::freeze_streams() {
  stream_manager.freeze();
}

void Client::thaw_streams() {
  stream_manager.thaw();
}

}  // namespace krpc
