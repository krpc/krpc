#ifndef HEADER_KRPC_ERROR
#define HEADER_KRPC_ERROR

#include <stdexcept>

namespace krpc {

  class RPCError: public std::runtime_error {
  public:
    RPCError(const std::string& msg): std::runtime_error(msg) {}
  };

}

#endif
