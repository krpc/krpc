#include "krpc/connection.hpp"

#include <google/protobuf/message_lite.h>

#include <algorithm>
#include <asio/buffer.hpp>
#include <asio/connect.hpp>  // IWYU pragma: keep
#include <asio/error.hpp>
#include <asio/error_code.hpp>
#include <asio/local/stream_protocol.hpp>
#include <asio/read.hpp>  // IWYU pragma: keep
#include <asio/steady_timer.hpp>
#include <asio/write.hpp>  // IWYU pragma: keep
#include <cstring>
#include <exception>
#include <memory>
#include <new>
#include <ostream>
#include <string>
#include <utility>
// IWYU pragma: no_include <asio/detail/impl/epoll_reactor.hpp>
// IWYU pragma: no_include <asio/detail/impl/reactive_socket_service_base.ipp>
// IWYU pragma: no_include <asio/impl/connect.hpp>
// IWYU pragma: no_include <asio/impl/read.hpp>
// IWYU pragma: no_include <asio/impl/write.hpp>

#include "krpc/decoder.hpp"
#include "krpc/error.hpp"

namespace krpc {

Connection::Connection(const std::string& address, unsigned int port,
                       std::chrono::milliseconds timeout)
    : socket(io_context), address(address), port(port), timeout(timeout), resolver(io_context) {}

// The socket is protocol agnostic, so it is connected through an endpoint of the same kind,
// which holds the address of any protocol as the system lays it out. The socket takes its
// protocol from the endpoint it is connected to, so nothing here is specific to one.
static void connect_generic(asio::generic::stream_protocol::socket& socket,
                            const asio::generic::stream_protocol::endpoint& endpoint) {
  socket.connect(endpoint);
}

// Connect, giving up once the deadline has passed. A network that drops a connection attempt
// instead of refusing it otherwise leaves the caller waiting indefinitely.
static void connect_generic(asio::io_context& io_context,
                            asio::generic::stream_protocol::socket& socket,
                            const asio::generic::stream_protocol::endpoint& endpoint,
                            std::chrono::steady_clock::time_point deadline) {
  bool timed_out = false;
  asio::steady_timer timer(socket.get_executor());
  timer.expires_at(deadline);
  timer.async_wait([&timed_out](const asio::error_code& error) {
    if (!error) timed_out = true;
  });

  bool connect_complete = false;
  asio::error_code connect_error;
  socket.async_connect(endpoint,
                       [&connect_complete, &connect_error](const asio::error_code& error) {
                         connect_error = error;
                         connect_complete = true;
                       });

  io_context.restart();
  while (io_context.run_one()) {
    if (connect_complete)
      timer.cancel();
    else if (timed_out)
      socket.cancel();
  }

  // A socket left half open belongs to a connection that was never made
  if (timed_out) {
    socket.close();
    throw asio::system_error(asio::error::timed_out);
  }
  if (connect_error) throw asio::system_error(connect_error);
}

void Connection::connect() {
  std::ostringstream port_str;
  port_str << port;
  auto endpoints = resolver.resolve(asio::ip::tcp::v4(), address, port_str.str());
  // Each address the name resolved to is tried in turn, so a host that has more than one is
  // reached through whichever of them answers, and the failure reported when none of them does
  // is the one from the last address tried. The timeout is what connecting is given as a
  // whole, so they share one deadline.
  auto deadline = std::chrono::steady_clock::now() + timeout;
  std::exception_ptr failure;
  for (auto& endpoint : endpoints) {
    try {
      if (timeout == std::chrono::milliseconds::zero())
        connect_generic(socket, endpoint.endpoint());
      else
        connect_generic(io_context, socket, endpoint.endpoint(), deadline);
    } catch (...) {
      // A socket opened for an address that did not answer is closed before the next address
      // is tried, as connecting one that is already open is an error.
      if (socket.is_open()) socket.close();
      failure = std::current_exception();
      continue;
    }
    // The protocol is strictly request and response, so holding a write back to coalesce it with
    // a later one can only add latency.
    socket.set_option(asio::ip::tcp::no_delay(true));
    return;
  }
  if (failure) std::rethrow_exception(failure);
  throw ConnectionError("could not resolve " + address);
}

LocalConnection::LocalConnection(const std::string& path) : Connection(path, 0), path(path) {}

void LocalConnection::connect() {
  // The generic endpoint copies the address in as raw bytes, which the analyzer cannot follow, so
  // it takes the address family it later reads back to be uninitialized.
  // NOLINTNEXTLINE(clang-analyzer-core.uninitialized.UndefReturn)
  connect_generic(socket, asio::local::stream_protocol::endpoint(path));
}

void Connection::close() {
  // A connection that was never opened, or has already been closed, has no socket to close
  if (socket.is_open()) socket.close();
  // Bytes read but not consumed belong to a connection that is gone, so they are dropped
  // and not handed out by a later read
  filled = 0;
  consumed = 0;
}

void Connection::send(const char* data, size_t length) {
  asio::write(socket, asio::buffer(data, length));
}

void Connection::send(const std::string& data) { asio::write(socket, asio::buffer(data)); }

std::string Connection::receive_message() {
  auto [data, size] = this->buffered_message();
  return std::string(data, size);
}

void Connection::receive_message(google::protobuf::MessageLite& message) {
  auto [data, size] = this->buffered_message();
  if (!message.ParseFromArray(data, static_cast<int>(size)))
    throw EncodingError("Failed to decode message");
}

std::pair<const char*, size_t> Connection::buffered_message() {
  // Read until the buffer holds a size prefix and the whole message it describes. A message
  // longer than a read is read through the buffer as well: the extra copy costs one memcpy,
  // where reading its body straight into its own storage costs a system call every time.
  uint32_t size = 0;
  size_t prefix_length = 0;
  while (!decoder::decode_size_prefix(buffer.data() + consumed, available(), &size, &prefix_length))
    this->fill();
  while (available() < prefix_length + size) this->fill();
  consumed += prefix_length;
  const char* message = buffer.data() + consumed;
  consumed += size;
  return {message, size};
}

void Connection::fill() {
  // Move what is left to the front, so the buffer does not grow with every read. In the
  // ordinary case nothing is left and this moves nothing.
  if (consumed > 0) {
    std::memmove(buffer.data(), buffer.data() + consumed, available());
    filled -= consumed;
    consumed = 0;
  }
  if (buffer.size() - filled < READ_SIZE) buffer.resize(filled + READ_SIZE);
  filled += socket.read_some(asio::buffer(&buffer[filled], buffer.size() - filled));
}

void Connection::take(char* data, size_t length) {
  std::memcpy(data, buffer.data() + consumed, length);
  consumed += length;
}

std::string Connection::receive(size_t length) {
  std::string data;
  data.resize(length);
  size_t buffered = std::min(length, available());
  this->take(&data[0], buffered);
  // Whatever an earlier read has already brought in is used first, and the rest read into the
  // caller's own storage: a message this long is one the buffer was never going to hold.
  if (buffered < length) asio::read(socket, asio::buffer(&data[buffered], length - buffered));
  return data;
}

std::string Connection::partial_receive(size_t length, std::chrono::milliseconds timeout) {
  // Data read for an earlier message is data that has arrived, so a caller polling for more
  // has to be given it before the socket is waited on.
  if (available() > 0) {
    length = std::min(length, available());
    std::string data;
    data.resize(length);
    this->take(&data[0], length);
    return data;
  }

  size_t read = 0;
  std::string data;
  data.resize(length);

  bool timer_complete = false;
  asio::steady_timer timer(socket.get_executor());
  timer.expires_after(timeout);
  timer.async_wait([&timer_complete](const asio::error_code& error) { timer_complete = true; });

  bool read_complete = false;
  asio::async_read(socket, asio::buffer(&data[0], length),
                   [&read, &read_complete](const asio::error_code& error, size_t length) {
                     read = length;
                     read_complete = true;
                   });

  io_context.restart();
  while (io_context.run_one()) {
    if (read_complete)
      timer.cancel();
    else if (timer_complete)
      socket.cancel();
  }

  if (read < length) data.resize(read);
  return data;
}

}  // namespace krpc
