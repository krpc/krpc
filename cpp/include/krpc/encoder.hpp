#ifndef HEADER_KRPC_ENCODER
#define HEADER_KRPC_ENCODER

#include <string>
#include <vector>

namespace krpc {

  class Encoder {
  public:
    static std::vector<char> client_name(const std::string& name);
  };

}

#endif
