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
  rpc_connection->send(encoder::client_name(name));
  std::string client_identifier = rpc_connection->receive(decoder::GUID_LENGTH);

  // Connect to Stream server
  std::shared_ptr<Connection> stream_connection;
  if (stream_port != 0) {
    stream_connection = std::shared_ptr<Connection>(new Connection(address, stream_port));
    stream_connection->connect(10, 0.1f);
    stream_connection->send(encoder::STREAM_HELLO_MESSAGE, encoder::STREAM_HELLO_MESSAGE_LENGTH);
    stream_connection->send(client_identifier);
    std::string ok_message = stream_connection->receive(decoder::OK_MESSAGE_LENGTH);
    std::string expected(decoder::OK_MESSAGE);
    if (!std::equal(ok_message.begin(), ok_message.end(), expected.begin()))
      throw ConnectionFailed("Did not receive OK message from server");
  }

  return Client(rpc_connection, stream_connection);
}

}  // namespace krpc
