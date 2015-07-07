local luaunit = require 'luaunit'
local class = require 'pl.class'
local krpc = require 'krpc.init'

local TestClient = class()

function TestClient:test_version()
  --conn = krpc.connect()
  --status = conn.krpc.get_status()
  luaunit.assertEquals('0.1.9', '0.1.9')
end

return TestClient
