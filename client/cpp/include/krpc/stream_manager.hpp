#pragma once

#include <atomic>
#include <condition_variable>  // NOLINT(build/c++11)
#include <cstdint>
#include <exception>
#include <functional>
#include <map>
#include <memory>
#include <mutex>  // NOLINT(build/c++11)
#include <set>
#include <thread>  // NOLINT(build/c++11)
#include <vector>

#include "krpc/stream_impl.hpp"

namespace krpc {

class Client;
class Connection;
namespace schema {
class Error;
class ProcedureCall;
class ProcedureResult;
}  // namespace schema

class StreamManager {
 public:
  /** Create a manager and start its update thread. The clients share the pointer returned.
      Releasing its last copy closes the manager, which the update thread keeps alive until it
      exits. Uses a copy of the client, so that the manager outlives the client it is built by. */
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
  void update(uint64_t id, const schema::ProcedureResult& result);
  std::condition_variable& get_update_condition();
  std::unique_lock<std::mutex>& get_update_condition_lock();
  /** Lock the update condition through get_update_condition_lock(). */
  void acquire_update();
  /** Unlock the update condition through get_update_condition_lock(). */
  void release_update();
  /** The holds each thread has on the conditions of the streams. */
  Holds& stream_holds();
  typedef std::function<void()> Callback;
  typedef std::map<int, Callback> Callbacks;
  int add_update_callback(const Callback& callback);
  void remove_update_callback(int tag);
  /** End every stream, after the connection has closed and no further update can arrive.
      Called on the update thread. */
  void notify_closed();

 private:
  StreamManager(Client* client, const std::shared_ptr<Connection>& connection);
  static void update_thread_main(const std::shared_ptr<StreamManager>& stream_manager);
  void read_updates();
  /** Stop the update thread and end every stream, on the thread releasing the last client. */
  void close();
  /** End every stream with the connection closed. Gives up on the streams left once a close
      has started, if give_up_on_close is true. */
  void end_streams(bool give_up_on_close);
  /** Wake the threads waiting for an update. */
  void notify_update();
  /** Remove a stream from the registry. Returns false if it is not registered. */
  bool unregister(const StreamImpl* stream);
  /** The exception an error is thrown as. */
  std::exception_ptr error_ptr(const schema::Error& error) const;
  // A copy of the client that built the manager, which streams and the values they decode
  // use. Its stream manager is this one, not owned.
  std::unique_ptr<Client> client;
  std::weak_ptr<StreamManager> self;
  std::shared_ptr<Connection> connection;
  std::map<uint64_t, std::weak_ptr<StreamImpl>> streams;
  // The highest stream id the server has given this client, and those of its ids not yet
  // registered. A result for one of these, or for a higher id, can arrive before its stream
  // is registered, and is kept until it is.
  int64_t issued;
  std::set<uint64_t> unregistered;
  // The latest value and the ending kept for a stream not registered yet. A result after an
  // ending is dropped.
  struct Pending {
    std::shared_ptr<schema::ProcedureResult> value;
    std::shared_ptr<schema::ProcedureResult> ending;
  };
  std::map<uint64_t, Pending> pending;
  std::shared_ptr<std::recursive_mutex> update_lock;
  // Whether a close has started
  std::atomic_bool stop;
  // Whether the connection has closed, after which a stream is ended as it is added
  bool closed;
  std::thread update_thread;
  Holds holds;
  std::condition_variable condition;
  std::mutex condition_mutex;
  std::unique_lock<std::mutex> condition_lock;
  // The thread holding the update condition through acquire_update()
  std::atomic<std::thread::id> condition_owner;
  Callbacks callbacks;
  int next_callback_tag;
};

}  // namespace krpc
