#pragma once

#include <atomic>
#include <condition_variable>  // NOLINT(build/c++11)
#include <cstdint>
#include <exception>
#include <functional>
#include <map>
#include <memory>
#include <mutex>  // NOLINT(build/c++11)
#include <stdexcept>
#include <string>
#include <thread>  // NOLINT(build/c++11)
#include <utility>

namespace krpc {

class Client;
class StreamManager;

/** The number of holds each thread has on a condition. */
class Holds {
 public:
  /** Whether the calling thread has a hold. */
  bool held() const;
  void add();
  /** Remove a hold of the calling thread. Returns true if it was the thread's last. */
  bool remove();

 private:
  mutable std::mutex mutex;
  std::map<std::thread::id, int> counts;
};

/** A value that failed to decode for a callback. */
class CallbackDecodeError : public std::runtime_error {
 public:
  using std::runtime_error::runtime_error;
};

/** The state shared by every handle of one stream. The value, its counts and the ending change
    with the stream's condition and the update lock both held, so holding either one is enough
    to read them. The callbacks and the firings given to them change under the update lock
    alone. */
class StreamImpl {
 public:
  StreamImpl(Client* client, uint64_t id, std::shared_ptr<std::recursive_mutex> update_lock,
             std::weak_ptr<StreamManager> manager = std::weak_ptr<StreamManager>());
  Client* get_client() const;
  uint64_t get_id() const;
  float rate() const;
  void set_rate(float value);
  /** A copy of the encoded value, and whether the value is null, read together. Throws the
      exception that ended the stream, if it has ended. */
  std::string get_data(bool* is_null);
  typedef std::function<void(const std::string&, bool)> Callback;
  /** A callback, and the version of the last value it was called with, or for an event
      callback the number of firings when it was last called. Its mutex is held while it runs,
      so that it runs on one thread at a time and in the order of the values. */
  struct CallbackEntry {
    CallbackEntry(Callback callback, bool event) : callback(std::move(callback)), event(event) {}
    Callback callback;
    // Whether the callback is an event callback, which is called only with firings
    bool event;
    uint64_t delivered = 0;
    std::recursive_mutex mutex;
  };
  typedef std::map<int, std::shared_ptr<CallbackEntry>> Callbacks;
  /** Store a value, with the update lock held. Returns false, storing nothing, if the stream
      has ended. */
  bool store(const std::string& data, bool is_null);
  /** Store a value and wake the threads waiting for one, with the condition held. Returns
      false, storing nothing, if the stream has ended. Otherwise sets callbacks to those to
      call with the value, version to its version, and fired to the number of firings. */
  bool update_and_notify(const std::string& data, bool is_null, Callbacks* callbacks,
                         uint64_t* version, uint64_t* fired);
  /** Call each callback with a value, with no stream lock held. A callback already called
      with this version or a later one is skipped, and an event callback already called with
      this number of firings or more, as is every callback once the stream has ended. A
      callback that throws is reported, and the rest are still called. */
  void call_callbacks(const Callbacks& callbacks, const std::string& data, bool is_null,
                      uint64_t version, uint64_t fired);
  bool has_updated() const;
  /** The number of values received. */
  uint64_t get_version() const;
  /** The number of true values received, which for an event is its firings. */
  uint64_t get_fired() const;
  bool has_ended() const;
  /** Throws the exception that ended the stream, if it has ended. */
  void throw_if_ended() const;
  typedef std::function<void(std::exception_ptr)> EndCallback;
  typedef std::map<int, EndCallback> EndCallbacks;
  /** End the stream and wake its waiters. Returns the end callbacks to call, with no lock
      held, or none if the stream had already ended. */
  EndCallbacks end(const std::exception_ptr& ending);
  /** End the stream, with the condition held. */
  EndCallbacks end_held(const std::exception_ptr& ending);
  /** Call fn with a lock that holds the condition. A thread that has acquired the condition
      through any handle keeps its hold, and any other thread takes the condition for the
      call. */
  void with_condition(const std::function<void(std::unique_lock<std::mutex>&)>& fn);
  /** Wait on the condition, which lock holds, until done returns true or the timeout in
      seconds passes. A negative timeout waits indefinitely. Returns false on a timeout, and
      throws the ending if the stream ends first. */
  bool wait(std::unique_lock<std::mutex>* lock, double timeout, const std::function<bool()>& done);
  /** Whether the calling thread holds the condition through acquire(), on any handle. */
  bool is_held() const;
  /** Record a hold on the condition by the calling thread, through acquire(). */
  void add_hold();
  /** Remove a hold on the condition by the calling thread. Returns true if it was the
      thread's last hold, which the caller then unlocks. */
  bool remove_hold();
  std::condition_variable& get_condition();
  std::mutex& get_condition_mutex();
  /** Add a callback. One added to a stream with a value is called with it here. */
  int add_callback(const Callback& callback);
  /** Add an event callback. It is called here, once, if the event has not ended and has a
      firing that neither the callbacks nor a wait that has seen the given number of firings
      have been given. */
  int add_event_callback(const Callback& callback, uint64_t seen);
  void remove_callback(int tag);
  /** Add an end callback. One added after the stream ended is called here. */
  int add_end_callback(const EndCallback& callback);
  void remove_end_callback(int tag);
  /** Remove the stream from the server, and end it for every handle. Does nothing if the
      stream has ended. */
  void remove();

 private:
  /** Call a callback with a value, unless it has had this one or a later one, or the stream
      has ended. The count is the value's version, or for an event callback the number of
      firings. An event callback is called only with a firing. The first call, made by the
      thread adding the callback, is skipped if the callback is running. */
  void deliver(CallbackEntry* entry, const std::string& data, bool is_null, uint64_t count,
               bool first = false);
  Client* client;
  uint64_t id;
  std::shared_ptr<std::recursive_mutex> update_lock;
  std::weak_ptr<StreamManager> manager;
  // Set after the value it flags, and read with no lock
  std::atomic<bool> updated;
  std::string data;
  bool _is_null;
  uint64_t version;
  uint64_t fired;
  // The number of firings when firings were last given to the event callbacks
  uint64_t callbacks_fired;
  // Whether a value that failed to decode for a callback has been reported
  std::atomic<bool> decode_failure_reported;
  std::exception_ptr ending;
  std::condition_variable condition;
  std::mutex condition_mutex;
  // The number of handles holding the condition through acquire(), per thread. A thread
  // waiting on the condition keeps its holds while another thread takes the condition.
  Holds holds;
  Callbacks callbacks;
  int next_callback_tag;
  EndCallbacks end_callbacks;
  int next_end_callback_tag;
  std::atomic<float> _rate;
};

/** Call each end callback with the ending. A callback that throws is reported, and the
    rest are still called. */
void call_end_callbacks(const StreamImpl::EndCallbacks& callbacks,
                        const std::exception_ptr& ending);

}  // namespace krpc
