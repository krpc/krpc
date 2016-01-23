local luaunit = require 'luaunit'
local class = require 'pl.class'
local encoder = require 'krpc.encoder'
local platform = require 'krpc.platform'
local Types = require 'krpc.types'
local schema = require 'krpc.schema.KRPC'

local TestEncoder = class()

local types = Types()

function TestEncoder:test_rpc_hello_message()
  local message = encoder.RPC_HELLO_MESSAGE
  luaunit.assertEquals(message:len(), 12)
  luaunit.assertEquals(platform.hexlify(message), '48454c4c4f2d525043000000')
end

function TestEncoder:test_stream_hello_message()
  local message = encoder.STREAM_HELLO_MESSAGE
  luaunit.assertEquals(message:len(), 12)
  luaunit.assertEquals(platform.hexlify(message), '48454c4c4f2d53545245414d')
end

function TestEncoder:test_client_name()
  local message = encoder.client_name('foo')
  luaunit.assertEquals(message:len(), 32)
  luaunit.assertEquals(platform.hexlify(message), '666f6f' .. string.rep('00', 29))
end

function TestEncoder:test_empty_client_name()
  local message = encoder.client_name()
  luaunit.assertEquals(message:len(), 32)
  luaunit.assertEquals(platform.hexlify(message), string.rep('00', 32))
end

function TestEncoder:test_long_client_name()
  local message = encoder.client_name(string.rep('a', 33))
  luaunit.assertEquals(message:len(), 32)
  luaunit.assertEquals(platform.hexlify(message), string.rep('61', 32))
end

function TestEncoder:test_encode_message()
  local request = schema.Request()
  request.service = 'ServiceName'
  request.procedure = 'ProcedureName'
  data = encoder.encode(request, types:as_type('KRPC.Request'))
  expected = '0a0b536572766963654e616d65120d50726f6365647572654e616d65'
  luaunit.assertEquals(platform.hexlify(data), expected)
end

function TestEncoder:test_encode_value()
  local data = encoder.encode(300, types:as_type('int32'))
  luaunit.assertEquals('ac02', platform.hexlify(data))
end

function TestEncoder:test_encode_unicode_string()
  local data = encoder.encode('\226\132\162', types:as_type('string'))
  luaunit.assertEquals('03e284a2', platform.hexlify(data))
end

function TestEncoder:test_encode_message_delimited()
  local request = schema.Request()
  request.service = 'ServiceName'
  request.procedure = 'ProcedureName'
  local data = encoder.encode_delimited(request, types:as_type('KRPC.Request'))
  local expected = '1c'..'0a0b536572766963654e616d65120d50726f6365647572654e616d65'
  luaunit.assertEquals(expected, platform.hexlify(data))
end

function TestEncoder:test_encode_value_delimited()
  local data = encoder.encode_delimited(300, types:as_type('int32'))
  luaunit.assertEquals('02'..'ac02', platform.hexlify(data))
end

function TestEncoder:test_encode_class()
  local typ = types:as_type('Class(ServiceName.ClassName)')
  local class_type = typ.lua_type
  local value = class_type(300)
  luaunit.assertEquals(300, value._object_id)
  local data = encoder.encode(value, typ)
  luaunit.assertEquals('ac02', platform.hexlify(data))
end

function TestEncoder:test_encode_class_none()
  local typ = types:as_type('Class(ServiceName.ClassName)')
  local value = nil
  local data = encoder.encode(value, typ)
  luaunit.assertEquals('00', platform.hexlify(data))
end

return TestEncoder
