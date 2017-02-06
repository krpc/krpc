#pragma once

#include <map>
#include <memory>
#include <string>
#include <utility>
#include <vector>

#include "krpc/connection.hpp"
#include "krpc/krpc.pb.hpp"
#include "krpc/stream_manager.hpp"

namespace krpc {

class Client {
 public:
  Client();
  Client(const std::shared_ptr<Connection>& rpc_connection,
         const std::shared_ptr<Connection>& stream_connection);

  std::string invoke(const schema::Request& request);
  std::string invoke(const schema::ProcedureCall& call);
  std::string invoke(
    const std::string& service, const std::string& procedure,
    const std::vector<std::string>& args = std::vector<std::string>());

  schema::Request build_request(
    const std::string& service, const std::string& procedure,
    const std::vector<std::string>& args = std::vector<std::string>());
  schema::ProcedureCall build_call(
    const std::string& service, const std::string& procedure,
    const std::vector<std::string>& args = std::vector<std::string>());
  void add_exception_thrower(const std::string& service, const std::string& name,
                             const std::function<void(std::string)>& thrower);

 private:
  void throw_exception(const schema::Error& error) const;

 public:
  google::protobuf::uint64 add_stream(const schema::ProcedureCall& call);
  void remove_stream(google::protobuf::uint64 id);
  std::string get_stream(google::protobuf::uint64 id);
  void freeze_streams();
  void thaw_streams();

 private:
  std::shared_ptr<Connection> rpc_connection;
  StreamManager stream_manager;
  std::shared_ptr<std::mutex> lock;
  std::map<std::pair<std::string, std::string>,
           std::function<void(std::string)>> exception_throwers;
};

}  // namespace krpc
