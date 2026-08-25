#include <krpc_cnano/communication.h>

#include "gtest/gtest.h"

// The serial transport reads and writes its port through the file API, which a pipe answers to
// in the same way, so these need no serial port to run against.
#if defined(KRPC_COMMUNICATION_WINDOWS)

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <windows.h>

#include <chrono>
#include <thread>

// A serial port carries the RPC and stream connections over the one link, so the messages sent
// over it are wrapped in a multiplexed message saying which connection they belong to. Which
// framing a build uses is decided by the preprocessor, and the wrong choice shows up only in
// what a server makes of the bytes, which needs a serial port to run against.
#ifndef KRPC_MULTIPLEXED
#error "The Windows serial transport must be built with the multiplexed framing"
#endif

// A read that returns fewer bytes than requested must resume at the point the previous one
// finished, and not restart at the beginning of the buffer.
TEST(test_communication, test_read_partial) {
  HANDLE read_end = INVALID_HANDLE_VALUE;
  HANDLE write_end = INVALID_HANDLE_VALUE;
  ASSERT_TRUE(CreatePipe(&read_end, &write_end, nullptr, 0));

  const uint8_t data[] = {1, 2, 3, 4, 5, 6, 7, 8};
  DWORD written = 0;
  ASSERT_TRUE(WriteFile(write_end, data, 3, &written, nullptr));
  ASSERT_EQ(3u, written);

  // Supply the remainder only once the reader is blocked, so that the first read is short
  std::thread writer([write_end, &data]() {
    std::this_thread::sleep_for(std::chrono::milliseconds(50));
    DWORD more = 0;
    EXPECT_TRUE(WriteFile(write_end, data + 3, 5, &more, nullptr));
    EXPECT_EQ(5u, more);
  });

  uint8_t buf[8] = {0};
  krpc_error_t error = krpc_read(read_end, buf, sizeof(buf));
  writer.join();
  CloseHandle(read_end);
  CloseHandle(write_end);

  ASSERT_EQ(KRPC_OK, error);
  for (size_t i = 0; i < sizeof(buf); i++) ASSERT_EQ(data[i], buf[i]);
}

// A read whose data can never arrive has to fail, and not hand back whatever the buffer
// already held. A pipe reports the far end closing as a failed read, where a serial port, whose
// far end is hardware, reports no data.
TEST(test_communication, test_read_peer_closed) {
  HANDLE read_end = INVALID_HANDLE_VALUE;
  HANDLE write_end = INVALID_HANDLE_VALUE;
  ASSERT_TRUE(CreatePipe(&read_end, &write_end, nullptr, 0));
  CloseHandle(write_end);

  uint8_t buf[4] = {0};
  krpc_error_t error = krpc_read(read_end, buf, sizeof(buf));
  CloseHandle(read_end);

  ASSERT_EQ(KRPC_ERROR_IO, error);
}

#endif
