local krpc = require 'krpc'
local socket = require 'socket'
local conn = krpc.connect()

-- [docs]
local GameScene = conn.krpc.GameScene
conn.krpc.game_scene = GameScene.space_center
while conn.krpc.game_scene.value ~= GameScene.space_center.value do
  socket.sleep(0.1)
end
-- [/docs]
