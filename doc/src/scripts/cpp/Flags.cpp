#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  using MapFilterType = krpc::services::SpaceCenter::MapFilterType;
  auto conn = krpc::connect();
  krpc::services::SpaceCenter sc(conn);

  // Show only probes and ships in map mode
  sc.set_map_filter(MapFilterType::probes | MapFilterType::ships);

  // Check whether map mode shows debris
  auto filter = sc.map_filter();
  std::cout << ((filter & MapFilterType::debris) == MapFilterType::debris)
            << std::endl;

  // Show debris as well
  sc.set_map_filter(filter | MapFilterType::debris);
}
