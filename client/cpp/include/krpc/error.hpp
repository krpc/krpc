#pragma once

#include <stdexcept>
#include <string>
#include <utility>

namespace krpc {

/** Thrown when an error occurs connecting to the server, or when the connection fails or
    ends. status is the name of the ConnectionResponse status when the server refused the
    connection, such as "DENIED". reason is the name of the Disconnect reason when the server
    ended the connection, such as "SERVER_STOPPED". Each is empty otherwise. */
class ConnectionError : public std::runtime_error {
 public:
  inline explicit ConnectionError(const std::string& msg, std::string status = "",
                                  std::string reason = "")
      : std::runtime_error(msg), _status(std::move(status)), _reason(std::move(reason)) {}
  inline const std::string& status() const { return _status; }
  inline const std::string& reason() const { return _reason; }

 private:
  std::string _status;
  std::string _reason;
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
