#pragma once

#include "krpc/client.hpp"

namespace krpc {

class Service {
 public:
  explicit Service(const Client& client) : _client(client) {}

 protected:
  Client _client;
};

}  // namespace krpc
