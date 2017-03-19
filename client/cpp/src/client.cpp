#include "krpc/client.hpp"

#include <google/protobuf/io/coded_stream.h>

#include <string>
#include <vector>

#include "krpc/decoder.hpp"
#include "krpc/encoder.hpp"
#include "krpc/error.hpp"

namespace krpc {

Client::Client(): lock(new std::mutex) {}

Client::Client(const std::string& name, const std::string& address,
               unsigned int rpc_port, unsigned int stream_port) :
  lock(new std::mutex) {
  if (rpc_port == stream_port)
    throw ConnectionFailed("RPC and stream port numbers are the same");

  // Connect to RPC server
  rpc_connection = std::make_shared<Connection>(address, rpc_port);
  rpc_connection->connect(10, 0.1f);
  rpc_connection->send(encoder::RPC_HELLO_MESSAGE, encoder::RPC_HELLO_MESSAGE_LENGTH);
  rpc_connection->send(encoder::client_name(name));
  std::string client_identifier = rpc_connection->receive(decoder::GUID_LENGTH);

  // Connect to Stream server
  if (stream_port != 0) {
    auto stream_connection = std::make_shared<Connection>(address, stream_port);
    stream_connection->connect(10, 0.1f);
    stream_connection->send(encoder::STREAM_HELLO_MESSAGE, encoder::STREAM_HELLO_MESSAGE_LENGTH);
    stream_connection->send(client_identifier);
    std::string ok_message = stream_connection->receive(decoder::OK_MESSAGE_LENGTH);
    std::string expected(decoder::OK_MESSAGE);
    if (!std::equal(ok_message.begin(), ok_message.end(), expected.begin()))
      throw ConnectionFailed("Did not receive OK message from server");
    stream_manager = std::make_shared<StreamManager>(this, stream_connection);
  }
}

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
  return stream_manager->add_stream(request);
}

void Client::remove_stream(google::protobuf::uint64 id) {
  stream_manager->remove_stream(id);
}

std::string Client::get_stream(google::protobuf::uint64 id) {
  return stream_manager->get(id);
}

void Client::freeze_streams() {
  stream_manager->freeze();
}

void Client::thaw_streams() {
  stream_manager->thaw();
}

}  // namespace krpc
