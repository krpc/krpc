#pragma once

#include <chrono>  // NOLINT(build/c++11)
#include <cstddef>
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
  /** Send data to the connection. Blocks until all data has been sent. */
  void send(const char* data, size_t length);
  void send(const std::string& data);
  /** Receive data from the connection for a message. Blocks until a message has been received. */
  std::string receive_message();
  /** Receive a message from the connection and parse it. Blocks until a message has been
      received. Parses it out of the read buffer, so nothing is copied on the way. */
  void receive_message(google::protobuf::MessageLite& message);
  /** Receive data from the connection. Blocks until length bytes have been received. */
  std::string receive(size_t length);
  /** Receive up to length bytes of data from the connection. */
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
  std::pair<const char*, size_t> buffered_message();
  /** Read a block from the socket into the buffer. Blocks until at least one byte arrives. */
  void fill();
  /** Take length bytes of what has been read but not consumed yet. */
  void take(char* data, size_t length);
  /** How much has been read but not consumed yet. */
  size_t available() const { return filled - consumed; }

  const std::string address;
  const unsigned int port;
  // How long to wait for the connection to be made. Zero waits indefinitely.
  const std::chrono::milliseconds timeout;
  asio::ip::tcp::resolver resolver;
  // Data read from the socket, how much of it there is and how much has been consumed. Reads
  // are made a block at a time rather than exactly the bytes wanted, so that a message costs
  // one read rather than one per byte of its size prefix plus one for its body.
  std::vector<char> buffer;
  size_t filled = 0;
  size_t consumed = 0;
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
