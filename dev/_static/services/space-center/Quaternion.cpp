#include <iostream>
#include <tuple>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  krpc::Client conn = krpc::connect();
  krpc::services::SpaceCenter sc(&conn);
  std::tuple<double, double, double, double> q = sc.active_vessel().flight().rotation();
  std::cout << std::get<0>(q) << " "
            << std::get<1>(q) << " "
            << std::get<2>(q) << " "
            << std::get<3>(q) << std::endl;
}
