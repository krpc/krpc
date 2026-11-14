#include <krpc.hpp>
#include <krpc/services/debug.hpp>

int main() {
  auto conn = krpc::connect("Debug Example");
  krpc::services::Debug debug(conn);
  debug.set_infinite_propellant(true);
}
