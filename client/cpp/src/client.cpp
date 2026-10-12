#include "krpc/client.hpp"

#include <memory>
#include <string>
#include <string_view>
#include <utility>
#include <vector>

#include "krpc/connection.hpp"
#include "krpc/decoder.hpp"
#include "krpc/encoder.hpp"
#include "krpc/error.hpp"
#include "krpc/stream_manager.hpp"

namespace krpc {

class StreamImpl;

Client::Client()
    : lock(new std::mutex),
      listed_returns(std::make_shared<ListedReturns>()),
      exception_throwers(std::make_shared<decltype(exception_throwers)::element_type>()),
      exception_throwers_lock(new std::mutex) {}

Client::Client(const std::string& name, const std::string& address, unsigned int rpc_port,
               unsigned int stream_port, std::chrono::milliseconds timeout)
    : Client(name, std::make_shared<Connection>(address, rpc_port, timeout),
             stream_port == 0 ? nullptr
                              : std::make_shared<Connection>(address, stream_port, timeout)) {}

Client::Client(const std::string& name, std::shared_ptr<Connection> rpc,
               const std::shared_ptr<Connection>& stream)
    : Client() {
  // Connect to RPC server
  rpc_connection = std::move(rpc);
  rpc_connection->connect();
  schema::ConnectionRequest request;
  request.set_type(schema::ConnectionRequest::RPC);
  request.set_client_name(name);
  rpc_connection->send(encoder::encode_message_with_size(request));
  schema::ConnectionResponse response;
  rpc_connection->receive_message(response);
  if (response.status() != schema::ConnectionResponse::OK)
    throw ConnectionError(response.message());

  // Connect to Stream server. Only once the rpc connection has been accepted, so that a
  // rejected connection does not leave a second one behind.
  if (stream) {
    stream->connect();
    schema::ConnectionRequest request;
    request.set_type(schema::ConnectionRequest::STREAM);
    request.set_client_identifier(response.client_identifier());
    stream->send(encoder::encode_message_with_size(request));
    schema::ConnectionResponse response;
    stream->receive_message(response);
    if (response.status() != schema::ConnectionResponse::OK)
      throw ConnectionError(response.message());
    stream_manager = StreamManager::create(this, stream);
  }
}

schema::ProcedureResult Client::invoke(const schema::Request& request) {
  std::lock_guard<std::mutex> lock_guard(*lock);
  return this->send_request(request);
}

schema::ProcedureResult Client::invoke(const schema::ProcedureCall& call) {
  std::lock_guard<std::mutex> lock_guard(*lock);
  request_buffer.Clear();
  // Copied because the call is received by const reference and a Request owns its calls, so it
  // cannot be moved in. CopyFrom also stays correct if ProcedureCall gains fields.
  request_buffer.add_calls()->CopyFrom(call);
  return this->send_request(request_buffer);
}

schema::ProcedureResult Client::invoke(std::string_view service, std::string_view procedure,
                                       const std::vector<encoder::Value>& args) {
  std::lock_guard<std::mutex> lock_guard(*lock);
  request_buffer.Clear();
  schema::ProcedureCall* call = request_buffer.add_calls();
  call->set_service(service);
  call->set_procedure(procedure);
  add_arguments(call, args);
  return this->send_request(request_buffer);
}

schema::ProcedureResult Client::invoke(std::string_view service, std::string_view procedure,
                                       const std::vector<encoder::Value>& args, Returns returns) {
  std::lock_guard<std::mutex> lock_guard(*lock);
  request_buffer.Clear();
  schema::ProcedureCall* call = request_buffer.add_calls();
  call->set_service(service);
  call->set_procedure(procedure);
  add_arguments(call, args);
  return this->send_request(request_buffer, &returns);
}

// Send a request and return the result it is answered by. Called with lock held, which makes
// the response and the bytes the request is written as safe to keep from one call to the next.
schema::ProcedureResult Client::send_request(const schema::Request& request,
                                             const Returns* returns) {
  // Looked up before the request is sent, as listing the services makes a call of its own
  std::vector<Returns> looked_up;
  if (stream_manager && returns == nullptr) {
    for (const auto& call : request.calls())
      looked_up.push_back(lookup_returns(call.service(), call.procedure()));
  }
  encoder::encode_message_with_size(request, &request_data);
  rpc_connection->send(request_data);
  rpc_connection->receive_message(response_buffer);
  if (returns != nullptr) {
    note_stream_id(0, *returns);
  } else {
    for (int i = 0; i < static_cast<int>(looked_up.size()); i++) note_stream_id(i, looked_up[i]);
  }
  note_removed_streams(request);

  if (response_buffer.has_error()) throw_exception(response_buffer.error());

  // Moved out of the response, which is about to be reused.
  schema::ProcedureResult result = std::move(*response_buffer.mutable_results(0));

  if (result.has_error()) throw_exception(result.error());

  return result;
}

// What a procedure returns, from the server's list of services, which is fetched by the first
// call. Called with lock held.
Client::Returns Client::lookup_returns(const std::string& service, const std::string& procedure) {
  if (!listed_returns->listed) {
    // Listed once, even if the listing fails
    listed_returns->listed = true;
    list_returns();
  }
  auto returns = listed_returns->returns.find(std::make_pair(service, procedure));
  return returns == listed_returns->returns.end() ? Returns::value : returns->second;
}

// Store the procedures the server lists that return a new stream or event. Called with lock
// held, before the buffers are used for the call being made.
void Client::list_returns() {
  schema::Request request;
  auto* call = request.add_calls();
  call->set_service("KRPC");
  call->set_procedure("GetServices");
  rpc_connection->send(encoder::encode_message_with_size(request));
  schema::Response response;
  rpc_connection->receive_message(response);
  if (response.has_error() || response.results_size() == 0 || response.results(0).has_error())
    return;
  schema::Services services;
  if (!services.ParseFromString(response.results(0).value())) return;
  for (const auto& service : services.services()) {
    for (const auto& procedure : service.procedures()) {
      auto code = procedure.return_type().code();
      if (code != schema::Type::STREAM && code != schema::Type::EVENT) continue;
      listed_returns->returns[std::make_pair(service.name(), procedure.name())] =
          code == schema::Type::EVENT ? Returns::event : Returns::stream;
    }
  }
}

// Note the id of a new stream, returned by the call at index, with the stream manager. Called
// with lock held, so ids are noted in the order the replies arrive, and the manager keeps the
// latest value and an ending sent for a stream before it is registered.
void Client::note_stream_id(int index, Returns returns) {
  if (returns == Returns::value || !stream_manager || response_buffer.has_error() ||
      index >= response_buffer.results_size())
    return;
  const auto& result = response_buffer.results(index);
  if (result.has_error() || result.is_null()) return;
  // A reply that fails to parse fails again where the caller decodes it
  if (returns == Returns::event) {
    schema::Event message;
    if (message.ParseFromString(result.value())) stream_manager->note_issued(message.stream().id());
  } else {
    schema::Stream message;
    if (message.ParseFromString(result.value())) stream_manager->note_issued(message.id());
  }
}

// Note the ids of the streams the server has removed for a request, whichever way the request
// was made. Called with lock held, so ids are noted in the order the replies arrive.
void Client::note_removed_streams(const schema::Request& request) {
  if (!stream_manager || response_buffer.has_error()) return;
  for (int i = 0; i < request.calls_size() && i < response_buffer.results_size(); i++) {
    const auto& call = request.calls(i);
    if (call.service() != "KRPC" || call.procedure() != "RemoveStream" ||
        response_buffer.results(i).has_error())
      continue;
    for (const auto& argument : call.arguments()) {
      if (argument.position() != 0) continue;
      // Decodes, as the server has decoded it for a call that succeeded
      uint64_t id = 0;
      decoder::decode(id, argument.value(), this);
      stream_manager->note_removed(id);
    }
  }
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

void Client::throw_exception(const schema::Error& error) const {
  if (!error.service().empty() && !error.name().empty()) {
    auto key = std::make_pair(error.service(), error.name());
    std::unique_lock<std::mutex> guard(*exception_throwers_lock);
    auto thrower = exception_throwers->find(key);
    if (thrower == exception_throwers->end()) {
      // The type is unknown here if the generated header for its service was never
      // instantiated in this translation unit, so nothing registered a thrower for it.
      // Report the error itself, named by its type on the server.
      throw RPCError(error.service() + "." + error.name() + ": " + error.description());
    }
    // Copy the thrower and release the lock before calling it: it throws, and it must not
    // be holding this lock while the exception propagates out through the caller.
    auto fn = thrower->second;
    guard.unlock();
    fn(error.description());
  } else {
    throw RPCError(error.description());
  }
}

void Client::check_stream_connection() const {
  if (!stream_manager) throw StreamError("Not connected to stream server");
}

StreamManager& Client::streams() const {
  check_stream_connection();
  return *stream_manager;
}

std::shared_ptr<StreamImpl> Client::add_stream(const schema::ProcedureCall& call) {
  return streams().add_stream(call);
}

std::shared_ptr<StreamImpl> Client::get_stream(uint64_t id) { return streams().get_stream(id); }

void Client::remove_stream(uint64_t id) { streams().remove_stream(id); }

std::condition_variable& Client::get_stream_update_condition() const {
  return streams().get_update_condition();
}

std::unique_lock<std::mutex>& Client::get_stream_update_condition_lock() const {
  return streams().get_update_condition_lock();
}

void Client::acquire_stream_update() { streams().acquire_update(); }

void Client::release_stream_update() { streams().release_update(); }

void Client::wait_for_stream_update(double timeout) {
  if (timeout < 0) {
    streams().get_update_condition().wait(streams().get_update_condition_lock());
  } else {
    auto rel_time = std::chrono::milliseconds(static_cast<int>(timeout * 1000));
    streams().get_update_condition().wait_for(streams().get_update_condition_lock(), rel_time);
  }
}

int Client::add_stream_update_callback(const Callback& callback) {
  return streams().add_update_callback(callback);
}

void Client::remove_stream_update_callback(int tag) { streams().remove_update_callback(tag); }

}  // namespace krpc
