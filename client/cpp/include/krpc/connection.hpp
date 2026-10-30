#pragma once

#include <chrono>  // NOLINT(build/c++11)
#include <cstddef>
#include <stdexcept>
#include <string>
#include <utility>
#include <vector>

#ifndef ASIO_STANDALONE
#define ASIO_STANDALONE
#endif
#include <asio/generic/stream_protocol.hpp>
#include <asio/io_context.hpp>
#include <asio/ip/tcp.hpp>

namespace google {
namespace protobuf {
class MessageLite;
}
}  // namespace google

namespace krpc {

class Connection {
 public:
  Connection(const std::string& address, unsigned int port,
             std::chrono::milliseconds timeout = std::chrono::milliseconds::zero());
  virtual ~Connection() = default;
  /** Open the connection to the server. */
  virtual void connect();
  /** Close the connection. Sending or receiving on it afterwards reports the failure. */
  void close();
  /** Close the write side of the connection, leaving the read side open. */
  void shutdown_send();
  /** Close both sides of the connection, which wakes a thread blocked reading it. */
  void shutdown_both();
  /** Send data to the connection. Blocks until all data has been sent. */
  void send(const char* data, size_t length);
  void send(const std::string& data);
  /** Receive data from the connection for a message. Blocks until a message has been received. */
  std::string receive_message();
  /** Receive a message from the connection and parse it. Blocks until a message has been
      received. Parses it out of the read buffer, so nothing is copied on the way. */
  void receive_message(google::protobuf::MessageLite& message);
  /** Receive a message from the connection and parse it. Throws a TimeoutError if no whole
      message arrives within the timeout, or if a message that has started to arrive receives
      no new byte within the stall timeout. A zero timeout waits indefinitely. */
  void receive_message(google::protobuf::MessageLite& message, std::chrono::milliseconds timeout,
                       std::chrono::milliseconds stall_timeout);
  /** Receive the bytes of a message, which stay valid until the next read. Throws a TimeoutError
      as receive_message does. */
  std::pair<const char*, size_t> receive_message_data(std::chrono::milliseconds timeout,
                                                      std::chrono::milliseconds stall_timeout) {
    return buffered_message(timeout, stall_timeout);
  }
  /** Read and discard what arrives within the timeout. Returns false once the server has
      closed the connection, or if nothing arrives. */
  bool discard(std::chrono::milliseconds timeout);
  /** Whether bytes have been read that are not consumed yet. */
  bool buffered() const { return available() > 0; }
  /** Whether a whole message has been read and not consumed yet. */
  bool message_buffered() const;
  /** Wait until data has arrived, or the connection has closed. Returns false if neither happens
      within the timeout. A zero timeout waits indefinitely. */
  bool wait_readable(std::chrono::milliseconds timeout);
  /** Whether data has arrived, or the connection has closed, without waiting. */
  bool readable() { return poll(0); }
  /** Receive data from the connection. Blocks until length bytes have been received. */
  std::string receive(size_t length);
  /** Receive up to length bytes of data from the connection, or none if the timeout passes
      first. Throws if the connection has closed or failed. */
  std::string partial_receive(size_t length,
                              std::chrono::milliseconds timeout = std::chrono::milliseconds(10));

 protected:
  asio::io_context io_context;
  // A protocol agnostic socket, so that the same connection can carry either a TCP
  // or a unix domain socket and everything above it is unchanged
  asio::generic::stream_protocol::socket socket;

 private:
  /** How much is read from the socket at a time. */
  static const size_t READ_SIZE = 8192;

  /** Wait for the buffer to hold a whole message, consume it, and return where it starts and
      how long it is. Valid until the next read. */
  std::pair<const char*, size_t> buffered_message(
      std::chrono::milliseconds timeout = std::chrono::milliseconds::zero(),
      std::chrono::milliseconds stall_timeout = std::chrono::milliseconds::zero());
  /** Read a block from the socket into the buffer. Blocks until at least one byte arrives. */
  void fill();
  /** Read a block from the socket into the buffer, waiting no longer than the timeout.
      Returns false if nothing arrived in time. */
  bool fill(std::chrono::milliseconds timeout);
  /** Take length bytes of what has been read but not consumed yet. */
  void take(char* data, size_t length);
  /** How much has been read but not consumed yet. */
  size_t available() const { return filled - consumed; }
  /** Poll the socket for data, for up to the given milliseconds, or indefinitely if -1. */
  bool poll(int milliseconds);

  const std::string address;
  const unsigned int port;
  // The time to wait for the connection to be made. Zero waits indefinitely.
  const std::chrono::milliseconds timeout;
  asio::ip::tcp::resolver resolver;
  // Data read from the socket, how much of it there is and how much has been consumed. Reads
  // are made a block at a time, so that a message costs one read and not one per byte of its
  // size prefix plus one for its body.
  std::vector<char> buffer;
  size_t filled = 0;
  size_t consumed = 0;
  // When a read last received a byte
  std::chrono::steady_clock::time_point progress = std::chrono::steady_clock::now();
};

/** Thrown when a read from the server times out. */
class TimeoutError : public std::runtime_error {
 public:
  inline explicit TimeoutError(const std::string& msg) : std::runtime_error(msg) {}
};

/** A connection to a server on the same machine, over a unix domain socket. Only opening
    the socket differs from a TCP connection, so that is all this replaces. */
class LocalConnection : public Connection {
 public:
  explicit LocalConnection(const std::string& path);
  void connect() override;

 private:
  const std::string path;
};

}  // namespace krpc
