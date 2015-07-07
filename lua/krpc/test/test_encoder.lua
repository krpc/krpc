local luaunit = require 'luaunit'
local class = require 'pl.class'
local encoder = require 'krpc.encoder'
local platform = require 'krpc.platform'
local schema = require 'krpc.schema.KRPC'

local TestEncoder = class()

function TestEncoder:test_rpc_hello_message()
  message = encoder.RPC_HELLO_MESSAGE
  luaunit.assertEquals(message:len(), 12)
  luaunit.assertEquals(platform.hexlify(message), '48454c4c4f2d525043000000')
end

function TestEncoder:test_stream_hello_message()
  message = encoder.STREAM_HELLO_MESSAGE
  luaunit.assertEquals(message:len(), 12)
  luaunit.assertEquals(platform.hexlify(message), '48454c4c4f2d53545245414d')
end

function TestEncoder:test_client_name()
  message = encoder.client_name('foo')
  luaunit.assertEquals(message:len(), 32)
  luaunit.assertEquals(platform.hexlify(message), '666f6f' .. string.rep('00', 29))
end

function TestEncoder:test_empty_client_name()
  message = encoder.client_name()
  luaunit.assertEquals(message:len(), 32)
  luaunit.assertEquals(platform.hexlify(message), string.rep('00', 32))
end

function TestEncoder:test_long_client_name()
  message = encoder.client_name(string.rep('a', 33))
  luaunit.assertEquals(message:len(), 32)
  luaunit.assertEquals(platform.hexlify(message), string.rep('61', 32))
end

function TestEncoder:test_encode_message()
  request = schema.Request()
  request.service = 'ServiceName'
  request.procedure = 'ProcedureName'
  --TODO
  --data = encoder.encode(request, self.types.as_type('KRPC.Request'))
  --expected = '0a0b536572766963654e616d65120d50726f6365647572654e616d65'
  --luaunit.assertEqual(platform.hexlify(data), expected)
end

return TestEncoder
