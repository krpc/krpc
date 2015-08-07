#include "krpc/krpc.hpp"
#include "krpc/encoder.hpp"
#include <iostream>

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
    rpc_connection->send(RPC_HELLO_MESSAGE, sizeof(RPC_HELLO_MESSAGE));
    rpc_connection->send(Encoder::client_name(name));
    std::vector<char> client_identifier = rpc_connection->receive(GUID_LENGTH);

    // Connect to Stream server
    boost::shared_ptr<Connection> stream_connection;
    if (stream_port != 0) {
      stream_connection = boost::shared_ptr<Connection>(new Connection(address, stream_port));
      stream_connection->connect();
      stream_connection->send(STREAM_HELLO_MESSAGE, sizeof(STREAM_HELLO_MESSAGE));
      stream_connection->send(client_identifier);
      std::vector<char> ok_message = stream_connection->receive(sizeof(OK_MESSAGE));
      std::vector<char> expected(OK_MESSAGE, OK_MESSAGE+sizeof(OK_MESSAGE));
      if (!std::equal(ok_message.begin(), ok_message.end(), expected.begin())) {
        BOOST_THROW_EXCEPTION(
          ConnectionFailed()
          << error_description("Did not receive OK message from server"));
      }
    }

    return Client(rpc_connection, stream_connection);
  }

}
