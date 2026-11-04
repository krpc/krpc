#include <krpc_cnano.h>

#include <cstring>
#include <deque>
#include <iterator>
#include <vector>

#include "gtest/gtest.h"

namespace {

// A transport the program supplies: an in-memory link, with the bytes to read queued up front
struct memory_link {
  std::deque<uint8_t> incoming;
  std::vector<uint8_t> outgoing;
  int closes = 0;
};

krpc_error_t memory_read(uintptr_t handle, uint8_t* buf, size_t count) {
  auto* link = reinterpret_cast<memory_link*>(handle);
  for (size_t i = 0; i < count; i++) {
    if (link->incoming.empty()) return KRPC_ERROR_EOF;
    buf[i] = link->incoming.front();
    link->incoming.pop_front();
  }
  return KRPC_OK;
}

krpc_error_t memory_write(uintptr_t handle, const uint8_t* buf, size_t count) {
  auto* link = reinterpret_cast<memory_link*>(handle);
  link->outgoing.insert(link->outgoing.end(), buf, buf + count);
  return KRPC_OK;
}

krpc_error_t memory_close(uintptr_t handle) {
  reinterpret_cast<memory_link*>(handle)->closes++;
  return KRPC_OK;
}

const krpc_transport_t memory_transport = {&memory_read, &memory_write, &memory_close, false};

krpc_connection_t memory_connection(memory_link* link, const krpc_transport_t* transport) {
  krpc_connection_t connection;
  connection.transport = transport;
  connection.handle = reinterpret_cast<uintptr_t>(link);
  return connection;
}

// A Message holding a connection response with status OK and protocol version 2, framed with
// its size
const uint8_t connection_response_ok[] = {0x06, 0x2a, 0x04, 0x08, 0x00, 0x20, 0x02};

}  // namespace

TEST(test_transport, test_read_write_close) {
  memory_link link;
  link.incoming = {1, 2, 3};
  krpc_connection_t connection = memory_connection(&link, &memory_transport);

  uint8_t buf[3] = {0};
  ASSERT_EQ(KRPC_OK, krpc_read(connection, buf, sizeof(buf)));
  ASSERT_EQ(std::vector<uint8_t>({1, 2, 3}), std::vector<uint8_t>(buf, buf + sizeof(buf)));
  ASSERT_EQ(KRPC_ERROR_EOF, krpc_read(connection, buf, 1));

  ASSERT_EQ(KRPC_OK, krpc_write(connection, buf, sizeof(buf)));
  ASSERT_EQ(std::vector<uint8_t>({1, 2, 3}), link.outgoing);

  ASSERT_EQ(KRPC_OK, krpc_close(connection));
  ASSERT_EQ(1, link.closes);
}

// A transport with no close function has nothing to close
TEST(test_transport, test_close_without_close_function) {
  const krpc_transport_t transport = {&memory_read, &memory_write, nullptr, false};
  memory_link link;
  ASSERT_EQ(KRPC_OK, krpc_close(memory_connection(&link, &transport)));
}

// The client talks to a server through a transport the program supplies
TEST(test_transport, test_connect) {
  memory_link link;
  link.incoming.assign(std::begin(connection_response_ok), std::end(connection_response_ok));
  krpc_connection_t connection = memory_connection(&link, &memory_transport);

  ASSERT_EQ(KRPC_OK, krpc_connect(connection, "test"));
  ASSERT_FALSE(link.outgoing.empty());
  ASSERT_EQ(KRPC_OK, krpc_disconnect(connection));
  ASSERT_EQ(1, link.closes);
}

// A connection whose open failed has no transport, and every call on it fails
TEST(test_transport, test_not_open) {
  krpc_connection_t connection = {nullptr, 0};
  uint8_t buf[1] = {0};
  ASSERT_EQ(KRPC_ERROR_IO, krpc_connect(connection, "test"));
  ASSERT_EQ(KRPC_ERROR_IO, krpc_read(connection, buf, sizeof(buf)));
  ASSERT_EQ(KRPC_ERROR_IO, krpc_write(connection, buf, sizeof(buf)));
  ASSERT_EQ(KRPC_ERROR_IO, krpc_close(connection));
  ASSERT_EQ(KRPC_ERROR_IO, krpc_disconnect(connection));
}

// A failed connect closes a connection that is not resumable, and leaves a resumable one open
// for the program to retry
TEST(test_transport, test_connect_failed) {
  const krpc_transport_t resumable = {&memory_read, &memory_write, &memory_close, true};

  memory_link closed;
  ASSERT_EQ(KRPC_ERROR_CONNECTION_FAILED,
            krpc_connect(memory_connection(&closed, &memory_transport), "test"));
  ASSERT_EQ(1, closed.closes);

  memory_link kept;
  ASSERT_EQ(KRPC_ERROR_CONNECTION_FAILED,
            krpc_connect(memory_connection(&kept, &resumable), "test"));
  ASSERT_EQ(0, kept.closes);
}
