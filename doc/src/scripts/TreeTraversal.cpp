#include <krpc.hpp>
#include <krpc/services/space_center.hpp>
#include <iostream>
#include <stack>

using namespace krpc;
using namespace krpc::services;

int main() {
  Client conn = krpc::connect("");
  SpaceCenter sc(&conn);
  auto vessel = sc.active_vessel();

  auto root = vessel.parts().root();
  std::stack<std::pair<SpaceCenter::Part,int> > stack;
  stack.push(std::pair<SpaceCenter::Part,int>(root, 0));
  while (stack.size() > 0) {
    auto part = stack.top().first;
    auto depth = stack.top().second;
    stack.pop();
    std::cout << std::string(depth, ' ') << part.title() << std::endl;
    auto children = part.children();
    for (std::vector<SpaceCenter::Part>::iterator child = children.begin(); child != children.end(); child++) {
      stack.push(std::pair<SpaceCenter::Part,int>(*child, depth+1));
    }
  }
}
