#include "krpc/connection.hpp"
#include <boost/date_time/posix_time/posix_time.hpp>
#include <boost/thread/thread.hpp>

namespace asio = boost::asio;
namespace ip = boost::asio::ip;

namespace krpc {

  Connection::Connection(const std::string& address, unsigned int port):
    socket(io_service), address(address), port(port), resolver(io_service) {}

  void Connection::connect(unsigned int retries, float timeout) {
    std::ostringstream port_str;
    port_str << port;
    ip::tcp::resolver::query query(ip::tcp::v4(), address, port_str.str());
    ip::tcp::resolver::iterator iterator = resolver.resolve(query);
    while (true) {
      try {
        asio::connect(socket, iterator);
        break;
      } catch(const boost::system::system_error& e) {
        if (retries <= 0)
          throw e;
        retries -= 1;
        boost::this_thread::sleep(boost::posix_time::milliseconds(timeout*1000));
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
    size_t read_length = asio::read(socket, asio::buffer(&data[0], length));
    assert(length == read_length);
    return data;
  }

}
