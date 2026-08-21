#pragma once

#include <gtest/gtest.h>

#include <asio/buffer.hpp>
#include <asio/error_code.hpp>
#include <asio/io_context.hpp>
#include <asio/write.hpp>
#include <atomic>
#include <cstddef>
#include <memory>
#include <string>
#include <thread>  // NOLINT(build/c++11)

#include "krpc/connection.hpp"
#include "krpc/encoder.hpp"
#include "krpc/krpc.pb.hpp"

/** A stand-in for a kRPC server, which sends back whatever it is sent. These tests are
    about how a transport moves bytes, so nothing above the transport has to be understood
    to answer them. */
template <typename Protocol>
class echo_server {
 public:
  explicit echo_server(const typename Protocol::endpoint& endpoint)
      : acceptor_(io_context_), stopping_(false) {
    // Opened, bound and listened on a step at a time rather than through the constructor
    // that does all three, which also asks for the address to be reusable. That option is
    // meaningless for a unix domain socket, and asking for it on one is an error on Windows;
    // the port these tests listen on over TCP/IP is one the system picks, so nothing needs it.
    acceptor_.open(endpoint.protocol());
    acceptor_.bind(endpoint);
    acceptor_.listen();
    thread_ = std::thread([this]() { this->run(); });
  }

  ~echo_server() {
    stopping_ = true;
    // Wake the thread waiting in accept by connecting to it. Closing the acceptor from
    // here instead would be closing a socket another thread is in the middle of using.
    asio::error_code error;
    typename Protocol::socket waker(io_context_);
    waker.connect(acceptor_.local_endpoint(), error);
    thread_.join();
    acceptor_.close(error);
  }

  typename Protocol::endpoint endpoint() const { return acceptor_.local_endpoint(); }

 private:
  void run() {
    std::string buffer(4096, '\0');
    while (true) {
      asio::error_code error;
      typename Protocol::socket socket(io_context_);
      acceptor_.accept(socket, error);
      if (error || stopping_) return;
      while (true) {
        size_t length = socket.read_some(asio::buffer(&buffer[0], buffer.size()), error);
        if (error) break;
        asio::write(socket, asio::buffer(buffer.data(), length), error);
        if (error) break;
      }
    }
  }

  asio::io_context io_context_;
  typename Protocol::acceptor acceptor_;
  std::atomic<bool> stopping_;
  std::thread thread_;
};

/** What a connection to a server does regardless of what carries it: sending, receiving
    whole and in part, and taking a message out of whatever a read happens to have brought
    in. Only opening the connection differs between the transports, so the bodies are
    shared and each transport supplies a server to talk to and a connection reaching it. */
template <typename Transport>
class connection_test : public ::testing::Test {
 public:
  void SetUp() override { transport.start(); }
  void TearDown() override { transport.stop(); }
  std::shared_ptr<krpc::Connection> connect() { return transport.connect(); }
  Transport transport;
};

TYPED_TEST_SUITE_P(connection_test);

TYPED_TEST_P(connection_test, send_receive) {
  auto connection = this->connect();
  connection->send("foo");
  ASSERT_EQ("foo", connection->receive(3));
}

TYPED_TEST_P(connection_test, long_send_receive) {
  // Longer than the block a read brings in, so the reply is reassembled out of several
  auto connection = this->connect();
  std::string message(16 * 1024, 'x');
  connection->send(message);
  ASSERT_EQ(message, connection->receive(message.size()));
}

TYPED_TEST_P(connection_test, send_receive_in_pieces) {
  // What is received need not line up with what was sent, as a read returns whatever has
  // arrived rather than what a particular send put on the wire
  auto connection = this->connect();
  connection->send("foobar");
  ASSERT_EQ("foo", connection->receive(3));
  ASSERT_EQ("bar", connection->receive(3));
}

TYPED_TEST_P(connection_test, partial_receive) {
  auto connection = this->connect();
  connection->send("foo");
  std::string received;
  while (received.size() < 3) received += connection->partial_receive(16);
  ASSERT_EQ("foo", received);
}

TYPED_TEST_P(connection_test, partial_receive_with_nothing_waiting) {
  // Nothing has been sent, so this gives up once its timeout has passed rather than
  // blocking until something arrives
  auto connection = this->connect();
  ASSERT_EQ("", connection->partial_receive(16));
}

TYPED_TEST_P(connection_test, receive_message) {
  auto connection = this->connect();
  krpc::schema::Response response;
  response.add_results()->set_value("foo");
  connection->send(krpc::encoder::encode_message_with_size(response));

  krpc::schema::Response received;
  connection->receive_message(received);
  ASSERT_EQ(1, received.results_size());
  ASSERT_EQ("foo", received.results(0).value());
}

TYPED_TEST_P(connection_test, receive_two_messages_from_one_read) {
  // A read brings in a block rather than a message, so a block holding two of them has to
  // yield both without a second read
  auto connection = this->connect();
  krpc::schema::Response response;
  response.add_results()->set_value("foo");
  std::string encoded = krpc::encoder::encode_message_with_size(response);
  connection->send(encoded + encoded);

  for (int i = 0; i < 2; i++) {
    krpc::schema::Response received;
    connection->receive_message(received);
    ASSERT_EQ("foo", received.results(0).value());
  }
}

REGISTER_TYPED_TEST_SUITE_P(connection_test, send_receive, long_send_receive,
                            send_receive_in_pieces, partial_receive,
                            partial_receive_with_nothing_waiting, receive_message,
                            receive_two_messages_from_one_read);
