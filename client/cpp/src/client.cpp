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

std::string Client::invoke(const schema::Request& request) {
  std::string data;
  {
    std::lock_guard<std::mutex> lock_guard(*lock);
    rpc_connection->send(encoder::encode_message_with_size(request));
    data = rpc_connection->receive_message();
  }

  schema::Response response;
  decoder::decode(response, data, this);

  if (!response.error().empty())
    throw RPCError(response.error());

  return response.return_value();
}

std::string Client::invoke(const std::string& service,
                           const std::string& procedure,
                           const std::vector<std::string>& args) {
  return this->invoke(this->build_request(service, procedure, args));
}

schema::Request Client::build_request(const std::string& service,
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

google::protobuf::uint64 Client::add_stream(const schema::Request& request) {
  return stream_manager.add_stream(request);
}

void Client::remove_stream(google::protobuf::uint64 id) {
  stream_manager.remove_stream(id);
}

std::string Client::get_stream(google::protobuf::uint64 id) {
  return stream_manager.get(id);
}

}  // namespace krpc
