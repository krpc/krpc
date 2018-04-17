#pragma once

#include <krpc_cnano.h>

#include <string>

inline std::string hexlify(const uint8_t * data, size_t size) {
  std::string result(size*2, '0');
  const char hex[] = "0123456789abcdef";
  for (unsigned int i = 0, j = 0; i < size; i++, j += 2) {
    unsigned char c = static_cast<unsigned char>(data[i]);
    result[j] = hex[c >> 4];
    result[j+1] = hex[c & 0xf];
  }
  return result;
}

inline static unsigned char hex2uint(unsigned char x) {
  if (48 <= x && x <= 57)
    return x-48;
  else if (97 <= x && x <= 102)
    return x-97 + 10;
  else
    return 0;
}

inline void unhexlify(uint8_t * result, const std::string& data) {
  for (unsigned int i = 0, j = 0; i < data.size(); i += 2, j++)
    result[j] = (hex2uint(data[i]) << 4) | hex2uint(data[i+1]);
}
