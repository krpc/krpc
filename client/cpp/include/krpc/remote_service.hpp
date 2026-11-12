#pragma once

#include "krpc/connection.hpp"

namespace krpc {

class RemoteService {
 public:
  explicit RemoteService(const Connection& connection) : _connection(connection) {}

 protected:
  Connection _connection;
};

}  // namespace krpc
