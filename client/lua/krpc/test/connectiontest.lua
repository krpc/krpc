local luaunit = require 'luaunit'
local class = require 'pl.class'
local socket = require 'socket'
local encoder = require 'krpc.encoder'
local schema = require 'krpc.schema.KRPC'

-- A stand-in for a kRPC server, which sends back whatever it is sent. These tests are
-- about how a transport moves bytes, so nothing above the transport has to be understood
-- to answer them. It is driven by the test and not by a thread of its own: the client
-- sends, then the test tells the server how much to echo back.
local EchoServer = class()

function EchoServer:_init(listener)
  self._listener = listener
  self._accepted = nil
end

function EchoServer:accept_one()
  self._accepted = self._listener:accept()
end

function EchoServer:echo(length)
  local data = self._accepted:receive(length)
  self._accepted:send(data)
end

function EchoServer:close()
  if self._accepted ~= nil then
    self._accepted:close()
  end
  self._listener:close()
end

-- The behavior a connection to a server has whatever carries it. Only opening the
-- connection differs between the transports, so each of them supplies a server to talk to
-- and a connection reaching it, and shares these.
local ConnectionTest = class()

function ConnectionTest:setUp()
  self.server = self:start_server()
  self.conn = self:connect()
  self.server:accept_one()
end

function ConnectionTest:tearDown()
  self.conn:close()
  self.server:close()
  self:stop_server()
end

function ConnectionTest:test_send_receive()
  self.conn:send('foo')
  self.server:echo(3)
  luaunit.assertEquals(self.conn:receive(3), 'foo')
end

function ConnectionTest:test_receive_nothing()
  luaunit.assertEquals(self.conn:receive(0), '')
end

function ConnectionTest:test_long_send_receive()
  local message = string.rep('x', 16 * 1024)
  self.conn:send(message)
  self.server:echo(#message)
  luaunit.assertEquals(self.conn:receive(#message), message)
end

function ConnectionTest:test_send_receive_in_pieces()
  -- A receive need not line up with a send, as the transport carries a stream of bytes
  -- and not the messages put into it
  self.conn:send('foobar')
  self.server:echo(6)
  luaunit.assertEquals(self.conn:receive(3), 'foo')
  luaunit.assertEquals(self.conn:receive(3), 'bar')
end

function ConnectionTest:test_send_receive_message()
  local response = schema.Response()
  local result = response.results:add()
  result.value = 'foo'
  local data = encoder.encode_message_with_size(response)
  self.conn:send(data)
  self.server:echo(#data)
  local received = self.conn:receive_message(schema.Response)
  luaunit.assertEquals(received.results[1].value, 'foo')
end

function ConnectionTest:test_receive_on_closed_connection()
  self.conn:close()
  luaunit.assertError(function() self.conn:receive(1) end)
end

-- A TCP listener on a port the system picks, and the address a client reaches it at.
-- The loopback address is given, and not a name, so that the listener is on the address
-- family the connection under test opens a socket for: 'localhost' resolves to the IPv6
-- loopback first on some platforms, and a connection is IPv4.
local function tcpip_listener()
  local listener = assert(socket.bind('127.0.0.1', 0))
  local address, port = listener:getsockname()
  return listener, address, port
end

return {
  EchoServer = EchoServer,
  ConnectionTest = ConnectionTest,
  tcpip_listener = tcpip_listener,
}
