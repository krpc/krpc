#include <fstream>
#include <krpc.hpp>
#include <krpc/services/docking_camera.hpp>
#include <krpc/services/space_center.hpp>

int main() {
  auto conn = krpc::connect("Docking Camera Example");
  krpc::services::SpaceCenter space_center(conn);
  krpc::services::DockingCamera docking_camera(conn);
  auto vessel = space_center.active_vessel().value();
  auto part = vessel.parts().with_name("OnboardCamera").front();
  auto camera = docking_camera.camera(part);
  std::ofstream("camera.png", std::ios::binary) << camera.image();
}
