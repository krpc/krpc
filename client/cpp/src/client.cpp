#include "krpc/client.hpp"

#include <google/protobuf/io/coded_stream.h>

#include <string>
#include <vector>

#include "krpc/encoder.hpp"
#include "krpc/decoder.hpp"
#include "krpc/error.hpp"

namespace krpc {

Client::Client(): lock(new std::mutex) {}

Client::Client(const std::shared_ptr<Connection>& rpc_connection,
               const std::shared_ptr<Connection>& stream_connection):
  rpc_connection(rpc_connection),
  stream_manager(this, stream_connection),
  lock(new std::mutex) {}

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
    throw RPCError(response.error().description());

  if (response.results(0).has_error())
    throw RPCError(response.results(0).error().description());

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

google::protobuf::uint64 Client::add_stream(const schema::ProcedureCall& call) {
  return stream_manager.add_stream(call);
}

void Client::remove_stream(google::protobuf::uint64 id) {
  stream_manager.remove_stream(id);
}

std::string Client::get_stream(google::protobuf::uint64 id) {
  return stream_manager.get(id);
}

void Client::freeze_streams() {
  stream_manager.freeze();
}

void Client::thaw_streams() {
  stream_manager.thaw();
}

}  // namespace krpc
