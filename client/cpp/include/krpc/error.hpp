#pragma once

#include <stdexcept>
#include <string>

namespace krpc {

/** Thrown when an error occurs connecting to the server */
class ConnectionError : public std::runtime_error {
 public:
  inline explicit ConnectionError(const std::string& msg) : std::runtime_error(msg) {}
};

/** Thrown when an error occurs executing a remote procedure call */
class RPCError : public std::runtime_error {
 public:
  inline explicit RPCError(const std::string& msg) : std::runtime_error(msg) {}
};

/** Thrown when an error occurs on a stream operation */
class StreamError : public std::runtime_error {
 public:
  inline explicit StreamError(const std::string& msg) : std::runtime_error(msg) {}
};

/** Thrown when an error occurs encoding or decoding a message */
class EncodingError : public std::runtime_error {
 public:
  inline explicit EncodingError(const std::string& msg) : std::runtime_error(msg) {}
};

}  // namespace krpc
