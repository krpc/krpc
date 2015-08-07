#include "krpc/krpc.hpp"
#include "krpc/services/krpc.hpp"
#include <iostream>
#include <boost/exception/all.hpp>

int main() {
  try {
    krpc::Client conn = krpc::connect("TestClient", "localhost", 50000, 50001);
    krpc::services::KRPC krpc(conn);
    krpc::Status status = krpc.get_status();
    std::cout << "Server version = " << status.version() << std::endl;
    krpc::Services services = krpc.get_services();
    std::cout << "Services:" << std::endl;
    for (int i = 0; i < services.services_size(); i++) {
      krpc::Service service = services.services(i);
      std::cout << "    " << service.name() << std::endl;
      for (int j = 0; j < service.procedures_size(); j++) {
        std::cout << "        " << service.procedures(j).name() << std::endl;
      }
    }
  } catch(boost::exception& e) {
    std::cerr << diagnostic_information(e);
  }
  return 0;
}
