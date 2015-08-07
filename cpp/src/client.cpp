#include "krpc/client.hpp"
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

    {
      std::string output;
      request.SerializeToString(&output);
      std::vector<char> data(output.begin(), output.end());

      int header_length = google::protobuf::io::CodedOutputStream::VarintSize64(data.size());
      char* header = new char[header_length];
      google::protobuf::io::CodedOutputStream::WriteVarint64ToArray(data.size(), (unsigned char*)header);

      rpc_connection->send(header, header_length);
      rpc_connection->send(data);
    }

    {
      size_t size = 0;
      std::stringstream result_header;

      uint64_t response_size;
      while (true) {
        std::vector<char> data = rpc_connection->receive(1);
        result_header << data[0];
        size++;
        google::protobuf::io::CodedInputStream input_stream((unsigned char*)&result_header.str()[0], size);
        bool result = input_stream.ReadVarint64(&response_size);
        if (result)
          break;
      }

      std::vector<char> response_data = rpc_connection->receive(response_size);

      Response response;
      response.ParseFromString(std::string(response_data.begin(), response_data.end()));

      return response.return_value();
    }
  }

}
