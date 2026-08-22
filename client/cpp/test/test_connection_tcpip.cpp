#include <asio/ip/address.hpp>
#include <asio/ip/tcp.hpp>
#include <memory>

#include "connection_test.hpp"
#include "gtest/gtest.h"
#include "krpc/connection.hpp"
#include "krpc/error.hpp"

namespace {

class tcpip_transport {
 public:
  void start() {
    // Port zero, so the system picks one nothing else is using. Bound to the loopback address
    // rather than to every interface, both because nothing outside this machine has any business
    // reaching it and because the address a server is bound to is the one it is reached on when
    // it is stopped; connecting to the address that stands for every interface is an error on
    // Windows, which would leave the server waiting to be woken.
    server.reset(new echo_server<asio::ip::tcp>(
        asio::ip::tcp::endpoint(asio::ip::make_address("127.0.0.1"), 0)));
  }

  void stop() { server.reset(); }

  std::shared_ptr<krpc::Connection> connect() {
    auto connection = std::make_shared<krpc::Connection>("127.0.0.1", server->endpoint().port());
    connection->connect();
    return connection;
  }

  std::unique_ptr<echo_server<asio::ip::tcp>> server;
};

}  // namespace

INSTANTIATE_TYPED_TEST_SUITE_P(tcpip, connection_test, tcpip_transport);

TEST(test_connection_tcpip, connect_to_an_address_that_does_not_resolve) {
  krpc::Connection connection("not-a-host.invalid", 50000);
  ASSERT_THROW(connection.connect(), std::exception);
}
