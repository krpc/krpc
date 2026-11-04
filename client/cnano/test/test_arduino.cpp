#include <krpc_cnano/communication.h>

#include "gtest/gtest.h"

static const uint8_t data[] = {1, 2, 3, 4, 5, 6, 7, 8};

static krpc_connection_t open_stream(Stream* stream) {
  krpc_connection_t connection = {nullptr, 0};
  EXPECT_EQ(KRPC_OK, krpc_open_stream(&connection, stream));
  return connection;
}

TEST(test_arduino, test_read) {
  const size_t chunks[] = {8};
  ScriptedSerial serial(data, sizeof(data), chunks, 1);

  uint8_t buf[8] = {0};
  ASSERT_EQ(KRPC_OK, krpc_read(open_stream(&serial), buf, sizeof(buf)));
  for (size_t i = 0; i < sizeof(buf); i++) ASSERT_EQ(data[i], buf[i]);
}

// A read that returns fewer bytes than requested must resume at the point the previous one
// finished, and not restart at the beginning of the buffer.
TEST(test_arduino, test_read_partial) {
  const size_t chunks[] = {3, 5};
  ScriptedSerial serial(data, sizeof(data), chunks, 2);

  uint8_t buf[8] = {0};
  ASSERT_EQ(KRPC_OK, krpc_read(open_stream(&serial), buf, sizeof(buf)));
  for (size_t i = 0; i < sizeof(buf); i++) ASSERT_EQ(data[i], buf[i]);
}

// A read that returns nothing means the stream timeout elapsed without a single byte arriving,
// which is as close to a disconnect as a serial link gets. It must fail, and not retry forever.
TEST(test_arduino, test_read_timeout) {
  const size_t chunks[] = {2, 0};
  ScriptedSerial serial(data, sizeof(data), chunks, 2);

  uint8_t buf[4] = {0};
  ASSERT_EQ(KRPC_ERROR_EOF, krpc_read(open_stream(&serial), buf, sizeof(buf)));
}

TEST(test_arduino, test_write) {
  ScriptedSerial serial(data, sizeof(data), nullptr, 0);

  ASSERT_EQ(KRPC_OK, krpc_write(open_stream(&serial), data, sizeof(data)));
  ASSERT_EQ(sizeof(data), serial.num_written);
  for (size_t i = 0; i < sizeof(data); i++) ASSERT_EQ(data[i], serial.written[i]);
}

// A stream is resumable, and closing it leaves the port as the sketch started it
TEST(test_arduino, test_open_stream) {
  ScriptedSerial serial(data, sizeof(data), nullptr, 0);

  krpc_connection_t connection = open_stream(&serial);
  ASSERT_EQ(&krpc_transport_stream, connection.transport);
  ASSERT_TRUE(connection.transport->resumable);
  ASSERT_EQ(KRPC_OK, krpc_close(connection));
}

// Opening a serial port starts it at the given speed, and waits for it to be ready
TEST(test_arduino, test_open_serial) {
  ScriptedSerial serial(data, sizeof(data), nullptr, 0);
  serial.unready_checks = 3;

  krpc_connection_t connection = {nullptr, 0};
  ASSERT_EQ(KRPC_OK, krpc_open_serial(&connection, &serial, 115200));
  ASSERT_EQ(115200u, serial.speed);
  ASSERT_EQ(0, serial.unready_checks);
  ASSERT_EQ(&krpc_transport_stream, connection.transport);
  ASSERT_EQ(reinterpret_cast<uintptr_t>(static_cast<Stream*>(&serial)), connection.handle);
}
