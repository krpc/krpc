#pragma once

#include <memory>
#include <string>
#include <vector>

#include "krpc/connection.hpp"
#include "krpc/krpc.pb.hpp"
#include "krpc/stream_manager.hpp"

namespace krpc {

class Client {
 public:
  Client();
  Client(const std::string& name, const std::string& address,
         unsigned int rpc_port = 50000, unsigned int stream_port = 50001);
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
  void freeze_streams();
  void thaw_streams();

 private:
  std::shared_ptr<Connection> rpc_connection;
  std::shared_ptr<StreamManager> stream_manager;
  std::shared_ptr<std::mutex> lock;
};

}  // namespace krpc
