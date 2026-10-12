#pragma once

#include <chrono>              // NOLINT(build/c++11)
#include <condition_variable>  // NOLINT(build/c++11)
#include <cstdint>
#include <functional>
#include <map>
#include <memory>
#include <mutex>  // NOLINT(build/c++11)
#include <string>
#include <string_view>
#include <utility>
#include <vector>

#include "krpc/encoder.hpp"
#include "krpc/krpc.pb.hpp"

namespace krpc {

class Connection;
class StreamManager;
class StreamImpl;

class Client {
 public:
  Client();
  Client(const std::string& name, const std::string& address, unsigned int rpc_port = 50000,
         unsigned int stream_port = 50001,
         std::chrono::milliseconds timeout = std::chrono::milliseconds::zero());
  /** Perform the connection handshake over the given connections, which have been built
      but not yet opened. The handshake is the same whatever carries it. */
  Client(const std::string& name, std::shared_ptr<Connection> rpc_connection,
         const std::shared_ptr<Connection>& stream_connection);

  /** Invoke the calls of a request, and return the result of the first. The first call
      through a client with a stream connection fetches the server's list of services, to find
      the procedures that return a new stream or event. */
  schema::ProcedureResult invoke(const schema::Request& request);
  /** Invoke a call, as invoke(const schema::Request&) does. */
  schema::ProcedureResult invoke(const schema::ProcedureCall& call);
  /** Invoke a procedure, as invoke(const schema::Request&) does. */
  schema::ProcedureResult invoke(
      std::string_view service, std::string_view procedure,
      const std::vector<encoder::Value>& args = std::vector<encoder::Value>());

  /** The kind of value a procedure returns: a new stream as a KRPC.Stream message, a new event
      as a KRPC.Event message, or any other value. */
  enum class Returns : uint8_t { value, stream, event };
  /** Invoke a procedure that returns what returns says, as generated stubs do. The id of a
      new stream or event is noted with the stream manager. */
  schema::ProcedureResult invoke(std::string_view service, std::string_view procedure,
                                 const std::vector<encoder::Value>& args, Returns returns);

  schema::Request build_request(
      std::string_view service, std::string_view procedure,
      const std::vector<encoder::Value>& args = std::vector<encoder::Value>());
  schema::ProcedureCall build_call(
      std::string_view service, std::string_view procedure,
      const std::vector<encoder::Value>& args = std::vector<encoder::Value>());
  void add_exception_thrower(const std::string& service, const std::string& name,
                             const std::function<void(std::string)>& thrower);

 private:
  friend class StreamManager;
  friend class StreamImpl;
  void throw_exception(const schema::Error& error) const;
  /** Send a request, whose first call returns what returns says, or whose calls are looked up
      in the server's list of services if returns is null. */
  schema::ProcedureResult send_request(const schema::Request& request,
                                       const Returns* returns = nullptr);
  Returns lookup_returns(const std::string& service, const std::string& procedure);
  void list_returns();
  void note_stream_id(int index, Returns returns);
  void note_removed_streams(const schema::Request& request);
  static void add_arguments(schema::ProcedureCall* call, const std::vector<encoder::Value>& args);
  StreamManager& streams() const;

 public:
  /** Throw a StreamError if the client has no connection to the stream server. */
  void check_stream_connection() const;
  std::shared_ptr<StreamImpl> add_stream(const schema::ProcedureCall& call);
  std::shared_ptr<StreamImpl> get_stream(uint64_t id);
  void remove_stream(uint64_t id);

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
  std::shared_ptr<std::mutex> lock;
  // The request a call is built into, the bytes it is written as, and the response it is
  // answered by. Kept from one call to the next and guarded by lock, so that a call reuses the
  // allocations of the one before it: clearing a protobuf message keeps the storage its fields
  // have, and the buffer keeps its capacity.
  schema::Request request_buffer;
  std::string request_data;
  schema::Response response_buffer;
  // The procedures that return a new stream or event, from the server's list of services,
  // keyed by service and procedure name. Shared by every copy of the client and guarded by
  // lock.
  struct ListedReturns {
    bool listed = false;
    std::map<std::pair<std::string, std::string>, Returns> returns;
  };
  std::shared_ptr<ListedReturns> listed_returns;
  // Shared by every copy of the client, including the stream manager's own
  std::shared_ptr<std::map<std::pair<std::string, std::string>, std::function<void(std::string)>>>
      exception_throwers;
  // Guards exception_throwers. Services register their throwers as they are constructed,
  // which can happen on any thread and at any time, while errors are turned into exceptions
  // on both the calling thread and the stream update thread. Held by shared pointer as the
  // client is copyable and a mutex is not.
  std::shared_ptr<std::mutex> exception_throwers_lock;
  // Declared last, so that it is destroyed first. Ending the streams calls end callbacks,
  // which can use the rest of the client.
  std::shared_ptr<StreamManager> stream_manager;
};

}  // namespace krpc
