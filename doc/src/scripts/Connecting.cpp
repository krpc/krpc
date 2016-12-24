#include <iostream>
#include <krpc.hpp>
#include <krpc/services/krpc.hpp>

int main() {
  krpc::Client conn = krpc::connect("Remote example", "my.domain.name", 1000, 1001);
  krpc::services::KRPC krpc(&conn);
  std::cout << krpc.get_status().version() << std::endl;
}
