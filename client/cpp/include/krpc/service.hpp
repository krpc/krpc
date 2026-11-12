#pragma once

#include "krpc/connection.hpp"

namespace krpc {

class Service {
 public:
  explicit Service(const Connection& connection) : _connection(connection) {}

 protected:
  Connection _connection;
};

}  // namespace krpc
