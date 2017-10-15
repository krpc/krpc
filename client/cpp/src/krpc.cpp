#include "krpc.hpp"

#include <string>

namespace krpc {

Client connect(const std::string& name, const std::string& address,
               unsigned int rpc_port, unsigned int stream_port) {
  return Client(name, address, rpc_port, stream_port);
}

}  // namespace krpc
