#pragma once

#include <atomic>
#include <condition_variable>  // NOLINT(build/c++11)
#include <cstdint>
#include <functional>
#include <map>
#include <memory>
#include <mutex>   // NOLINT(build/c++11)
#include <thread>  // NOLINT(build/c++11)

namespace krpc {

class Client;
class Connection;
class StreamImpl;
namespace schema {
class ProcedureCall;
class ProcedureResult;
}  // namespace schema

class StreamManager {
 public:
  StreamManager(Client* client, const std::shared_ptr<Connection>& connection);
  ~StreamManager();
  std::shared_ptr<StreamImpl> add_stream(const schema::ProcedureCall& call);
  std::shared_ptr<StreamImpl> get_stream(uint64_t id);
  void remove_stream(uint64_t id);
  /** Remove a stream from the server, and end it for every handle. Does nothing if the
      stream has already ended. */
  void remove_stream(StreamImpl* stream);
  void update(uint64_t id, const schema::ProcedureResult& result);
  std::condition_variable& get_update_condition();
  std::unique_lock<std::mutex>& get_update_condition_lock();
  typedef std::function<void()> Callback;
  typedef std::map<int, Callback> Callbacks;
  int add_update_callback(const Callback& callback);
  void remove_update_callback(int tag);
  /** End every stream, after the connection has closed and no further update can arrive. */
  void notify_closed();

 private:
  static void update_thread_main(StreamManager* stream_manager,
                                 const std::shared_ptr<Connection>& connection,
                                 const std::shared_ptr<std::atomic_bool>& stop);
  static void read_updates(StreamManager* stream_manager,
                           const std::shared_ptr<Connection>& connection,
                           const std::shared_ptr<std::atomic_bool>& stop);
  Client* client;
  std::shared_ptr<Connection> connection;
  std::map<uint64_t, std::weak_ptr<StreamImpl>> streams;
  std::shared_ptr<std::recursive_mutex> update_lock;
  std::shared_ptr<std::atomic_bool> stop;
  // Whether the connection has closed, after which a stream is ended as it is added
  bool closed;
  std::shared_ptr<std::thread> update_thread;
  std::condition_variable condition;
  std::mutex condition_mutex;
  std::unique_lock<std::mutex> condition_lock;
  Callbacks callbacks;
  int next_callback_tag;
};

}  // namespace krpc
