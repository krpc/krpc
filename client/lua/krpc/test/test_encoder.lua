local luaunit = require 'luaunit'
local class = require 'pl.class'
local encoder = require 'krpc.encoder'
local platform = require 'krpc.platform'
local Types = require 'krpc.types'
local schema = require 'krpc.schema.KRPC'

local TestEncoder = class()

local types = Types()

function TestEncoder:test_encode_message()
  local call = schema.ProcedureCall()
  call.service = 'ServiceName'
  call.procedure = 'ProcedureName'
  data = encoder.encode(call, types:procedure_call_type())
  expected = '0a0b536572766963654e616d65120d50726f6365647572654e616d65'
  luaunit.assertEquals(platform.hexlify(data), expected)
end

function TestEncoder:test_encode_value()
  local data = encoder.encode(300, types:uint32_type())
  luaunit.assertEquals('ac02', platform.hexlify(data))
end

function TestEncoder:test_encode_unicode_string()
  local data = encoder.encode('\226\132\162', types:string_type())
  luaunit.assertEquals('03e284a2', platform.hexlify(data))
end

function TestEncoder:test_encode_message_with_size()
  local call = schema.ProcedureCall()
  call.service = 'ServiceName'
  call.procedure = 'ProcedureName'
  local data = encoder.encode_message_with_size(call, types:procedure_call_type())
  local expected = '1c'..'0a0b536572766963654e616d65120d50726f6365647572654e616d65'
  luaunit.assertEquals(expected, platform.hexlify(data))
end

function TestEncoder:test_encode_class()
  local typ = types:class_type('ServiceName', 'ClassName')
  local class_type = typ.lua_type
  local value = class_type(300)
  luaunit.assertEquals(300, value._object_id)
  local data = encoder.encode(value, typ)
  luaunit.assertEquals('ac02', platform.hexlify(data))
end

function TestEncoder:test_encode_class_none()
  local typ = types:class_type('ServiceName', 'ClassName')
  local value = nil
  local data = encoder.encode(value, typ)
  luaunit.assertEquals('00', platform.hexlify(data))
end

return TestEncoder
