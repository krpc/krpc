import krpc.client.Connection;
import krpc.client.services.KRPC;
import krpc.client.services.KRPC.GameScene;

public class ChangeScene {
  public static void main(String[] args) throws InterruptedException {
    try (Connection connection = Connection.newInstance()) {
      KRPC krpc = KRPC.newInstance(connection);
      // [docs]
      krpc.setGameScene(GameScene.SPACE_CENTER);
      while (krpc.getGameScene() != GameScene.SPACE_CENTER) {
        Thread.sleep(100);
      }
      // [/docs]
    }
  }
}
