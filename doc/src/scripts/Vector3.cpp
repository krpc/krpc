#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <iostream>

using namespace krpc;
using namespace krpc::services;

int main() {
  Client conn = krpc::connect();
  SpaceCenter sc(&conn);
  auto v = sc.active_vessel().flight().prograde();
  std::cout << v.get<0>() << " " << v.get<1>() << " " << v.get<2>() << std::endl;
}
