#pragma once

#include <asio/io_context.hpp>
#include <asio/ip/address.hpp>
#include <asio/ip/tcp.hpp>
#include <krpc.hpp>
#include <krpc/services/krpc.hpp>

#include "services/test_service.hpp"

class server_test : public ::testing::Test {
 public:
  server_test();
  /** Connect over whichever transport the harness started the server with, which it tells
      us about by port or by socket path. The rpc and stream arguments name which of the
      server's two endpoints each connection should go to, so a test can deliberately
      connect them the wrong way round. */
  krpc::Client connect(const std::string& name = "C++ClientTest", const std::string& rpc = "rpc",
                       const std::string& stream = "stream");
  int get_rpc_port() const;
  int get_stream_port() const;
  const char* get_rpc_path() const;
  const char* get_stream_path() const;
  /** A port nothing is listening on, for the tests that connect to the wrong one. Binding a
      port and giving it straight back leaves one that a connection is refused on, and leaves
      it in the range the system hands out. A port derived from the server's own can land
      anywhere, including on a low one, and a connection to those is dropped rather than
      refused on Windows, which leaves the client waiting. */
  static int unused_port();
  krpc::Client conn;
  krpc::services::KRPC krpc;
  krpc::services::TestService test_service;
};

inline server_test::server_test() : conn(connect()), krpc(&conn), test_service(&conn) {}

inline krpc::Client server_test::connect(const std::string& name, const std::string& rpc,
                                         const std::string& stream) {
  if (get_rpc_path() != nullptr)
    return krpc::connect_local(name, rpc == "rpc" ? get_rpc_path() : get_stream_path(),
                               stream == "rpc" ? get_rpc_path() : get_stream_path());
  return krpc::connect(name, "localhost", rpc == "rpc" ? get_rpc_port() : get_stream_port(),
                       stream == "rpc" ? get_rpc_port() : get_stream_port());
}

inline const char* server_test::get_rpc_path() const { return std::getenv("RPC_PATH"); }

inline const char* server_test::get_stream_path() const { return std::getenv("STREAM_PATH"); }

inline int server_test::get_rpc_port() const {
  char* env_rpc_port = std::getenv("RPC_PORT");
  return env_rpc_port == nullptr ? 50000 : std::stoi(env_rpc_port);
}

inline int server_test::get_stream_port() const {
  char* env_stream_port = std::getenv("STREAM_PORT");
  return env_stream_port == nullptr ? 50001 : std::stoi(env_stream_port);
}

inline int server_test::unused_port() {
  asio::io_context io_context;
  asio::ip::tcp::acceptor acceptor(io_context);
  acceptor.open(asio::ip::tcp::v4());
  acceptor.bind(asio::ip::tcp::endpoint(asio::ip::make_address("127.0.0.1"), 0));
  return acceptor.local_endpoint().port();
}
