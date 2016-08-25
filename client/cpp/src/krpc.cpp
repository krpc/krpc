#include "krpc.hpp"

#include <string>

#include "krpc/encoder.hpp"
#include "krpc/decoder.hpp"

namespace krpc {

Client connect(const std::string& name, const std::string& address,
               unsigned int rpc_port, unsigned int stream_port) {
  if (rpc_port == stream_port)
    throw ConnectionFailed("RPC and Stream port numbers are the same");

  // Connect to RPC server
  std::shared_ptr<Connection> rpc_connection(new Connection(address, rpc_port));
  rpc_connection->connect(10, 0.1f);
  rpc_connection->send(encoder::RPC_HELLO_MESSAGE, encoder::RPC_HELLO_MESSAGE_LENGTH);
  schema::ConnectionRequest request;
  request.set_client_name(name);
  rpc_connection->send(encoder::encode_delimited(request));
  schema::ConnectionResponse response;
  decoder::decode(response, rpc_connection->receive_message(), nullptr);

  // Connect to Stream server
  std::shared_ptr<Connection> stream_connection;
  if (stream_port != 0) {
    stream_connection = std::shared_ptr<Connection>(new Connection(address, stream_port));
    stream_connection->connect(10, 0.1f);
    stream_connection->send(encoder::STREAM_HELLO_MESSAGE, encoder::STREAM_HELLO_MESSAGE_LENGTH);
    schema::ConnectionRequest request;
    request.set_client_identifier(response.client_identifier());
    stream_connection->send(encoder::encode_delimited(request));
    schema::ConnectionResponse response;
    decoder::decode(response, stream_connection->receive_message(), nullptr);
    if (response.status() != schema::ConnectionResponse::OK)
      throw ConnectionFailed(response.message());
  }

  return Client(rpc_connection, stream_connection);
}

}  // namespace krpc
