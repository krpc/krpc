#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <iostream>

using namespace krpc::services;

int main() {
  krpc::Client conn = krpc::connect();
  SpaceCenter sc(&conn);
  std::tuple<double,double,double,double> q = sc.active_vessel().flight().rotation();
  std::cout << std::get<0>(q) << " "
            << std::get<1>(q) << " "
            << std::get<2>(q) << " "
            << std::get<3>(q) << std::endl;
}
