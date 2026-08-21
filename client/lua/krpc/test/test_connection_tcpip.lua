local class = require 'pl.class'
local Connection = require 'krpc.connection'
local connectiontest = require 'krpc.test.connectiontest'

-- The connection carried over TCP/IP, against a server the test listens on itself rather
-- than a kRPC server
local TestConnectionTCPIP = class(connectiontest.ConnectionTest)

function TestConnectionTCPIP:start_server()
  local listener, address, port = connectiontest.tcpip_listener()
  self._address = address
  self._port = port
  return connectiontest.EchoServer(listener)
end

function TestConnectionTCPIP:stop_server()
end

function TestConnectionTCPIP:connect()
  local conn = Connection(self._address, self._port)
  conn:connect()
  return conn
end

return TestConnectionTCPIP
