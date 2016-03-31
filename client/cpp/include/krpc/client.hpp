#ifndef HEADER_KRPC_CLIENT
#define HEADER_KRPC_CLIENT

#include "krpc/krpc.pb.hpp"
#include "krpc/connection.hpp"
#include "krpc/stream_manager.hpp"
#include <memory>

namespace krpc {

  class Client {
  private:
    std::shared_ptr<Connection> rpc_connection;
    StreamManager stream_manager;
    std::shared_ptr<std::mutex> lock;
  public:

    Client();
    Client(const std::shared_ptr<Connection>& rpc_connection, const std::shared_ptr<Connection>& stream_connection);

    schema::Request request(
      const std::string& service, const std::string& procedure,
      const std::vector<std::string>& args = std::vector<std::string>());

    std::string invoke(const schema::Request& request);

    std::string invoke(
      const std::string& service, const std::string& procedure,
      const std::vector<std::string>& args = std::vector<std::string>());

    google::protobuf::uint64 add_stream(const schema::Request& request);
    void remove_stream(google::protobuf::uint64 id);
    std::string get_stream(google::protobuf::uint64 id);
  };

}

#endif
