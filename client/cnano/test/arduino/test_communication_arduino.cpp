#include <krpc_cnano/communication.h>

#include "gtest/gtest.h"

static const uint8_t data[] = {1, 2, 3, 4, 5, 6, 7, 8};

TEST(test_communication_arduino, test_read) {
  const size_t chunks[] = {8};
  HardwareSerial serial(data, sizeof(data), chunks, 1);

  uint8_t buf[8] = {0};
  ASSERT_EQ(KRPC_OK, krpc_read(&serial, buf, sizeof(buf)));
  for (size_t i = 0; i < sizeof(buf); i++) ASSERT_EQ(data[i], buf[i]);
}

// A read that returns fewer bytes than requested must resume at the point the previous one
// finished, rather than restarting at the beginning of the buffer.
TEST(test_communication_arduino, test_read_partial) {
  const size_t chunks[] = {3, 5};
  HardwareSerial serial(data, sizeof(data), chunks, 2);

  uint8_t buf[8] = {0};
  ASSERT_EQ(KRPC_OK, krpc_read(&serial, buf, sizeof(buf)));
  for (size_t i = 0; i < sizeof(buf); i++) ASSERT_EQ(data[i], buf[i]);
}

// A read that returns nothing means the serial timeout elapsed without a single byte
// arriving, which is as close to a disconnect as a serial link gets. It must fail rather than
// retry forever.
TEST(test_communication_arduino, test_read_timeout) {
  const size_t chunks[] = {2, 0};
  HardwareSerial serial(data, sizeof(data), chunks, 2);

  uint8_t buf[4] = {0};
  ASSERT_EQ(KRPC_ERROR_EOF, krpc_read(&serial, buf, sizeof(buf)));
}

TEST(test_communication_arduino, test_write) {
  HardwareSerial serial(data, sizeof(data), NULL, 0);

  ASSERT_EQ(KRPC_OK, krpc_write(&serial, data, sizeof(data)));
  ASSERT_EQ(sizeof(data), serial.num_written);
  for (size_t i = 0; i < sizeof(data); i++) ASSERT_EQ(data[i], serial.written[i]);
}
