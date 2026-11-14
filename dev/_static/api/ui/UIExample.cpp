#include <krpc.hpp>
#include <krpc/services/ui.hpp>

int main() {
  auto conn = krpc::connect("User Interface Example");
  krpc::services::UI ui(conn);
  ui.message("Hello, World!", 10);
}
