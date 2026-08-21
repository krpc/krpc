local class = require 'pl.class'
local unix = require 'socket.unix'
local LocalConnection = require 'krpc.localconnection'
local connectiontest = require 'krpc.test.connectiontest'

-- The connection carried over a unix domain socket, against a server the test listens on
-- itself rather than a kRPC server. The tests are those of the TCP/IP connection: what
-- differs is only how the socket is opened.
local TestConnectionLocalSocket = class(connectiontest.ConnectionTest)

function TestConnectionLocalSocket:start_server()
  -- A socket path has to fit in the kernel's address structure, which leaves far less room
  -- than a path named after the test would take, so it goes under the temporary directory
  self._path = os.tmpname()
  os.remove(self._path)
  local listener = assert(unix())
  assert(listener:bind(self._path))
  assert(listener:listen(1))
  return connectiontest.EchoServer(listener)
end

function TestConnectionLocalSocket:stop_server()
  os.remove(self._path)
end

function TestConnectionLocalSocket:connect()
  local conn = LocalConnection(self._path)
  conn:connect()
  return conn
end

return TestConnectionLocalSocket
