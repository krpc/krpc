#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <iostream>

using namespace krpc;
using namespace krpc::services;

int main() {
  Client conn = krpc::connect();
  SpaceCenter sc(&conn);
  auto q = sc.active_vessel().flight().rotation();
  std::cout << q.get<0>() << " " << q.get<1>() << " " << q.get<2>() << " " << q.get<3>() << std::endl;
}
