#pragma once

#include <chrono>  // NOLINT(build/c++11)
#include <cstdint>
#include <functional>
#include <map>
#include <memory>
#include <mutex>  // NOLINT(build/c++11)
#include <string>
#include <string_view>
#include <utility>
#include <vector>

#include "krpc/condition.hpp"
#include "krpc/encoder.hpp"
#include "krpc/krpc.pb.hpp"

namespace krpc {

class Connection;
class StreamManager;
class StreamImpl;
namespace function_stream_detail {
struct function_type_cache;
}  // namespace function_stream_detail

class Client {
 public:
  Client();
  Client(const std::string& name, const std::string& address, unsigned int port = 50000,
         std::chrono::milliseconds timeout = std::chrono::milliseconds::zero(),
         std::chrono::milliseconds handshake_timeout = std::chrono::seconds(5),
         std::chrono::milliseconds stall_timeout = std::chrono::seconds(30));
  /** Perform the connection handshake over the given connection, which has been built but not
      yet opened. The handshake is the same whatever carries it. Gives up after
      handshake_timeout of waiting for the server to accept the connection, or waits
      indefinitely if it is zero. Fails the connection when a message from the server receives
      no new byte for stall_timeout once it has started to arrive, or never if it is zero. */
  Client(const std::string& name, const std::shared_ptr<Connection>& connection,
         std::chrono::milliseconds handshake_timeout = std::chrono::seconds(5),
         std::chrono::milliseconds stall_timeout = std::chrono::seconds(30));

  /** Invoke the calls of a request, and return the result of the first. The first call fetches
      the server's list of services, to find the procedures that return a new stream or event.
      A failed fetch is not retried. */
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
  /** Invoke a procedure that returns what returns says, as generated stubs do. A new stream or
      event is registered with the stream manager. */
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
  friend struct function_stream_detail::function_type_cache;
  void throw_exception(const schema::Error& error) const;
  /** Send a request, whose first call returns what returns says, or whose calls are looked up
      in the server's list of services if returns is null. */
  schema::ProcedureResult send_request(schema::Request request, const Returns* returns = nullptr);
  using ReturnsByProcedure = std::map<std::pair<std::string, std::string>, Returns>;
  Returns lookup_returns(const std::string& service, const std::string& procedure);
  ReturnsByProcedure list_returns();
  static void add_arguments(schema::ProcedureCall* call, const std::vector<encoder::Value>& args);
  StreamManager& streams() const;

  /** The return type of a server side function, if stored. A code of NONE is no value. */
  bool get_function_return_type(uint64_t id, schema::Type* type) const;
  /** Store the return type of a server side function. */
  void set_function_return_type(uint64_t id, const schema::Type& type);
  /** The parameter types of a server side function, if stored. */
  bool get_function_parameter_types(uint64_t id, std::vector<schema::Type>* types) const;
  /** Store the parameter types of a server side function. */
  void set_function_parameter_types(uint64_t id, const std::vector<schema::Type>& types);

 public:
  /** Throw a StreamError if the client is not connected to a server. */
  void check_stream_connection() const;
  std::shared_ptr<StreamImpl> add_stream(const schema::ProcedureCall& call);
  std::shared_ptr<StreamImpl> get_stream(uint64_t id);

  /**
   * Condition variable that is notified when a stream update
   * message has finished being processed.
   */
  Condition& get_stream_update_condition() const;
  /** Lock on the condition's mutex, which is re-entrant. Every copy of the client shares the
      lock, so it is used by one thread. */
  std::unique_lock<RecursiveMutex>& get_stream_update_condition_lock() const;
  /** Lock the condition's mutex */
  void acquire_stream_update();
  /** Unlock the condition's mutex */
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
  // Guards listed_returns
  std::shared_ptr<std::mutex> lock;
  // The procedures that return a new stream or event, from the server's list of services,
  // keyed by service and procedure name. Shared by every copy of the client and guarded by
  // lock.
  struct ListedReturns {
    bool listed = false;
    ReturnsByProcedure returns;
  };
  std::shared_ptr<ListedReturns> listed_returns;
  // Shared by every copy of the client, including the stream manager's own
  std::shared_ptr<std::map<std::pair<std::string, std::string>, std::function<void(std::string)>>>
      exception_throwers;
  // Guards exception_throwers. Services register their throwers as they are constructed,
  // which can happen on any thread and at any time, while errors are turned into exceptions
  // on both the calling thread and the reader. Held by shared pointer as the
  // client is copyable and a mutex is not.
  std::shared_ptr<std::mutex> exception_throwers_lock;
  std::map<uint64_t, schema::Type> function_return_types;
  std::map<uint64_t, std::vector<schema::Type>> function_parameter_types;
  // Guards function_return_types and function_parameter_types
  std::shared_ptr<std::mutex> function_types_lock;
  // Declared last, so that it is destroyed first. Ending the streams calls end callbacks,
  // which can use the rest of the client.
  std::shared_ptr<StreamManager> stream_manager;
};

}  // namespace krpc
