#include "krpc.hpp"

#include <string>

#include "krpc/decoder.hpp"
#include "krpc/encoder.hpp"

namespace krpc {

Client connect(const std::string& name, const std::string& address,
               unsigned int rpc_port, unsigned int stream_port) {
  // Connect to RPC server
  std::shared_ptr<Connection> rpc_connection(new Connection(address, rpc_port));
  rpc_connection->connect();
  schema::ConnectionRequest request;
  request.set_type(schema::ConnectionRequest::RPC);
  request.set_client_name(name);
  rpc_connection->send(encoder::encode_message_with_size(request));
  schema::ConnectionResponse response;
  decoder::decode(response, rpc_connection->receive_message(), nullptr);
  if (response.status() != schema::ConnectionResponse::OK)
    throw ConnectionError(response.message());

  // Connect to Stream server
  std::shared_ptr<Connection> stream_connection;
  if (stream_port != 0) {
    stream_connection = std::shared_ptr<Connection>(new Connection(address, stream_port));
    stream_connection->connect();
    schema::ConnectionRequest request;
    request.set_type(schema::ConnectionRequest::STREAM);
    request.set_client_identifier(response.client_identifier());
    stream_connection->send(encoder::encode_message_with_size(request));
    schema::ConnectionResponse response;
    decoder::decode(response, stream_connection->receive_message(), nullptr);
    if (response.status() != schema::ConnectionResponse::OK)
      throw ConnectionError(response.message());
  }

  return Client(rpc_connection, stream_connection);
}

}  // namespace krpc
