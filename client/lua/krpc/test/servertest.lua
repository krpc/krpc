local class = require 'pl.class'
local krpc = require 'krpc.init'
local platform = require 'krpc.platform'

ServerTest = class()

function ServerTest:setUp()
  self.conn = self.connect()
end

function ServerTest:tearDown()
  self.conn:close()
end

function ServerTest:connect()
  return krpc.connect('LuaClientTest', 'localhost', 50014, 50015)
end

return ServerTest
