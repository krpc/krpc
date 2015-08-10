#include "krpc/client.hpp"
#include "krpc/encoder.hpp"
#include "krpc/decoder.hpp"
#include "krpc/KRPC.pb.h"
#include <google/protobuf/io/coded_stream.h>

namespace krpc {

  Client::Client(const boost::shared_ptr<Connection>& rpc_connection,
                 const boost::shared_ptr<Connection>& stream_connection):
    rpc_connection(rpc_connection),
    stream_connection(stream_connection) {}

  std::string Client::invoke(const std::string& service, const std::string& procedure) {
    Request request;
    request.set_service(service);
    request.set_procedure(procedure);
    rpc_connection->send(Encoder::encode_delimited(request));

    {
      size_t size = 0;
      std::string data;
      while (true) {
        try {
          data += rpc_connection->receive(1); //TODO: partial_receive needed here?
          size = Decoder::decode_size_and_position(data).first;
          break;
        } catch (DecodeFailed& e) {
        }
      }

      data = rpc_connection->receive(size);
      krpc::Response response;
      Decoder::decode(response, data);
      return response.return_value();
    }
  }

}
