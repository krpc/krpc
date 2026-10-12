#pragma once

#include <atomic>
#include <cstdint>
#include <deque>
#include <exception>
#include <functional>
#include <map>
#include <memory>
#include <mutex>  // NOLINT(build/c++11)
#include <stdexcept>
#include <string>
#include <utility>
#include <vector>

#include "krpc/condition.hpp"

namespace krpc {

class Client;
class StreamManager;

/** A value that failed to decode for a callback. */
class CallbackDecodeError : public std::runtime_error {
 public:
  using std::runtime_error::runtime_error;
};

/** The state shared by every handle of one stream. It is changed under the stream's condition,
    and then the update lock. */
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
  /** A value callback, or an event callback that is called only with firings. The version is
      that of the last value passed to a value callback, or the firing count last passed to an
      event callback. The lock is held while the callback runs, so it runs on one thread at a
      time. While the adding thread makes the first call, the update thread queues its values
      in missed for the adding thread to pass on. */
  struct CallbackEntry {
    CallbackEntry(int tag, Callback callback, bool event)
        : tag(tag), callback(std::move(callback)), event(event) {}
    struct Value {
      uint64_t version = 0;
      std::string data;
      bool is_null = false;
    };
    int tag;
    Callback callback;
    bool event;
    uint64_t version = 0;
    RecursiveMutex lock;
    bool adding = false;
    std::deque<Value> missed;
  };
  typedef std::vector<std::shared_ptr<CallbackEntry>> Callbacks;
  /** Store a value, with the condition held. */
  void set_value(const std::string& data, bool is_null);
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
  /** Wait on the condition, which the calling thread holds, until done returns true or the
      timeout in seconds passes. A negative timeout waits indefinitely. Returns false on a
      timeout, and throws the ending if the stream ends first. */
  bool wait(double timeout, const std::function<bool()>& done);
  Condition& get_condition();
  RecursiveMutex& get_condition_mutex();
  /** The callbacks, read under the update lock. */
  Callbacks get_callbacks() const;
  /** Call a callback with a value, unless it has been passed a newer one, has been removed, or
      the stream has ended. The version is the firing count for an event callback, which is
      passed only firings. Called with no lock held.

      The first call, from the adding thread, skips the value if the update thread is calling
      the callback, as that thread has a newer value. The update thread queues its values for
      the adding thread while the first call runs. */
  void call(const std::shared_ptr<CallbackEntry>& callback, uint64_t version,
            const std::string& data, bool is_null, bool added = false);
  /** Add a callback. One added to a stream with a value is called with it here. */
  int add_callback(const Callback& callback);
  /** Add an event callback. It is called here for a firing passed neither to a wait that has
      seen the given number of firings, nor to the event's callbacks. */
  int add_event_callback(const Callback& callback, uint64_t seen);
  /** Reserve the latest firing for a late event callback, if it has been passed neither to a
      wait that has seen the given number of firings, nor to the event's callbacks, nor to a
      replay in flight. Returns the firing count reserved, or zero for none. */
  uint64_t reserve_replay(uint64_t seen);
  /** Pass a reserved firing to a late event callback, and release the reservation. Called
      with no lock held. */
  void replay(const std::shared_ptr<CallbackEntry>& callback, uint64_t fired);
  void remove_callback(int tag);
  /** Add an end callback. One added after the stream ended is called here. */
  int add_end_callback(const EndCallback& callback);
  void remove_end_callback(int tag);
  /** Remove the stream from the server, and end it for every handle. Does nothing if the
      stream has ended. */
  void remove();

 private:
  /** Take a callback's lock on the update thread. Returns false, without the lock, once the
      value is queued for the adding thread or the client is closing. */
  bool enter_on_update_thread(CallbackEntry* callback, uint64_t version, const std::string& data,
                              bool is_null);
  /** Call a callback, on the adding thread, with the values the update thread queued during
      its first call. A value queued before the stream ended is still passed on. Clears adding
      in the same hold of the update lock that finds the queue empty, so the update thread
      queues no value after the last check. Called with the callback's lock held. */
  void call_missed(CallbackEntry* callback);
  /** Note that a callback is passed a version. Returns false, noting nothing, if the callback
      has been removed or passed that version or a newer one. Called with the update lock
      held. */
  bool note_passed(CallbackEntry* callback, uint64_t version);
  void invoke(const CallbackEntry& callback, const std::string& data, bool is_null);
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
  // The highest firing count passed to an event callback
  uint64_t callbacks_fired;
  // The firing count a replay in flight passes, so that a concurrent late event callback does
  // not replay it too
  uint64_t replaying;
  // Whether a value that failed to decode for a callback has been reported
  std::atomic<bool> decode_failure_reported;
  std::exception_ptr ending;
  RecursiveMutex condition_mutex;
  Condition condition;
  std::map<int, std::shared_ptr<CallbackEntry>> callbacks;
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
