#pragma once

#include <stdexcept>
#include <string>
#include <utility>

namespace krpc {

/** The base of every error the client throws of its own. A server exception with a mapped type,
    such as ArgumentException, throws the standard exception it stands for, and the client's
    argument errors are std::invalid_argument. */
class RemoteError : public std::runtime_error {
 public:
  inline explicit RemoteError(const std::string& msg) : std::runtime_error(msg) {}
};

/** Thrown when an error occurs connecting to the server, or when the connection fails or
    ends. status is the name of the ConnectionResponse status when the server refused the
    connection, such as "DENIED". reason is the name of the Disconnect reason when the server
    ended the connection, such as "SERVER_STOPPED". Each is empty otherwise. */
class ConnectionError : public RemoteError {
 public:
  inline explicit ConnectionError(const std::string& msg, std::string status = "",
                                  std::string reason = "")
      : RemoteError(msg), _status(std::move(status)), _reason(std::move(reason)) {}
  inline const std::string& status() const { return _status; }
  inline const std::string& reason() const { return _reason; }

 private:
  std::string _status;
  std::string _reason;
};

/** Thrown for an error the server reports, through a call, a stream or an event, that has no
    standard exception type. The exception classes of the services derive it. */
class ServerError : public RemoteError {
 public:
  inline explicit ServerError(const std::string& msg) : RemoteError(msg) {}
};

/** Thrown for a stream or event that has been removed, or that cannot be added */
class StreamError : public RemoteError {
 public:
  inline explicit StreamError(const std::string& msg) : RemoteError(msg) {}
};

/** Thrown for a value that fails to encode or decode */
class EncodingError : public RemoteError {
 public:
  inline explicit EncodingError(const std::string& msg) : RemoteError(msg) {}
};

}  // namespace krpc
