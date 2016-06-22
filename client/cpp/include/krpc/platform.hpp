#pragma once

#include <string>

namespace krpc {
namespace platform {

std::string hexlify(const std::string& data);
std::string unhexlify(const std::string& data);

}  // namespace platform
}  // namespace krpc
