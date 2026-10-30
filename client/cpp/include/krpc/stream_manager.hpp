#pragma once

#include <atomic>
#include <cstdint>
#include <exception>
#include <functional>
#include <map>
#include <memory>
#include <mutex>  // NOLINT(build/c++11)
#include <string>
#include <vector>

#include "krpc/condition.hpp"
#include "krpc/session.hpp"
#include "krpc/stream_impl.hpp"

namespace krpc {

class Client;
class Connection;
namespace schema {
class Error;
class ProcedureCall;
class ProcedureResult;
class StreamUpdate;
}  // namespace schema

class StreamManager {
 public:
  /** Create a manager and start the connection's reader and dispatcher threads, once the
      handshake has been made on the connection. The clients share the pointer returned.
      Releasing its last copy closes the connection. Uses a copy of the client, so that the
      manager outlives the client it is built by. */
  static std::shared_ptr<StreamManager> create(Client* client,
                                               const std::shared_ptr<Connection>& connection,
                                               std::chrono::milliseconds stall_timeout);
  ~StreamManager();
  StreamManager(const StreamManager&) = delete;
  StreamManager& operator=(const StreamManager&) = delete;
  Session& get_session() { return *session; }
  std::shared_ptr<StreamImpl> add_stream(const schema::ProcedureCall& call);
  std::shared_ptr<StreamImpl> get_stream(uint64_t id);
  /** Register a stream the server has added for this client. Called by the reader, before it
      reads the stream's first update. */
  void register_stream(uint64_t id);
  /** Whether any stream is open. Read without the update lock, by the session deciding
      whether to read between calls. */
  bool has_streams() const { return open_streams; }
  /** Whether a stream is registered for the id. */
  bool is_registered(uint64_t id);
  /** Remove a stream from the server once its last handle is gone. Called by the stream as it
      is destroyed. */
  void drop_stream(uint64_t id);
  /** Remove a stream from the server, and end it for every handle. Does nothing if the
      stream has already ended. */
  void remove_stream(StreamImpl* stream);
  /** Store the values and endings of a stream update, and queue the work of waking waiters
      and calling callbacks with the dispatcher. Called by the reader. */
  void update(const schema::StreamUpdate& message);
  /** Store one update and wait for the dispatcher thread to apply it. For the tests, which
      stand in for the reader. */
  void update_and_wait(const schema::StreamUpdate& message);
  Condition& get_update_condition();
  std::unique_lock<RecursiveMutex>& get_update_condition_lock();
  void acquire_update();
  void release_update();
  typedef std::function<void()> Callback;
  typedef std::map<int, Callback> Callbacks;
  int add_update_callback(const Callback& callback);
  void remove_update_callback(int tag);
  /** Note that the client is closing the connection. The dispatcher thread then stops trying
      to take a stream's condition, and the closing thread wakes the stream's waiters. */
  void request_close();
  bool is_close_requested() const;
  /** Whether the connection has closed and every stream has ended. */
  bool is_closed() const;
  /** Whether this thread holds the update condition or the condition of a stream. */
  bool holds_a_condition();
  /** End every stream, once the connection is closing and no further update is applied.
      Called by the closing thread, and by the dispatcher thread when the connection fails,
      with the error that ended it.

      The waiters of a stream whose condition another thread holds are woken once that thread
      lets go of it. */
  void notify_closed(const std::exception_ptr& cause = nullptr);

 private:
  StreamManager(Client* client, const std::shared_ptr<Connection>& connection,
                std::chrono::milliseconds stall_timeout);
  /** Close the connection and end every stream, on the thread releasing the last client. */
  void close();
  /** A change for the dispatcher to apply to a stream: a new value, or an ending. */
  struct Change {
    std::shared_ptr<StreamImpl> stream;
    StreamImpl::Callbacks callbacks;
    uint64_t version = 0;
    uint64_t fired = 0;
    std::string data;
    bool is_null = false;
    StreamImpl::EndCallbacks end_callbacks;
    std::exception_ptr ending;
  };
  /** Wake the waiters of streams with a new value or an ending, and call their callbacks. Runs
      on the dispatcher thread, with no lock held. */
  void apply(const std::vector<Change>& changes, const Callbacks& update_callbacks);
  /** Wake the threads waiting for a stream update. */
  void notify_update(bool on_dispatcher);
  /** Lock a stream's condition, or the update condition, on the dispatcher thread. Returns
      false, without it, once the client is closing, as the closing thread can hold it while it
      joins the dispatcher thread. */
  bool lock(RecursiveMutex* mutex);
  /** The exception an error is thrown as. */
  std::exception_ptr error_ptr(const schema::Error& error) const;
  // A copy of the client that built the manager, which streams and the values they decode
  // use. Its stream manager is this one, not owned.
  std::unique_ptr<Client> client;
  std::weak_ptr<StreamManager> self;
  std::unique_ptr<Session> session;
  std::shared_ptr<std::recursive_mutex> update_lock;
  RecursiveMutex condition_mutex;
  Condition condition;
  std::unique_lock<RecursiveMutex> condition_lock;
  // The streams the server is sending results for
  std::map<uint64_t, std::weak_ptr<StreamImpl>> streams;
  // The streams the reader registered that no handle has claimed yet, kept alive until one
  // does
  std::map<uint64_t, std::shared_ptr<StreamImpl>> unclaimed;
  Callbacks callbacks;
  int next_callback_tag;
  // Whether streams holds any stream, for reading without the update lock
  std::atomic_bool open_streams{false};
  // Whether the connection has closed, after which a stream is ended as it is added
  std::atomic_bool closed;
  // Whether a thread is closing the connection
  std::atomic_bool close_requested;
  // Every registered stream, to find one whose condition the closing thread holds
  std::vector<std::weak_ptr<StreamImpl>> registered;
};

}  // namespace krpc
