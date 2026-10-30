#include <gtest/gtest-message.h>
#include <gtest/gtest-test-part.h>

#include <chrono>              // NOLINT(build/c++11)
#include <condition_variable>  // NOLINT(build/c++11)
#include <future>
#include <mutex>  // NOLINT(build/c++11)
#include <stdexcept>
#include <system_error>
#include <thread>  // NOLINT(build/c++11)

#include "gtest/gtest.h"
#include "krpc/condition.hpp"

// Whether another thread can lock the mutex within the duration. It unlocks it straight away.
template <typename Duration>
static bool locks_elsewhere(krpc::RecursiveMutex* mutex, Duration duration) {
  return std::async(std::launch::async,
                    [mutex, duration] {
                      if (!mutex->try_lock_for(duration)) return false;
                      mutex->unlock();
                      return true;
                    })
      .get();
}

TEST(test_condition, test_timed_try_by_the_holder_is_reentrant) {
  krpc::RecursiveMutex mutex;
  mutex.lock();
  ASSERT_TRUE(mutex.try_lock_for(std::chrono::milliseconds(10)));
  ASSERT_TRUE(mutex.try_lock_until(std::chrono::steady_clock::now()));
  mutex.unlock();
  mutex.unlock();
  ASSERT_FALSE(locks_elsewhere(&mutex, std::chrono::milliseconds(10)));
  mutex.unlock();
  ASSERT_TRUE(locks_elsewhere(&mutex, std::chrono::milliseconds(10)));
}

TEST(test_condition, test_held_by_this_thread) {
  krpc::RecursiveMutex mutex;
  ASSERT_FALSE(mutex.held_by_this_thread());
  mutex.lock();
  ASSERT_TRUE(mutex.held_by_this_thread());
  ASSERT_FALSE(
      std::async(std::launch::async, [&mutex] { return mutex.held_by_this_thread(); }).get());
  mutex.unlock();
  ASSERT_FALSE(mutex.held_by_this_thread());
}

TEST(test_condition, test_unlock_by_another_thread_throws) {
  krpc::RecursiveMutex mutex;
  ASSERT_THROW(mutex.unlock(), std::system_error);
  mutex.lock();
  ASSERT_THROW(std::async(std::launch::async, [&mutex] { mutex.unlock(); }).get(),
               std::system_error);
  mutex.unlock();
}

TEST(test_condition, test_timed_tries) {
  krpc::RecursiveMutex mutex;
  mutex.lock();
  auto start = std::chrono::steady_clock::now();
  ASSERT_FALSE(locks_elsewhere(&mutex, std::chrono::milliseconds(50)));
  ASSERT_GE(std::chrono::steady_clock::now() - start, std::chrono::milliseconds(50));
  // Gets the mutex once the holder unlocks it within the duration
  auto other = std::async(std::launch::async, [&mutex] {
    if (!mutex.try_lock_for(std::chrono::seconds(10))) return false;
    mutex.unlock();
    return true;
  });
  std::this_thread::sleep_for(std::chrono::milliseconds(20));
  mutex.unlock();
  ASSERT_TRUE(other.get());
  ASSERT_TRUE(mutex.try_lock_until(std::chrono::steady_clock::now()));
  mutex.unlock();
}

TEST(test_condition, test_wait_for_times_out) {
  krpc::RecursiveMutex mutex;
  krpc::Condition condition(mutex);
  std::unique_lock<krpc::RecursiveMutex> lock(mutex);
  ASSERT_EQ(std::cv_status::timeout, condition.wait_for(std::chrono::milliseconds(10)));
  ASSERT_FALSE(condition.wait_for(lock, std::chrono::milliseconds(10), [] { return false; }));
  ASSERT_TRUE(condition.wait_until(lock, std::chrono::steady_clock::now(), [] { return true; }));
  ASSERT_TRUE(mutex.held_by_this_thread());
}

TEST(test_condition, test_wait_without_the_mutex_throws) {
  krpc::RecursiveMutex mutex;
  krpc::Condition condition(mutex);
  ASSERT_THROW(condition.wait(), std::system_error);
}

// A clock that cannot be read, so a wait for one of its deadlines throws
struct unreadable_clock {
  typedef std::chrono::steady_clock::duration duration;
  typedef duration::rep rep;
  typedef duration::period period;
  typedef std::chrono::time_point<unreadable_clock> time_point;
  static constexpr bool is_steady = false;
  static time_point now() { throw std::runtime_error("the clock cannot be read"); }
};

TEST(test_condition, test_wait_that_throws_takes_every_level_again) {
  krpc::RecursiveMutex mutex;
  krpc::Condition condition(mutex);
  mutex.lock();
  mutex.lock();
  ASSERT_THROW(condition.wait_until(unreadable_clock::time_point()), std::runtime_error);
  mutex.unlock();
  ASSERT_FALSE(locks_elsewhere(&mutex, std::chrono::milliseconds(10)));
  mutex.unlock();
  ASSERT_TRUE(locks_elsewhere(&mutex, std::chrono::milliseconds(10)));
}
