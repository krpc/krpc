#include "krpc/encoder.hpp"

namespace krpc {

  std::vector<char> Encoder::client_name(const std::string& name) {
    std::vector<char> result(name.c_str(), name.c_str() + name.size());
    result.resize(32);
    return result;
  }

}
