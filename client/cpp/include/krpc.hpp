#pragma once

#include <chrono>  // NOLINT(build/c++11)
#include <string>

#include "krpc/client.hpp"   // IWYU pragma: export
#include "krpc/error.hpp"    // IWYU pragma: export
#include "krpc/hash.hpp"     // IWYU pragma: export
#include "krpc/krpc.pb.hpp"  // IWYU pragma: export
#include "krpc/object.hpp"   // IWYU pragma: export

namespace krpc {

/**
 * Connect to a kRPC server on the specified IP address and port number.
 * Optionally give the kRPC server the supplied name to identify the client
 * (up to 32 bytes of UTF-8 encoded text).
 * If timeout is non-zero, gives up after waiting that long for a connection, rather
 * than waiting indefinitely. Gives up after handshake_timeout of waiting for the server
 * to accept the connection, or waits indefinitely if it is zero, such as for a server
 * that asks the player to approve each connection. Fails the connection when a message from
 * the server receives no new byte for stall_timeout once it has started to arrive, or never if
 * it is zero.
 */
Client connect(const std::string& name = "", const std::string& address = "127.0.0.1",
               unsigned int port = 50000,
               std::chrono::milliseconds timeout = std::chrono::milliseconds::zero(),
               std::chrono::milliseconds handshake_timeout = std::chrono::seconds(5),
               std::chrono::milliseconds stall_timeout = std::chrono::seconds(30));

/**
 * Connect to a kRPC server on the same machine, over a unix domain socket named by the
 * given path rather than over TCP. An empty path stands for the one the server uses
 * unless it was configured with another. The connection behaves identically once
 * established. See connect for the handshake and stall timeouts.
 * Unix domain sockets are available on Linux, macOS, and Windows 10 1803 and later.
 */
Client connect_local(const std::string& name = "", const std::string& path = "",
                     std::chrono::milliseconds handshake_timeout = std::chrono::seconds(5),
                     std::chrono::milliseconds stall_timeout = std::chrono::seconds(30));

}  // namespace krpc
