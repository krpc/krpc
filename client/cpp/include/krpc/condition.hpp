#pragma once

#include <chrono>              // NOLINT(build/c++11)
#include <condition_variable>  // NOLINT(build/c++11)
#include <cstddef>
#include <mutex>  // NOLINT(build/c++11)
#include <system_error>
#include <thread>  // NOLINT(build/c++11)
#include <utility>

namespace krpc {

/** A mutex that the thread holding it can lock again, and that records which thread holds it.
    Each lock is matched by an unlock. */
class RecursiveMutex {
 public:
  RecursiveMutex() = default;
  RecursiveMutex(const RecursiveMutex&) = delete;
  RecursiveMutex& operator=(const RecursiveMutex&) = delete;
  ~RecursiveMutex() = default;
  void lock();
  bool try_lock();
  template <typename Rep, typename Period>
  bool try_lock_for(const std::chrono::duration<Rep, Period>& duration);
  template <typename Clock, typename Duration>
  bool try_lock_until(const std::chrono::time_point<Clock, Duration>& deadline);
  /** Throws std::system_error if the calling thread does not hold the mutex. */
  void unlock();
  /** Whether the calling thread holds the mutex. */
  bool held_by_this_thread() const;

 private:
  friend class Condition;
  /** Release every level of the calling thread's hold, with state locked. Returns the number
      of levels. */
  std::size_t release_all(const std::unique_lock<std::mutex>& state_lock);
  /** Take the mutex at the given number of levels, with state locked. */
  void restore(std::unique_lock<std::mutex>* state_lock, std::size_t levels);
  /** Release every level of the calling thread's hold, with state locked, and take them all
      again when destroyed, also when a wait throws. */
  class Released {
   public:
    Released(RecursiveMutex* mutex, std::unique_lock<std::mutex>* state_lock)
        : mutex(mutex), state_lock(state_lock), levels(mutex->release_all(*state_lock)) {}
    Released(const Released&) = delete;
    Released& operator=(const Released&) = delete;
    ~Released() { mutex->restore(state_lock, levels); }

   private:
    RecursiveMutex* mutex;
    std::unique_lock<std::mutex>* state_lock;
    std::size_t levels;
  };
  mutable std::mutex state;
  std::condition_variable unlocked;
  std::thread::id owner;
  std::size_t depth = 0;
};

/** A condition variable over a RecursiveMutex. A wait releases every level of the calling
    thread's hold, and takes them all again before it returns. The calling thread must hold the
    mutex. A wait can return without a notification, so it is called in a loop that checks for
    the change waited for. */
class Condition {
 public:
  explicit Condition(RecursiveMutex& mutex) : mutex(mutex) {}
  Condition(const Condition&) = delete;
  Condition& operator=(const Condition&) = delete;
  ~Condition() = default;
  void wait();
  template <typename Rep, typename Period>
  std::cv_status wait_for(const std::chrono::duration<Rep, Period>& duration);
  template <typename Clock, typename Duration>
  std::cv_status wait_until(const std::chrono::time_point<Clock, Duration>& deadline);
  // The overloads taking a lock follow std::condition_variable. The wait needs only that the
  // calling thread holds the mutex, through that lock or another.
  void wait(const std::unique_lock<RecursiveMutex>& lock);
  template <typename Predicate>
  void wait(const std::unique_lock<RecursiveMutex>& lock, Predicate done);
  template <typename Rep, typename Period>
  std::cv_status wait_for(const std::unique_lock<RecursiveMutex>& lock,
                          const std::chrono::duration<Rep, Period>& duration);
  template <typename Rep, typename Period, typename Predicate>
  bool wait_for(const std::unique_lock<RecursiveMutex>& lock,
                const std::chrono::duration<Rep, Period>& duration, Predicate done);
  template <typename Clock, typename Duration>
  std::cv_status wait_until(const std::unique_lock<RecursiveMutex>& lock,
                            const std::chrono::time_point<Clock, Duration>& deadline);
  template <typename Clock, typename Duration, typename Predicate>
  bool wait_until(const std::unique_lock<RecursiveMutex>& lock,
                  const std::chrono::time_point<Clock, Duration>& deadline, Predicate done);
  void notify_one();
  void notify_all();

 private:
  RecursiveMutex& mutex;
  std::condition_variable condition;
};

inline void RecursiveMutex::lock() {
  std::unique_lock<std::mutex> state_lock(state);
  auto self = std::this_thread::get_id();
  if (owner != self) {
    unlocked.wait(state_lock, [this] { return depth == 0; });
    owner = self;
  }
  depth++;
}

inline bool RecursiveMutex::try_lock() {
  std::lock_guard<std::mutex> state_lock(state);
  auto self = std::this_thread::get_id();
  if (owner != self) {
    if (depth > 0) return false;
    owner = self;
  }
  depth++;
  return true;
}

template <typename Rep, typename Period>
inline bool RecursiveMutex::try_lock_for(const std::chrono::duration<Rep, Period>& duration) {
  return try_lock_until(std::chrono::steady_clock::now() + duration);
}

template <typename Clock, typename Duration>
inline bool RecursiveMutex::try_lock_until(
    const std::chrono::time_point<Clock, Duration>& deadline) {
  std::unique_lock<std::mutex> state_lock(state);
  auto self = std::this_thread::get_id();
  if (owner != self) {
    if (!unlocked.wait_until(state_lock, deadline, [this] { return depth == 0; })) return false;
    owner = self;
  }
  depth++;
  return true;
}

inline void RecursiveMutex::unlock() {
  std::lock_guard<std::mutex> state_lock(state);
  if (depth == 0 || owner != std::this_thread::get_id())
    throw std::system_error(std::make_error_code(std::errc::operation_not_permitted),
                            "krpc::RecursiveMutex unlocked by a thread that does not hold it");
  if (--depth > 0) return;
  owner = std::thread::id();
  unlocked.notify_one();
}

inline bool RecursiveMutex::held_by_this_thread() const {
  std::lock_guard<std::mutex> state_lock(state);
  return depth > 0 && owner == std::this_thread::get_id();
}

inline std::size_t RecursiveMutex::release_all(const std::unique_lock<std::mutex>& /*state_lock*/) {
  if (depth == 0 || owner != std::this_thread::get_id())
    throw std::system_error(std::make_error_code(std::errc::operation_not_permitted),
                            "krpc::Condition waited on by a thread that does not hold its mutex");
  auto levels = depth;
  depth = 0;
  owner = std::thread::id();
  unlocked.notify_one();
  return levels;
}

inline void RecursiveMutex::restore(std::unique_lock<std::mutex>* state_lock, std::size_t levels) {
  unlocked.wait(*state_lock, [this] { return depth == 0; });
  owner = std::this_thread::get_id();
  depth = levels;
}

// The waiter holds the mutex's state from releasing the mutex until it waits, so a thread that
// takes the mutex to change what the waiter checks notifies only once the waiter is waiting
inline void Condition::wait() {
  std::unique_lock<std::mutex> state_lock(mutex.state);
  RecursiveMutex::Released released(&mutex, &state_lock);
  condition.wait(state_lock);
}

template <typename Rep, typename Period>
inline std::cv_status Condition::wait_for(const std::chrono::duration<Rep, Period>& duration) {
  return wait_until(std::chrono::steady_clock::now() + duration);
}

template <typename Clock, typename Duration>
inline std::cv_status Condition::wait_until(
    const std::chrono::time_point<Clock, Duration>& deadline) {
  std::unique_lock<std::mutex> state_lock(mutex.state);
  RecursiveMutex::Released released(&mutex, &state_lock);
  return condition.wait_until(state_lock, deadline);
}

inline void Condition::wait(const std::unique_lock<RecursiveMutex>& /*lock*/) { wait(); }

template <typename Predicate>
inline void Condition::wait(const std::unique_lock<RecursiveMutex>& /*lock*/, Predicate done) {
  while (!done()) wait();
}

template <typename Rep, typename Period>
inline std::cv_status Condition::wait_for(const std::unique_lock<RecursiveMutex>& /*lock*/,
                                          const std::chrono::duration<Rep, Period>& duration) {
  return wait_for(duration);
}

template <typename Rep, typename Period, typename Predicate>
inline bool Condition::wait_for(const std::unique_lock<RecursiveMutex>& lock,
                                const std::chrono::duration<Rep, Period>& duration,
                                Predicate done) {
  return wait_until(lock, std::chrono::steady_clock::now() + duration, std::move(done));
}

template <typename Clock, typename Duration>
inline std::cv_status Condition::wait_until(
    const std::unique_lock<RecursiveMutex>& /*lock*/,
    const std::chrono::time_point<Clock, Duration>& deadline) {
  return wait_until(deadline);
}

template <typename Clock, typename Duration, typename Predicate>
inline bool Condition::wait_until(const std::unique_lock<RecursiveMutex>& /*lock*/,
                                  const std::chrono::time_point<Clock, Duration>& deadline,
                                  Predicate done) {
  while (!done()) {
    if (wait_until(deadline) == std::cv_status::timeout) return done();
  }
  return true;
}

namespace detail {

// Only the thread holding the mutex can unlock it, so a lock destroyed or reassigned on another
// thread leaves the mutex held
inline void release_if_held_elsewhere(std::unique_lock<RecursiveMutex>* lock) {
  if (lock->owns_lock() && !lock->mutex()->held_by_this_thread()) lock->release();
}

}  // namespace detail

inline void Condition::notify_one() {
  std::lock_guard<std::mutex> state_lock(mutex.state);
  condition.notify_one();
}

inline void Condition::notify_all() {
  std::lock_guard<std::mutex> state_lock(mutex.state);
  condition.notify_all();
}

}  // namespace krpc
