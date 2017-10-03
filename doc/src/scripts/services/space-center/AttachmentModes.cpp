#include <iostream>
#include <stack>
#include <string>
#include <utility>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>

using SpaceCenter = krpc::services::SpaceCenter;

int main() {
  auto conn = krpc::connect();
  SpaceCenter sc(&conn);
  auto vessel = sc.active_vessel();

  auto root = vessel.parts().root();
  std::stack<std::pair<SpaceCenter::Part, int> > stack;
  stack.push(std::pair<SpaceCenter::Part, int>(root, 0));
  while (!stack.empty()) {
    auto part = stack.top().first;
    auto depth = stack.top().second;
    stack.pop();
    std::string attach_mode = part.axially_attached() ? "axial" : "radial";
    std::cout << std::string(depth, ' ') << part.title() << " - " << attach_mode << std::endl;
    auto children = part.children();
    for (auto child : children) {
      stack.push(std::pair<SpaceCenter::Part, int>(child, depth+1));
    }
  }
}
