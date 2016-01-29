#include "krpc/client.hpp"
#include "krpc/encoder.hpp"
#include "krpc/decoder.hpp"
#include "krpc/error.hpp"
#include <google/protobuf/io/coded_stream.h>

namespace krpc {

  Client::Client() {}

  Client::Client(const std::shared_ptr<Connection>& rpc_connection,
                 const std::shared_ptr<Connection>& stream_connection):
    rpc_connection(rpc_connection),
    stream_connection(stream_connection) {}

  schema::Request Client::request(
    const std::string& service, const std::string& procedure,
    const std::vector<std::string>& args) {
    schema::Request request;
    request.set_service(service);
    request.set_procedure(procedure);
    for (unsigned int i = 0; i < args.size(); i++) {
      schema::Argument* arg = request.add_arguments();
      arg->set_position(i);
      arg->set_value(args[i]);
    }
    return request;
  }

  std::string Client::invoke(
    const std::string& service, const std::string& procedure,
    const std::vector<std::string>& args) {

    schema::Request request = this->request(service, procedure, args);
    rpc_connection->send(encoder::encode_delimited(request));

    size_t size = 0;
    std::string data;
    while (true) {
      try {
        data += rpc_connection->receive(1); //TODO: partial_receive needed here?
        size = decoder::decode_size_and_position(data).first;
        break;
      } catch (decoder::DecodeFailed& e) {
      }
    }

    data = rpc_connection->receive(size);
    schema::Response response;
    decoder::decode(response, data, this);

    if (response.has_error())
      throw RPCError(response.error());

    return response.return_value();
  }

}
