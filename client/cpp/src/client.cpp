#include "krpc/client.hpp"

#include <string>
#include <vector>

// IWYU pragma: no_include <asio/impl/io_service.ipp>

#include "krpc/connection.hpp"
#include "krpc/decoder.hpp"
#include "krpc/encoder.hpp"
#include "krpc/error.hpp"
#include "krpc/stream_manager.hpp"

namespace krpc {

class StreamImpl;

Client::Client(): lock(new std::mutex) {}

Client::Client(const std::string& name, const std::string& address,
               unsigned int rpc_port, unsigned int stream_port) :
  lock(new std::mutex) {
  // Connect to RPC server
  rpc_connection = std::make_shared<Connection>(address, rpc_port);
  rpc_connection->connect();
  schema::ConnectionRequest request;
  request.set_type(schema::ConnectionRequest::RPC);
  request.set_client_name(name);
  rpc_connection->send(encoder::encode_message_with_size(request));
  schema::ConnectionResponse response;
  decoder::decode(response, rpc_connection->receive_message(), nullptr);
  if (response.status() != schema::ConnectionResponse::OK)
    throw ConnectionError(response.message());

  // Connect to Stream server
  std::shared_ptr<Connection> stream_connection;
  if (stream_port != 0) {
    auto stream_connection = std::make_shared<Connection>(address, stream_port);
    stream_connection->connect();
    schema::ConnectionRequest request;
    request.set_type(schema::ConnectionRequest::STREAM);
    request.set_client_identifier(response.client_identifier());
    stream_connection->send(encoder::encode_message_with_size(request));
    schema::ConnectionResponse response;
    decoder::decode(response, stream_connection->receive_message(), nullptr);
    if (response.status() != schema::ConnectionResponse::OK)
      throw ConnectionError(response.message());
    stream_manager = std::make_shared<StreamManager>(this, stream_connection);
  }
}

std::string Client::invoke(const schema::Request& request) {
  std::string data;
  {
    std::lock_guard<std::mutex> lock_guard(*lock);
    rpc_connection->send(encoder::encode_message_with_size(request));
    data = rpc_connection->receive_message();
  }

  schema::Response response;
  decoder::decode(response, data, this);

  if (response.has_error())
    throw_exception(response.error());

  if (response.results(0).has_error())
    throw_exception(response.results(0).error());

  return response.results(0).value();
}

std::string Client::invoke(const schema::ProcedureCall& call) {
  // TODO: is there a way to avoid copying the ProcedureCall in order to create a Request?
  schema::Request request;
  schema::ProcedureCall* call2 = request.add_calls();
  call2->set_service(call.service());
  call2->set_procedure(call.procedure());
  for (auto arg : call.arguments()) {
    schema::Argument* arg2 = call2->add_arguments();
    arg2->set_position(arg.position());
    arg2->set_value(arg.value());
  }
  return this->invoke(request);
}

std::string Client::invoke(const std::string& service,
                           const std::string& procedure,
                           const std::vector<std::string>& args) {
  return this->invoke(this->build_request(service, procedure, args));
}

schema::Request Client::build_request(const std::string& service,
                                      const std::string& procedure,
                                      const std::vector<std::string>& args) {
  schema::Request request;
  schema::ProcedureCall * call = request.add_calls();
  call->set_service(service);
  call->set_procedure(procedure);
  for (unsigned int i = 0; i < args.size(); i++) {
    schema::Argument* arg = call->add_arguments();
    arg->set_position(i);
    arg->set_value(args[i]);
  }
  return request;
}

schema::ProcedureCall Client::build_call(const std::string& service,
                                         const std::string& procedure,
                                         const std::vector<std::string>& args) {
  schema::ProcedureCall call;
  call.set_service(service);
  call.set_procedure(procedure);
  for (unsigned int i = 0; i < args.size(); i++) {
    schema::Argument* arg = call.add_arguments();
    arg->set_position(i);
    arg->set_value(args[i]);
  }
  return call;
}

void Client::add_exception_thrower(const std::string& service, const std::string& name,
                                   const std::function<void(std::string)>& thrower) {
  exception_throwers[std::make_pair(service, name)] = thrower;
}

void Client::throw_exception(const schema::Error& error) const {
  if (!error.service().empty() && !error.name().empty()) {
    auto key = std::make_pair(error.service(), error.name());
    auto thrower = exception_throwers.find(key)->second;
    thrower(error.description());
  } else {
    throw RPCError(error.description());
  }
}

std::shared_ptr<StreamImpl> Client::add_stream(const schema::ProcedureCall& call) {
  return stream_manager->add_stream(call);
}

std::shared_ptr<StreamImpl> Client::get_stream(google::protobuf::uint64 id) {
  return stream_manager->get_stream(id);
}

void Client::remove_stream(google::protobuf::uint64 id) {
  stream_manager->remove_stream(id);
}

void Client::freeze_streams() {
  stream_manager->freeze();
}

void Client::thaw_streams() {
  stream_manager->thaw();
}

std::condition_variable& Client::get_stream_update_condition() const {
  return stream_manager->get_update_condition();
}

std::unique_lock<std::mutex>& Client::get_stream_update_condition_lock() const  {
  return stream_manager->get_update_condition_lock();
}

void Client::acquire_stream_update() {
  stream_manager->get_update_condition_lock().lock();
}

void Client::release_stream_update() {
  stream_manager->get_update_condition_lock().unlock();
}

void Client::wait_for_stream_update(double timeout) {
  if (timeout < 0) {
    stream_manager->get_update_condition().wait(stream_manager->get_update_condition_lock());
  } else {
    auto rel_time = std::chrono::milliseconds(static_cast<int>(timeout*1000));
    stream_manager->get_update_condition().wait_for(
      stream_manager->get_update_condition_lock(), rel_time);
  }
}

int Client::add_stream_update_callback(const Callback& callback) {
  return stream_manager->add_update_callback(callback);
}

void Client::remove_stream_update_callback(int tag) {
  stream_manager->remove_update_callback(tag);
}

}  // namespace krpc
