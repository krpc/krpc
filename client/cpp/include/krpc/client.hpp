#ifndef HEADER_KRPC_CLIENT
#define HEADER_KRPC_CLIENT

#include "krpc/krpc.pb.hpp"
#include "krpc/connection.hpp"
#include <memory>

namespace krpc {

  class Client {
    std::shared_ptr<Connection> rpc_connection;
    std::shared_ptr<Connection> stream_connection;
  public:
    Client();
    Client(const std::shared_ptr<Connection>& rpc_connection,
           const std::shared_ptr<Connection>& stream_connection);
    schema::Request request(
      const std::string& service, const std::string& procedure,
      const std::vector<std::string>& args = std::vector<std::string>());
    std::string invoke(
      const std::string& service, const std::string& procedure,
      const std::vector<std::string>& args = std::vector<std::string>());
  };

}

#endif
