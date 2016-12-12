#include "krpc/connection.hpp"

#include <string>
#include <thread>  // NOLINT(build/c++11)
#include <vector>

#include <asio/steady_timer.hpp>

namespace krpc {

Connection::Connection(const std::string& address, unsigned int port):
  socket(io_service), address(address), port(port), resolver(io_service) {}

void Connection::connect(unsigned int retries, float timeout) {
  std::ostringstream port_str;
  port_str << port;
  asio::ip::tcp::resolver::query query(asio::ip::tcp::v4(), address, port_str.str());
  asio::ip::tcp::resolver::iterator iterator = resolver.resolve(query);
  while (true) {
    try {
      asio::connect(socket, iterator);
      break;
    } catch(const asio::system_error&) {
      if (retries <= 0)
        throw;
      retries -= 1;
      std::this_thread::sleep_for(std::chrono::milliseconds(static_cast<int>(timeout*1000)));
    }
  }
}

void Connection::send(const char* data, size_t length) {
  asio::write(socket, asio::buffer(data, length));
}

void Connection::send(const std::string& data) {
  asio::write(socket, asio::buffer(data));
}

std::string Connection::receive(size_t length) {
  std::string data;
  data.resize(length);
  asio::read(socket, asio::buffer(&data[0], length));
  return data;
}

std::string Connection::partial_receive(size_t length, std::chrono::milliseconds timeout) {
  size_t read = 0;
  std::string data;
  data.resize(length);

  bool timer_complete = false;
  asio::steady_timer timer(socket.get_io_service());
  timer.expires_from_now(timeout);
  timer.async_wait(
    [&timer_complete] (const asio::error_code& error) {
      timer_complete = true;
    });

  bool read_complete = false;
  asio::async_read(
    socket, asio::buffer(&data[0], length),
    [&read, &read_complete] (const asio::error_code& error, size_t length) {
      read = length;
      read_complete = true;
    });

  socket.get_io_service().reset();
  while (socket.get_io_service().run_one()) {
    if (read_complete)
      timer.cancel();
    else if (timer_complete)
      socket.cancel();
  }

  if (read < length)
    data.resize(read);
  return data;
}

}  // namespace krpc
