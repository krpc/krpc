#pragma once

#include <condition_variable>  // NOLINT(build/c++11)
#include <functional>
#include <mutex>  // NOLINT(build/c++11)

#include "krpc/stream.hpp"

namespace krpc {

class Client;
namespace schema {
class Event;
}

class Event {
 public:
  Event();
  Event(Client * client, const krpc::schema::Event& message);
  /** Start the event */
  void start();
  /** Condition variable that is notified when the event occurs */
  std::condition_variable& get_condition() const;
  /** Lock used with the condition variable */
  std::unique_lock<std::mutex>& get_condition_lock() const;
  /** Acquire a lock on the condition variable */
  void acquire();
  /** Release the lock on the condition variable */
  void release();
  /** Wait until the event occurs. If timeout >= 0, the
      operation times out after that many seconds. */
  void wait(double timeout = -1);
  typedef std::function<void()> Callback;
  /**
   * Add a callback that is invoked whenever the event occurs.
   * Returns an integer tag for the callback which uniquely identifies it,
   * and allows it to be removed using remove_callback()
   */
  int add_callback(const Callback& callback);
  /** Remove a callback, based on its tag */
  void remove_callback(int tag);
  /** Remove the event from the server */
  void remove();
  /** Returns the underlying stream for the event */
  Stream<bool> stream() const;
  bool operator==(const Event& rhs) const;
  bool operator!=(const Event& rhs) const;
  explicit operator bool() const;

 private:
  Stream<bool> _stream;
};

}  // namespace krpc
