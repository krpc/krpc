import time

import krpc

conn = krpc.connect()

# [docs]
GameScene = conn.krpc.GameScene
conn.krpc.game_scene = GameScene.space_center
while conn.krpc.game_scene != GameScene.space_center:
    time.sleep(0.1)
# [/docs]
