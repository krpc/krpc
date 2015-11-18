#ifndef HEADER_KRPC_PLATFORM
#define HEADER_KRPC_PLATFORM

#include <string>
#include <cstdio>

namespace krpc {
  namespace platform {

    std::string hexlify(const std::string& data);
    std::string unhexlify(const std::string& data);

  }
}

#endif
