#include <krpc_cnano/communication.h>

#include "gtest/gtest.h"

#if defined(KRPC_COMMUNICATION_POSIX)

#include <unistd.h>

#include <chrono>
#include <thread>

// A read that returns fewer bytes than requested must resume at the point the previous one
// finished, rather than restarting at the beginning of the buffer.
TEST(test_communication, test_read_partial) {
  int fds[2];
  ASSERT_EQ(0, pipe(fds));

  const uint8_t data[] = {1, 2, 3, 4, 5, 6, 7, 8};
  ASSERT_EQ(3, write(fds[1], data, 3));

  // Supply the remainder only once the reader is blocked, so that the first read is short
  std::thread writer([&fds, &data]() {
    std::this_thread::sleep_for(std::chrono::milliseconds(50));
    EXPECT_EQ(5, write(fds[1], data + 3, 5));
  });

  uint8_t buf[8] = {0};
  krpc_error_t error = krpc_read(fds[0], buf, sizeof(buf));
  writer.join();
  close(fds[0]);
  close(fds[1]);

  ASSERT_EQ(KRPC_OK, error);
  for (size_t i = 0; i < sizeof(buf); i++) ASSERT_EQ(data[i], buf[i]);
}

TEST(test_communication, test_read_eof) {
  int fds[2];
  ASSERT_EQ(0, pipe(fds));
  close(fds[1]);

  uint8_t buf[4] = {0};
  krpc_error_t error = krpc_read(fds[0], buf, sizeof(buf));
  close(fds[0]);

  ASSERT_EQ(KRPC_ERROR_EOF, error);
}

#endif
