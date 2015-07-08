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
  return krpc.connect('localhost', 50011, 50012, 'TestClient')
end

return ServerTest
