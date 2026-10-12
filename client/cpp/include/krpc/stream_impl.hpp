#pragma once

#include <condition_variable>  // NOLINT(build/c++11)
#include <cstdint>
#include <exception>
#include <functional>
#include <map>
#include <memory>
#include <mutex>  // NOLINT(build/c++11)
#include <string>
#include <thread>  // NOLINT(build/c++11)

namespace krpc {

class Client;

/** The state shared by every handle of one stream. It is changed under the stream's
    condition, and then the update lock, so holding either one is enough to read it. */
class StreamImpl {
 public:
  StreamImpl(Client* client, uint64_t id, std::shared_ptr<std::recursive_mutex> update_lock);
  Client* get_client() const;
  uint64_t get_id() const;
  void start();
  bool has_started() const;
  float rate() const;
  void set_rate(float value);
  /** A copy of the encoded value, and whether the value is null, read together. Throws the
      exception that ended the stream, if it has ended. */
  std::string get_data(bool* is_null);
  /** Store a value and wake the threads waiting for one. Returns false, storing nothing, if
      the stream has ended. */
  bool update_and_notify(const std::string& data, bool is_null);
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
  typedef std::function<void(const std::string&, bool)> Callback;
  typedef std::map<int, Callback> Callbacks;
  const Callbacks& get_callbacks() const;
  int add_callback(const Callback& callback);
  void remove_callback(int tag);
  /** Add an end callback. One added after the stream ended is called here. */
  int add_end_callback(const EndCallback& callback);
  void remove_end_callback(int tag);
  /** Remove the stream from the server, and end it for every handle. Does nothing if the
      stream has ended. */
  void remove();

 private:
  Client* client;
  uint64_t id;
  std::shared_ptr<std::recursive_mutex> update_lock;
  bool started;
  bool updated;
  std::string data;
  bool _is_null;
  uint64_t version;
  uint64_t fired;
  std::exception_ptr ending;
  std::condition_variable condition;
  std::mutex condition_mutex;
  // The number of handles holding the condition through acquire(), per thread. A thread
  // waiting on the condition keeps its holds while another thread takes the condition.
  mutable std::mutex holds_mutex;
  std::map<std::thread::id, int> holds;
  Callbacks callbacks;
  int next_callback_tag;
  EndCallbacks end_callbacks;
  int next_end_callback_tag;
  float _rate;
};

/** Call each end callback with the ending. A callback that throws is reported, and the
    rest are still called. */
void call_end_callbacks(const StreamImpl::EndCallbacks& callbacks,
                        const std::exception_ptr& ending);

}  // namespace krpc
