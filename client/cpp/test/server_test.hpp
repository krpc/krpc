#pragma once

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
