#include "krpc/krpc.hpp"
#include "krpc/encoder.hpp"
#include "krpc/decoder.hpp"

namespace krpc {

  Client connect(const std::string& name, const std::string& address,
                 unsigned int rpc_port, unsigned int stream_port) {
    if (rpc_port == stream_port) {
      BOOST_THROW_EXCEPTION(
        ConnectionFailed()
        << error_description("RPC and Stream port numbers are the same"));
    }

    // Connect to RPC server
    boost::shared_ptr<Connection> rpc_connection(new Connection(address, rpc_port));
    rpc_connection->connect();
    rpc_connection->send(Encoder::RPC_HELLO_MESSAGE, Encoder::RPC_HELLO_MESSAGE_LENGTH);
    rpc_connection->send(Encoder::client_name(name));
    std::string client_identifier = rpc_connection->receive(Decoder::GUID_LENGTH);

    // Connect to Stream server
    boost::shared_ptr<Connection> stream_connection;
    if (stream_port != 0) {
      stream_connection = boost::shared_ptr<Connection>(new Connection(address, stream_port));
      stream_connection->connect();
      stream_connection->send(Encoder::STREAM_HELLO_MESSAGE, Encoder::STREAM_HELLO_MESSAGE_LENGTH);
      stream_connection->send(client_identifier);
      std::string ok_message = stream_connection->receive(Decoder::OK_MESSAGE_LENGTH);
      std::string expected(Decoder::OK_MESSAGE);
      if (!std::equal(ok_message.begin(), ok_message.end(), expected.begin())) {
        BOOST_THROW_EXCEPTION(
          ConnectionFailed()
          << error_description("Did not receive OK message from server"));
      }
    }

    return Client(rpc_connection, stream_connection);
  }

}
