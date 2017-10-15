local luaunit = require 'luaunit'
local class = require 'pl.class'
local decoder = require 'krpc.decoder'
local platform = require 'krpc.platform'
local schema = require 'krpc.schema.KRPC'
local Types = require 'krpc.types'

local TestDecoder = class()

local types = Types()

function TestDecoder:test_decode_message()
  local typ = schema.ProcedureCall
  local message = '0a0b536572766963654e616d65120d50726f6365647572654e616d65'
  local call = decoder.decode(platform.unhexlify(message), types:procedure_call_type())
  luaunit.assertEquals('ServiceName', call.service)
  luaunit.assertEquals('ProcedureName', call.procedure)
end

function TestDecoder:test_decode_value()
  local value = decoder.decode(platform.unhexlify('ac02'), types:uint32_type())
  luaunit.assertEquals(300, value)
end

function TestDecoder:test_decode_unicode_string()
  local value = decoder.decode(platform.unhexlify('03e284a2'), types:string_type())
  luaunit.assertEquals(value, '\226\132\162')
end

function TestDecoder:test_decode_size()
  local message = '1c'
  local size = decoder.decode_size(platform.unhexlify(message))
  luaunit.assertEquals(28, size)
end

function TestDecoder:test_decode_class()
  local typ = types:class_type('ServiceName', 'ClassName')
  local value = decoder.decode(platform.unhexlify('ac02'), typ)
  luaunit.assertTrue(typ.lua_type:class_of(value))
  luaunit.assertEquals(300, value._object_id)
end

function TestDecoder:test_decode_class_none()
  local typ = types:class_type('ServiceName', 'ClassName')
  local value = decoder.decode(platform.unhexlify('00'), typ)
  luaunit.assertEquals(Types.none, value)
end

function TestDecoder:test_guid()
  luaunit.assertEquals(
    '6f271b39-00dd-4de4-9732-f0d3a68838df',
    decoder.guid(platform.unhexlify('391b276fdd00e44d9732f0d3a68838df')))
end

return TestDecoder
