#pragma once

#include <atomic>
#include <cstdint>
#include <exception>
#include <functional>
#include <map>
#include <memory>
#include <mutex>  // NOLINT(build/c++11)
#include <set>
#include <string>
#include <thread>  // NOLINT(build/c++11)
#include <vector>

#include "krpc/condition.hpp"
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
  /** Create a manager and start its update thread. The clients share the pointer returned.
      Releasing its last copy closes the manager, which the update thread keeps alive until it
      exits. Uses a copy of the client, so that the manager outlives the client it is built by.
      A null connection starts no update thread. */
  static std::shared_ptr<StreamManager> create(Client* client,
                                               const std::shared_ptr<Connection>& connection);
  ~StreamManager();
  StreamManager(const StreamManager&) = delete;
  StreamManager& operator=(const StreamManager&) = delete;
  std::shared_ptr<StreamImpl> add_stream(const schema::ProcedureCall& call);
  std::shared_ptr<StreamImpl> get_stream(uint64_t id);
  /** Note a stream id the server has given this client. Called in the order the server's
      replies arrive. */
  void note_issued(uint64_t id);
  /** Remove a stream from the server. A stream wrapped in a handle ends for every handle. */
  void remove_stream(uint64_t id);
  /** Note a stream id the server has removed for this client, whose kept results and later
      results are dropped. Called in the order the server's replies arrive. */
  void note_removed(uint64_t id);
  /** Remove a stream from the server, and end it for every handle. Does nothing if the
      stream has already ended. */
  void remove_stream(StreamImpl* stream);
  /** Apply a stream update message, then call the update callbacks. */
  void update(const schema::StreamUpdate& message);
  Condition& get_update_condition();
  std::unique_lock<RecursiveMutex>& get_update_condition_lock();
  void acquire_update();
  void release_update();
  typedef std::function<void()> Callback;
  typedef std::map<int, Callback> Callbacks;
  int add_update_callback(const Callback& callback);
  void remove_update_callback(int tag);
  /** Note that the client is closing the connection. The update thread then stops trying to
      take a stream's condition, and the closing thread ends the stream. */
  void request_close();
  bool is_close_requested() const;
  /** Whether this thread holds the update condition or the condition of a stream. */
  bool holds_a_condition();
  /** End every stream, once the connection is closing and no further update is applied.
      Called by the closing thread and by the update thread.

      Each pass ends the streams whose condition it can take, and the passes repeat until every
      stream has ended. A thread can hold one stream's condition while it waits on another, and
      ending the other releases the first. The update thread leaves the remaining streams to
      the closing thread. */
  void notify_closed(bool on_update_thread = false);

 private:
  StreamManager(Client* client, const std::shared_ptr<Connection>& connection);
  static void update_thread_main(const std::shared_ptr<StreamManager>& stream_manager);
  void read_updates();
  /** Stop the update thread and end every stream, on the thread releasing the last client. */
  void close();
  /** A stream with its decoded value or ending. */
  struct Decoded {
    std::shared_ptr<StreamImpl> stream;
    std::string data;
    bool is_null;
    std::exception_ptr ending;
  };
  /** Decode a result. A final result moves the stream from the registry to the streams to
      end, as the server has already removed it. Called with the update lock held. */
  Decoded decode(const std::shared_ptr<StreamImpl>& stream, const schema::ProcedureResult& result);
  /** Store decoded values and endings, wake their waiters and call their callbacks. Called with
      no lock held. */
  void apply(const std::vector<Decoded>& decoded);
  /** Keep a result for a stream not registered yet. Called with the update lock held. */
  void keep(uint64_t id, const schema::ProcedureResult& result);
  /** Wake the threads waiting for a stream update. */
  void notify_update(bool on_update_thread);
  void try_end(const std::shared_ptr<StreamImpl>& stream, const std::exception_ptr& ending);
  /** End a stream out of the registry, and release its condition. */
  void end(const std::shared_ptr<StreamImpl>& stream, const std::exception_ptr& ending);
  /** Lock a stream's condition, or the update condition, on the update thread. Returns false,
      without it, once the client is closing, as the closing thread can hold it while it joins
      the update thread. */
  bool lock(RecursiveMutex* mutex);
  /** End a stream the server has ended. When the update thread stops trying to take its
      condition, the closing thread ends it. */
  void end_on_update_thread(const std::shared_ptr<StreamImpl>& stream,
                            const std::exception_ptr& ending);
  /** The exception an error is thrown as. */
  std::exception_ptr error_ptr(const schema::Error& error) const;
  // A copy of the client that built the manager, which streams and the values they decode
  // use. Its stream manager is this one, not owned.
  std::unique_ptr<Client> client;
  std::weak_ptr<StreamManager> self;
  std::shared_ptr<Connection> connection;
  std::shared_ptr<std::recursive_mutex> update_lock;
  RecursiveMutex condition_mutex;
  Condition condition;
  std::unique_lock<RecursiveMutex> condition_lock;
  // The streams the server is sending results for. A stream whose handles are all gone is
  // erased once it is found.
  std::map<uint64_t, std::weak_ptr<StreamImpl>> streams;
  Callbacks callbacks;
  int next_callback_tag;
  // Whether the connection has closed, after which a stream is ended as it is added
  std::atomic_bool closed;
  // Whether a thread is closing the connection
  std::atomic_bool close_requested;
  // Streams out of the registry and not yet ended, with the ending for each. The closing
  // thread ends any whose condition the update thread stops trying to take.
  std::map<std::shared_ptr<StreamImpl>, std::exception_ptr> to_end;
  // Every registered stream, to find one whose condition the closing thread holds
  std::vector<std::weak_ptr<StreamImpl>> registered;
  // The highest stream id the server has given this client, and those of its ids not yet
  // registered. A result for one of these, or for a higher id, can arrive before its stream
  // is registered, and is kept until it is. Only the latest value and an ending are kept, as
  // a stream registered later shows nothing else.
  int64_t issued;
  std::set<uint64_t> unregistered;
  struct Pending {
    std::shared_ptr<schema::ProcedureResult> value;
    std::shared_ptr<schema::ProcedureResult> ending;
  };
  std::map<uint64_t, Pending> pending;
  std::thread update_thread;
};

}  // namespace krpc
