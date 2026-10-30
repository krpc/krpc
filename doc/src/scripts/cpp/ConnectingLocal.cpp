#include <iostream>
#include <krpc.hpp>
#include <krpc/services/krpc.hpp>

int main() {
  auto conn = krpc::connect_local("My Example Program");
  krpc::services::KRPC krpc(&conn);
  std::cout << krpc.get_status().version() << std::endl;
}
