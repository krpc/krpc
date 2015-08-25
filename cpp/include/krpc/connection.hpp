#ifndef HEADER_KRPC_CONNECTION
#define HEADER_KRPC_CONNECTION

#include <boost/asio.hpp>
#include <boost/exception/all.hpp>
#include <string>
#include <vector>

namespace krpc {

  struct ConnectionFailed : virtual boost::exception, virtual std::exception {};

  class Connection {

    boost::asio::io_service io_service;
    boost::asio::ip::tcp::socket socket;
    const std::string address;
    const unsigned int port;
    boost::asio::ip::tcp::resolver resolver;

  public:

    Connection(const std::string& address, unsigned int port);
    void connect(unsigned int retries = 0, float timeout = 0);
    void close();
    /** Send data to the connection. Blocks until all data has been sent. */
    void send(const char* data, size_t length);
    void send(const std::string& data);
    /** Receive data from the connection. Blocks until length bytes have been received. */
    std::string receive(size_t length);
    /**
     * Receive up to length bytes of data from the connection.
     * Blocks until at least 1 byte has been received.
     */
    char* partial_receive();
  };

}

#endif
