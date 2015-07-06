local TestClient = {}
TestClient.__index = TestClient

local krpc = require "krpc.init"

function TestClient:test_version()
  --conn = krpc.connect()
  --status = conn.krpc.get_status()
  assertEquals('0.1.9', '0.1.9')
end

return TestClient
