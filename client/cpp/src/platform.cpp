#include "krpc/platform.hpp"

#include <string>

namespace krpc {
namespace platform {

std::string hexlify(const std::string& data) {
  std::string result(data.size()*2, '0');
  const char hex[] = "0123456789abcdef";
  for (unsigned int i = 0, j = 0; i < data.size(); i++, j += 2) {
    unsigned char c = static_cast<unsigned char>(data[i]);
    result[j] = hex[c >> 4];
    result[j+1] = hex[c & 0xf];
  }
  return result;
}

static unsigned char hex2uint(unsigned char x) {
  if (48 <= x && x <= 57)
    return x-48;
  else if (97 <= x && x <= 102)
    return x-97 + 10;
  else
    return 0;
}

std::string unhexlify(const std::string& data) {
  std::string result(data.size() / 2, '0');
  for (unsigned int i = 0, j = 0; i < data.size(); i += 2, j++)
    result[j] = (hex2uint(data[i]) << 4) | hex2uint(data[i+1]);
  return result;
}

}  // namespace platform
}  // namespace krpc
