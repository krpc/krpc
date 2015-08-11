#ifndef HEADER_KRPC
#define HEADER_KRPC

#include <string>
#include <memory>
#include "krpc/error.hpp"
#include "krpc/client.hpp"
#include "krpc/connection.hpp"

namespace krpc {

  /**
   * Connect to a kRPC server on the specified IP address and port numbers. If
   * stream_port is None, does not connect to the stream server.
   * Optionally give the kRPC server the supplied name to identify the client (up
   * to 32 bytes of UTF-8 encoded text).
   */
  Client connect(const std::string& name, const std::string& address,
                 unsigned int rpc_port, unsigned int stream_port);

}

#endif
