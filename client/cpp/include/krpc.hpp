#pragma once

#include <string>

#include "krpc/client.hpp"
#include "krpc/error.hpp"

namespace krpc {

/**
 * Connect to a kRPC server on the specified IP address and port numbers.
 * If stream_port is 0, does not connect to the stream server.
 * Optionally give the kRPC server the supplied name to identify the client
 * (up to 32 bytes of UTF-8 encoded text).
 */
Client connect(const std::string& name = "", const std::string& address = "127.0.0.1",
               unsigned int rpc_port = 50000, unsigned int stream_port = 50001);

}  // namespace krpc
