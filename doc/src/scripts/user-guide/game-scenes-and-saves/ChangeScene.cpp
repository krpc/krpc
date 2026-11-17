#include <chrono>
#include <krpc.hpp>
#include <krpc/services/krpc.hpp>
#include <thread>

int main() {
  auto conn = krpc::connect();
  krpc::services::KRPC krpc(conn);
  // [docs]
  using GameScene = krpc::services::KRPC::GameScene;
  krpc.set_game_scene(GameScene::space_center);
  while (krpc.game_scene() != GameScene::space_center)
    std::this_thread::sleep_for(std::chrono::milliseconds(100));
  // [/docs]
}
