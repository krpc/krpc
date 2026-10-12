#pragma once

#include <cstdint>
#include <exception>
#include <functional>
#include <mutex>  // NOLINT(build/c++11)

#include "krpc/condition.hpp"
#include "krpc/stream.hpp"

namespace krpc {

class Client;
namespace schema {
class Event;
}

class Event {
 public:
  Event();
  Event(Client* client, const krpc::schema::Event& message);
  /** Condition variable that is notified when the event occurs. Every copy of the event object
      shares it. */
  Condition& get_condition() const;
  /** Lock on the condition's mutex. Each event object has its own lock, used by one thread,
      and the mutex is re-entrant. */
  std::unique_lock<RecursiveMutex>& get_condition_lock() const;
  /** Lock the condition's mutex, which every copy of the event object shares */
  void acquire();
  /** Unlock the condition's mutex, which every copy of the event object shares */
  void release();
  /** Wait until the event occurs. Returns straight away if the event has occurred since the
      last wait on this event object. If timeout >= 0, the operation times out after that many
      seconds. Returns true if the event occurred, and false on a timeout. Throws the
      exception that ended the event, if it ends before or during the wait. */
  bool wait(double timeout = -1);
  typedef std::function<void()> Callback;
  /**
   * Add a callback that is invoked whenever the event occurs. It is invoked straight away if
   * the event has not ended, has occurred since the last wait on this event object, and no
   * other callback has been invoked for it. An error from the event does not invoke the
   * callback. Waiting on the event throws it.
   * Returns an integer tag for the callback which uniquely identifies it,
   * and allows it to be removed using remove_callback()
   */
  int add_callback(const Callback& callback);
  /** Remove a callback, based on its tag */
  void remove_callback(int tag);
  typedef std::function<void(std::exception_ptr)> EndCallback;
  /**
   * Add a callback that is invoked once when the event ends, with the exception that ended
   * it. It is invoked straight away if the event has already ended. Returns an integer tag
   * for the callback, which allows it to be removed using remove_end_callback()
   */
  int add_end_callback(const EndCallback& callback);
  /** Remove an end callback, based on its tag */
  void remove_end_callback(int tag);
  /** Remove the event from the server */
  void remove();
  /** Returns the underlying stream for the event */
  Stream<bool> stream() const;
  bool operator==(const Event& rhs) const;
  bool operator!=(const Event& rhs) const;
  explicit operator bool() const;

 private:
  Stream<bool> _stream;
  // The number of firings when a wait on this event object last returned
  uint64_t seen = 0;
};

}  // namespace krpc
