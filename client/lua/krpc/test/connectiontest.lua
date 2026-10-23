local luaunit = require 'luaunit'
local class = require 'pl.class'
local socket = require 'socket'
local encoder = require 'krpc.encoder'
local decoder = require 'krpc.decoder'
local Client = require 'krpc.client'
local Types = require 'krpc.types'

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

function EchoServer:hang_up()
  self._accepted:close()
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
  local data = encoder.encode_in_message('ConnectionRequest', { client_name = 'foo' })
  self.conn:send(data)
  self.server:echo(#data)
  local received = decoder.decode_message(self.conn:receive_message_data(), 'Message')
  luaunit.assertEquals(received.connection_request.client_name, 'foo')
end

function ConnectionTest:test_receive_on_closed_connection()
  self.conn:close()
  luaunit.assertError(function() self.conn:receive(1) end)
end

-- A failed read ends the connection, so a later call raises the same error without writing.
-- The client is built without its constructor, which makes a call to fetch the services.
function ConnectionTest:test_client_records_a_failed_read()
  local client = setmetatable({ _connection = self.conn, _next_id = 1, _types = Types() }, Client)
  self.server:hang_up()
  local ok, err = pcall(client._invoke, client, 'KRPC', 'GetStatus')
  luaunit.assertFalse(ok)
  luaunit.assertStrContains(err, 'Socket error')
  local wrote = false
  self.conn.send = function() wrote = true end
  luaunit.assertErrorMsgContains(err, client._invoke, client, 'KRPC', 'GetStatus')
  luaunit.assertFalse(wrote)
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
