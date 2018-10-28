#pragma once

#include <google/protobuf/stubs/port.h>

#include <condition_variable>  // NOLINT(build/c++11)
#include <functional>
#include <map>
#include <memory>
#include <mutex>  // NOLINT(build/c++11)
#include <string>
#include <utility>
#include <vector>

#include "krpc/krpc.pb.hpp"

namespace krpc {

class Connection;
class StreamManager;
class StreamImpl;

class Client {
 public:
  Client();
  Client(const std::string& name, const std::string& address,
         unsigned int rpc_port = 50000, unsigned int stream_port = 50001);

  std::string invoke(const schema::Request& request);
  std::string invoke(const schema::ProcedureCall& call);
  std::string invoke(
    const std::string& service, const std::string& procedure,
    const std::vector<std::string>& args = std::vector<std::string>());

  schema::Request build_request(
    const std::string& service, const std::string& procedure,
    const std::vector<std::string>& args = std::vector<std::string>());
  schema::ProcedureCall build_call(
    const std::string& service, const std::string& procedure,
    const std::vector<std::string>& args = std::vector<std::string>());
  void add_exception_thrower(const std::string& service, const std::string& name,
                             const std::function<void(std::string)>& thrower);

 private:
  friend class StreamManager;
  void throw_exception(const schema::Error& error) const;

 public:
  std::shared_ptr<StreamImpl> add_stream(const schema::ProcedureCall& call);
  std::shared_ptr<StreamImpl> get_stream(google::protobuf::uint64 id);
  void remove_stream(google::protobuf::uint64 id);
  void freeze_streams();
  void thaw_streams();

  /**
   * Condition variable that is notified when a stream update
   * message has finished being processed.
   */
  std::condition_variable& get_stream_update_condition() const;
  /** Lock used with the condition variable */
  std::unique_lock<std::mutex>& get_stream_update_condition_lock() const;
  /** Acquire a lock on the condition variable */
  void acquire_stream_update();
  /** Release the lock on the condition variable */
  void release_stream_update();
  /** Wait until the next stream update message. If timeout >= 0, the
      operation times out after that many seconds. */
  void wait_for_stream_update(double timeout = -1);
  typedef std::function<void()> Callback;
  /**
   * Add a callback that is invoked whenever a stream update message has
   * finished being processed.
   * Returns an integer tag for the callback which uniquely identifies it,
   * and allows it to be removed using remove_stream_update_callback()
   */
  int add_stream_update_callback(const Callback& callback);
  /** Remove a callback, based on its tag */
  void remove_stream_update_callback(int tag);

 private:
  std::shared_ptr<Connection> rpc_connection;
  std::shared_ptr<StreamManager> stream_manager;
  std::shared_ptr<std::mutex> lock;
  std::map<std::pair<std::string, std::string>,
           std::function<void(std::string)>> exception_throwers;
};

}  // namespace krpc
