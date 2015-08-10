#include "krpc/connection.hpp"

namespace asio = boost::asio;
namespace ip = boost::asio::ip;

namespace krpc {

  Connection::Connection(const std::string& address, unsigned int port):
    socket(io_service), address(address), port(port), resolver(io_service) {}

  void Connection::connect() {
    std::ostringstream port_str;
    port_str << port;
    ip::tcp::resolver::query query(ip::tcp::v4(), address, port_str.str());
    ip::tcp::resolver::iterator iterator = resolver.resolve(query);
    asio::connect(socket, iterator);
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
    size_t read_length = asio::read(socket, asio::buffer(&data[0], length));
    assert(length == read_length);
    return data;
  }

}
