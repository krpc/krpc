#pragma once

#include <stdexcept>
#include <string>

namespace krpc {

class RPCError : public std::runtime_error {
 public:
  explicit RPCError(const std::string& msg) : std::runtime_error(msg) {}
};

class StreamError : public std::runtime_error {
 public:
  explicit StreamError(const std::string& msg) : std::runtime_error(msg) {}
};

}  // namespace krpc
