#include <iomanip>
#include <iostream>
#include <krpc.hpp>
#include <krpc/services/scan_sat.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  krpc::Client conn = krpc::connect("SCANsat Example");
  krpc::services::SpaceCenter sc(conn);
  krpc::services::ScanSat scansat(conn);
  auto vessel = sc.active_vessel().value();

  for (auto scanner : scansat.scanners(vessel)) scanner.set_active(true);

  auto body = vessel.orbit().body();
  auto coverage =
      scansat.coverage(body, krpc::services::ScanSat::ScanType::altimetry);
  std::cout << std::fixed << std::setprecision(1) << "Altimetry coverage of "
            << body.name() << ": " << coverage * 100 << "%" << std::endl;

  auto flight = vessel.flight();
  auto elevation =
      scansat.elevation(body, flight.latitude(), flight.longitude());
  if (!elevation)
    std::cout << "The terrain below the vessel is not scanned" << std::endl;
  else
    std::cout << std::setprecision(0) << "The terrain below the vessel is at "
              << *elevation << " m" << std::endl;
}
