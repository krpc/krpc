#include <krpc_cnano/communication.h>

#include "gtest/gtest.h"

// The TCP transport is exercised over a socket pair, which behaves as a connected socket does
// without needing a server to connect to. Winsock has no equivalent, so these are POSIX only.
#if defined(KRPC_COMMUNICATION_TCP) && !defined(_WIN32)

#include <pthread.h>
#include <signal.h>
#include <sys/socket.h>
#include <sys/time.h>
#include <unistd.h>

#include <chrono>
#include <cstring>
#include <thread>

namespace {

volatile sig_atomic_t alarms = 0;

void count_alarm(int) { alarms++; }

}  // namespace

// A read that returns fewer bytes than requested must resume at the point the previous one
// finished, rather than restarting at the beginning of the buffer. A large response arrives in
// as many segments as the network chose to split it into, so this is the usual case rather than
// an unusual one.
TEST(test_communication, test_socket_read_partial) {
  int fds[2];
  ASSERT_EQ(0, socketpair(AF_UNIX, SOCK_STREAM, 0, fds));

  const uint8_t data[] = {1, 2, 3, 4, 5, 6, 7, 8};
  ASSERT_EQ(3, send(fds[1], data, 3, 0));

  // Supply the remainder only once the reader is blocked, so that the first read is short
  std::thread writer([&fds, &data]() {
    std::this_thread::sleep_for(std::chrono::milliseconds(50));
    EXPECT_EQ(5, send(fds[1], data + 3, 5, 0));
  });

  uint8_t buf[8] = {0};
  krpc_error_t error = krpc_read(fds[0], buf, sizeof(buf));
  writer.join();
  close(fds[0]);
  close(fds[1]);

  ASSERT_EQ(KRPC_OK, error);
  for (size_t i = 0; i < sizeof(buf); i++) ASSERT_EQ(data[i], buf[i]);
}

TEST(test_communication, test_socket_read_eof) {
  int fds[2];
  ASSERT_EQ(0, socketpair(AF_UNIX, SOCK_STREAM, 0, fds));
  close(fds[1]);

  uint8_t buf[4] = {0};
  krpc_error_t error = krpc_read(fds[0], buf, sizeof(buf));
  close(fds[0]);

  ASSERT_EQ(KRPC_ERROR_EOF, error);
}

// Writing to a socket whose peer has gone must report the failure. Left to itself it raises
// SIGPIPE, whose default action ends the program before the caller is told anything, so this
// test crashes rather than fails when the signal is not suppressed.
TEST(test_communication, test_socket_write_peer_closed) {
  int fds[2];
  ASSERT_EQ(0, socketpair(AF_UNIX, SOCK_STREAM, 0, fds));
  ASSERT_EQ(0, close(fds[1]));

  const uint8_t data[] = {1, 2, 3, 4};
  krpc_error_t error = krpc_write(fds[0], data, sizeof(data));
  close(fds[0]);

  ASSERT_EQ(KRPC_ERROR_IO, error);
}

// A signal delivered while a read is blocked interrupts it, and the read has to carry on from
// where it stopped: reporting a failure would leave the rest of the message on the socket for
// the next call to read as its own.
TEST(test_communication, test_socket_read_interrupted) {
  int fds[2];
  ASSERT_EQ(0, socketpair(AF_UNIX, SOCK_STREAM, 0, fds));

  const uint8_t data[] = {1, 2, 3, 4, 5, 6, 7, 8};
  ASSERT_EQ(3, send(fds[1], data, 3, 0));

  // A handler with no SA_RESTART, which is what leaves an interrupted read to be resumed by the
  // program rather than by the system
  struct sigaction action;
  struct sigaction previous_action;
  std::memset(&action, 0, sizeof(action));
  action.sa_handler = &count_alarm;
  sigemptyset(&action.sa_mask);
  action.sa_flags = 0;
  alarms = 0;
  ASSERT_EQ(0, sigaction(SIGALRM, &action, &previous_action));

  // The signal goes to a thread that is not blocking it, so it is blocked here for as long as it
  // takes to start the writer, leaving this thread, the one that blocks in the read, to take it
  sigset_t alarm_signal;
  sigset_t previous_mask;
  sigemptyset(&alarm_signal);
  sigaddset(&alarm_signal, SIGALRM);
  ASSERT_EQ(0, pthread_sigmask(SIG_BLOCK, &alarm_signal, &previous_mask));
  std::thread writer([&fds, &data]() {
    std::this_thread::sleep_for(std::chrono::milliseconds(200));
    EXPECT_EQ(5, send(fds[1], data + 3, 5, 0));
  });
  ASSERT_EQ(0, pthread_sigmask(SIG_SETMASK, &previous_mask, nullptr));

  // Well before the writer supplies the rest, so that the read is interrupted while blocked
  struct itimerval timer;
  std::memset(&timer, 0, sizeof(timer));
  timer.it_value.tv_usec = 50000;
  ASSERT_EQ(0, setitimer(ITIMER_REAL, &timer, nullptr));

  uint8_t buf[8] = {0};
  krpc_error_t error = krpc_read(fds[0], buf, sizeof(buf));
  writer.join();
  sigaction(SIGALRM, &previous_action, nullptr);
  close(fds[0]);
  close(fds[1]);

  ASSERT_EQ(KRPC_OK, error);
  ASSERT_GT(alarms, 0);
  for (size_t i = 0; i < sizeof(buf); i++) ASSERT_EQ(data[i], buf[i]);
}

#endif
