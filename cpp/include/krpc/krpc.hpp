#ifndef HEADER_KRPC
#define HEADER_KRPC

#include <string>
#include <memory>
#include <boost/exception/all.hpp>
#include "krpc/client.hpp"
#include "krpc/connection.hpp"

namespace krpc {

  const char RPC_HELLO_MESSAGE[] = { 0x48, 0x45, 0x4C, 0x4C, 0x4F, 0x2D, 0x52, 0x50, 0x43, 0x00, 0x00, 0x00 };
  const char STREAM_HELLO_MESSAGE[] = { 0x48, 0x45, 0x4C, 0x4C, 0x4F, 0x2D, 0x53, 0x54, 0x52, 0x45, 0x41, 0x4D };
  const char OK_MESSAGE[] = { 0x4F, 0x4B };
  const size_t GUID_LENGTH = 16;

  typedef boost::error_info<struct tag_error_description, std::string> error_description;

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
