#include <iostream>
#include <stdexcept>
#include <krpc.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect();
  krpc::services::KRPC krpc(&conn);
  krpc::services::SpaceCenter sc(&conn);
  try {
    sc.load("missing");
  } catch (const std::invalid_argument& error) {
    std::cout << "Failed to load the save: " << error.what() << std::endl;
  }
}
