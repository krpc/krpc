import { connect } from "@krpc-org/krpc";

const conn = await connect();

// [docs]
const GameScene = conn.krpc.GameScene;
await conn.krpc.setGameScene(GameScene.spaceCenter);
while ((await conn.krpc.getGameScene()) !== GameScene.spaceCenter) {
  await new Promise((resolve) => setTimeout(resolve, 100));
}
// [/docs]
await conn.close();
