#include <iostream>
#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <stack>
#include <string>
#include <utility>

using SpaceCenter = krpc::services::SpaceCenter;

int main() {
  krpc::Client conn = krpc::connect("");
  SpaceCenter sc(conn);
  auto vessel = sc.active_vessel().value();

  auto root = vessel.parts().root();
  std::stack<std::pair<SpaceCenter::Part, int>> stack;
  stack.push(std::pair<SpaceCenter::Part, int>(root, 0));
  while (!stack.empty()) {
    auto part = stack.top().first;
    auto depth = stack.top().second;
    stack.pop();
    std::cout << std::string(depth, ' ') << part.title() << std::endl;
    for (const auto& child : part.children())
      stack.push(std::pair<SpaceCenter::Part, int>(child, depth + 1));
  }
}
