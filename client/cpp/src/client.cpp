#include "krpc/client.hpp"

#include <exception>
#include <locale>
#include <memory>
#include <sstream>
#include <stdexcept>
#include <string>
#include <string_view>
#include <utility>
#include <vector>

#include "krpc/connection.hpp"
#include "krpc/decoder.hpp"
#include "krpc/encoder.hpp"
#include "krpc/error.hpp"
#include "krpc/session.hpp"
#include "krpc/stream_manager.hpp"

namespace krpc {

class StreamImpl;

namespace {

constexpr unsigned int PROTOCOL_VERSION = 2;

/** A duration as a number of seconds, e.g. "5 seconds" or "0.5 seconds". */
std::string format_seconds(std::chrono::milliseconds duration) {
  std::ostringstream out;
  out.imbue(std::locale::classic());
  out << static_cast<double>(duration.count()) / 1000.0
      << (duration == std::chrono::seconds(1) ? " second" : " seconds");
  return out.str();
}

/** Send the connection request, and check the server's response to it. */
void handshake(Connection* connection, const std::string& name, std::chrono::milliseconds timeout) {
  schema::Message request;
  request.mutable_connection_request()->set_client_name(name);
  request.mutable_connection_request()->set_protocol_version(PROTOCOL_VERSION);
  connection->send(encoder::encode_message_with_size(request));
  schema::Message message;
  try {
    // The stall timeout applies from the next message
    connection->receive_message(message, timeout, std::chrono::milliseconds::zero());
  } catch (const TimeoutError&) {
    throw ConnectionError("The server did not answer the connection request within " +
                          format_seconds(timeout));
  } catch (const EncodingError&) {
    throw ConnectionError("Failed to parse the server's response to the connection request");
  } catch (const std::exception& exn) {
    std::throw_with_nested(ConnectionError(std::string("Connection failed: ") + exn.what()));
  }
  if (message.has_disconnect()) {
    const auto& disconnect = message.disconnect();
    auto reason = schema::Disconnect::Reason_Name(disconnect.reason());
    auto text = "The server ended the connection (" + reason + ")";
    if (!disconnect.message().empty()) text += ": " + disconnect.message();
    throw ConnectionError(text, "", reason);
  }
  if (!message.has_connection_response())
    throw ConnectionError(
        message.content_case() == schema::Message::CONTENT_NOT_SET
            ? "Expected a response to the connection request, got a message of an unknown kind"
            : "Expected a response to the connection request, got another message");
  const auto& response = message.connection_response();
  if (response.status() != schema::ConnectionResponse::OK) {
    auto status = schema::ConnectionResponse::Status_Name(response.status());
    auto text = "The server refused the connection (" + status + ")";
    if (!response.message().empty()) text += ": " + response.message();
    throw ConnectionError(text, status);
  }
  if (response.protocol_version() != PROTOCOL_VERSION)
    throw ConnectionError("The server does not support protocol version " +
                          std::to_string(PROTOCOL_VERSION) +
                          ". Use a client from the same kRPC release as the server.");
}

}  // namespace

Client::Client()
    : lock(new std::mutex),
      listed_returns(std::make_shared<ListedReturns>()),
      exception_throwers(std::make_shared<decltype(exception_throwers)::element_type>()),
      exception_throwers_lock(new std::mutex),
      function_types_lock(new std::mutex) {}

Client::Client(const std::string& name, const std::string& address, unsigned int port,
               std::chrono::milliseconds timeout, std::chrono::milliseconds handshake_timeout)
    : Client(name, std::make_shared<Connection>(address, port, timeout), handshake_timeout) {}

Client::Client(const std::string& name, const std::shared_ptr<Connection>& connection,
               std::chrono::milliseconds handshake_timeout)
    : Client() {
  if (handshake_timeout.count() < 0)
    throw std::invalid_argument("The handshake timeout must not be negative");
  try {
    connection->connect();
  } catch (const ConnectionError&) {
    throw;
  } catch (const std::exception& exn) {
    std::throw_with_nested(
        ConnectionError(std::string("Failed to connect to the server: ") + exn.what()));
  }
  try {
    handshake(connection.get(), name, handshake_timeout);
  } catch (...) {
    connection->close();
    throw;
  }
  stream_manager = StreamManager::create(this, connection);
}

schema::ProcedureResult Client::invoke(const schema::Request& request) {
  return this->send_request(request);
}

schema::ProcedureResult Client::invoke(const schema::ProcedureCall& call) {
  schema::Request request;
  // Copied because the call is received by const reference and a Request owns its calls, so it
  // cannot be moved in. CopyFrom also stays correct if ProcedureCall gains fields.
  request.add_calls()->CopyFrom(call);
  return this->send_request(std::move(request));
}

schema::ProcedureResult Client::invoke(std::string_view service, std::string_view procedure,
                                       const std::vector<encoder::Value>& args) {
  return this->send_request(build_request(service, procedure, args));
}

schema::ProcedureResult Client::invoke(std::string_view service, std::string_view procedure,
                                       const std::vector<encoder::Value>& args, Returns returns) {
  return this->send_request(build_request(service, procedure, args), &returns);
}

// Send a request and return the result it is answered by. The reader registers a stream or an
// event a call adds before it reads the stream's first update.
schema::ProcedureResult Client::send_request(schema::Request request, const Returns* returns) {
  check_stream_connection();
  std::vector<Session::Adds> adds;
  auto to_adds = [](Returns value) {
    return value == Returns::stream  ? Session::Adds::stream
           : value == Returns::event ? Session::Adds::event
                                     : Session::Adds::nothing;
  };
  if (returns != nullptr) {
    adds.push_back(to_adds(*returns));
  } else {
    // Looked up before the request is sent, as listing the services makes a call of its own
    for (const auto& call : request.calls())
      adds.push_back(to_adds(lookup_returns(call.service(), call.procedure())));
  }
  auto response = stream_manager->get_session().call(std::move(request), std::move(adds));

  if (response.has_error()) throw_exception(response.error());

  schema::ProcedureResult result = std::move(*response.mutable_results(0));

  if (result.has_error()) throw_exception(result.error());

  return result;
}

// What a procedure returns, from the server's list of services, which is fetched by the first
// call.
Client::Returns Client::lookup_returns(const std::string& service, const std::string& procedure) {
  bool listed;
  {
    std::lock_guard<std::mutex> lock_guard(*lock);
    listed = listed_returns->listed;
  }
  // Fetched without the lock held, so threads racing on the first call each fetch the list
  ReturnsByProcedure fetched;
  if (!listed) fetched = list_returns();
  std::lock_guard<std::mutex> lock_guard(*lock);
  // Listed once, even if the listing fails
  if (!listed_returns->listed) {
    listed_returns->listed = true;
    listed_returns->returns = std::move(fetched);
  }
  auto returns = listed_returns->returns.find(std::make_pair(service, procedure));
  return returns == listed_returns->returns.end() ? Returns::value : returns->second;
}

// The procedures the server lists that return a new stream or event
Client::ReturnsByProcedure Client::list_returns() {
  ReturnsByProcedure result;
  schema::Response response;
  try {
    response = stream_manager->get_session().call(build_request("KRPC", "GetServices"), {});
  } catch (const ConnectionError&) {
    return result;
  }
  if (response.has_error() || response.results_size() == 0 || response.results(0).has_error())
    return result;
  schema::Services services;
  if (!services.ParseFromString(response.results(0).value())) return result;
  for (const auto& service : services.services()) {
    for (const auto& procedure : service.procedures()) {
      auto code = procedure.return_type().code();
      if (code != schema::Type::STREAM && code != schema::Type::EVENT) continue;
      result[std::make_pair(service.name(), procedure.name())] =
          code == schema::Type::EVENT ? Returns::event : Returns::stream;
    }
  }
  return result;
}

void Client::add_arguments(schema::ProcedureCall* call, const std::vector<encoder::Value>& args) {
  for (unsigned int i = 0; i < args.size(); i++) {
    schema::Argument* arg = call->add_arguments();
    arg->set_position(i);
    // A null argument is signaled by is_null with the value left unset.
    if (args[i].is_null)
      arg->set_is_null(true);
    else
      arg->set_value(args[i].value);
  }
}

schema::Request Client::build_request(std::string_view service, std::string_view procedure,
                                      const std::vector<encoder::Value>& args) {
  schema::Request request;
  schema::ProcedureCall* call = request.add_calls();
  call->set_service(service);
  call->set_procedure(procedure);
  add_arguments(call, args);
  return request;
}

schema::ProcedureCall Client::build_call(std::string_view service, std::string_view procedure,
                                         const std::vector<encoder::Value>& args) {
  schema::ProcedureCall call;
  call.set_service(service);
  call.set_procedure(procedure);
  add_arguments(&call, args);
  return call;
}

void Client::add_exception_thrower(const std::string& service, const std::string& name,
                                   const std::function<void(std::string)>& thrower) {
  std::lock_guard<std::mutex> guard(*exception_throwers_lock);
  (*exception_throwers)[std::make_pair(service, name)] = thrower;
}

bool Client::get_function_return_type(uint64_t id, schema::Type* type) const {
  std::lock_guard<std::mutex> guard(*function_types_lock);
  auto entry = function_return_types.find(id);
  if (entry == function_return_types.end()) return false;
  *type = entry->second;
  return true;
}

void Client::set_function_return_type(uint64_t id, const schema::Type& type) {
  std::lock_guard<std::mutex> guard(*function_types_lock);
  function_return_types[id] = type;
}

bool Client::get_function_parameter_types(uint64_t id, std::vector<schema::Type>* types) const {
  std::lock_guard<std::mutex> guard(*function_types_lock);
  auto entry = function_parameter_types.find(id);
  if (entry == function_parameter_types.end()) return false;
  *types = entry->second;
  return true;
}

void Client::set_function_parameter_types(uint64_t id, const std::vector<schema::Type>& types) {
  std::lock_guard<std::mutex> guard(*function_types_lock);
  function_parameter_types[id] = types;
}

void Client::throw_exception(const schema::Error& error) const {
  std::string message = error.description();
  if (!error.stack_trace().empty()) message += "\nServer stack trace:\n" + error.stack_trace();
  if (!error.service().empty() && !error.name().empty()) {
    auto key = std::make_pair(error.service(), error.name());
    std::unique_lock<std::mutex> guard(*exception_throwers_lock);
    auto thrower = exception_throwers->find(key);
    if (thrower == exception_throwers->end()) {
      // The type is unknown here if the generated header for its service was never
      // instantiated in this translation unit, so nothing registered a thrower for it.
      // Report the error itself, named by its type on the server.
      throw RPCError(error.service() + "." + error.name() + ": " + message);
    }
    // Copy the thrower and release the lock before calling it: it throws, and it must not
    // be holding this lock while the exception propagates out through the caller.
    auto fn = thrower->second;
    guard.unlock();
    fn(message);
  } else {
    throw RPCError(message);
  }
}

void Client::check_stream_connection() const {
  if (!stream_manager) throw StreamError("Not connected to a server");
}

StreamManager& Client::streams() const {
  check_stream_connection();
  return *stream_manager;
}

std::shared_ptr<StreamImpl> Client::add_stream(const schema::ProcedureCall& call) {
  return streams().add_stream(call);
}

std::shared_ptr<StreamImpl> Client::get_stream(uint64_t id) { return streams().get_stream(id); }

Condition& Client::get_stream_update_condition() const { return streams().get_update_condition(); }

std::unique_lock<RecursiveMutex>& Client::get_stream_update_condition_lock() const {
  return streams().get_update_condition_lock();
}

void Client::acquire_stream_update() { streams().acquire_update(); }

void Client::release_stream_update() { streams().release_update(); }

void Client::wait_for_stream_update(double timeout) {
  // Set before the close wakes the waiters, so a wait that starts after the wake returns
  if (streams().is_closed()) return;
  if (timeout < 0) {
    streams().get_update_condition().wait();
  } else {
    auto rel_time = std::chrono::milliseconds(static_cast<int>(timeout * 1000));
    streams().get_update_condition().wait_for(rel_time);
  }
}

int Client::add_stream_update_callback(const Callback& callback) {
  return streams().add_update_callback(callback);
}

void Client::remove_stream_update_callback(int tag) { streams().remove_update_callback(tag); }

}  // namespace krpc
