#pragma once

#include <string>

#include "krpc/client.hpp"   // IWYU pragma: export
#include "krpc/error.hpp"    // IWYU pragma: export
#include "krpc/krpc.pb.hpp"  // IWYU pragma: export
#include "krpc/object.hpp"   // IWYU pragma: export

namespace krpc {

/**
 * Connect to a kRPC server on the specified IP address and port numbers.
 * If stream_port is 0, does not connect to the stream server.
 * Optionally give the kRPC server the supplied name to identify the client
 * (up to 32 bytes of UTF-8 encoded text).
 */
Client connect(const std::string& name = "", const std::string& address = "127.0.0.1",
               unsigned int rpc_port = 50000, unsigned int stream_port = 50001);

/**
 * Connect to a kRPC server on the same machine, over unix domain sockets named by the
 * given paths rather than over TCP. An empty path stands for the one the server uses
 * unless it was configured with another. The connection behaves identically once
 * established.
 * Unix domain sockets are available on Linux, macOS, and Windows 10 1803 and later.
 */
Client connect_local(const std::string& name = "", const std::string& rpc_path = "",
                     const std::string& stream_path = "");

}  // namespace krpc
