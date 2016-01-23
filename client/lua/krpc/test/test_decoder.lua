local luaunit = require 'luaunit'
local class = require 'pl.class'
local decoder = require 'krpc.decoder'
local platform = require 'krpc.platform'
local schema = require 'krpc.schema.KRPC'
local Types = require 'krpc.types'

local TestDecoder = class()

local types = Types()

function TestDecoder:test_decode_message()
  local typ = schema.Request
  local message = '0a0b536572766963654e616d65120d50726f6365647572654e616d65'
  local request = decoder.decode(platform.unhexlify(message), types:as_type('KRPC.Request'))
  luaunit.assertEquals('ServiceName', request.service)
  luaunit.assertEquals('ProcedureName', request.procedure)
end

function TestDecoder:test_decode_value()
  local value = decoder.decode(platform.unhexlify('ac02'), types:as_type('int32'))
  luaunit.assertEquals(300, value)
end

function TestDecoder:test_decode_unicode_string()
  local value = decoder.decode(platform.unhexlify('03e284a2'), types:as_type('string'))
  luaunit.assertEquals(value, '\226\132\162')
end

function TestDecoder:test_decode_size_and_position()
  local message = '1c'
  local size,position = decoder.decode_size_and_position(platform.unhexlify(message))
  luaunit.assertEquals(28, size)
  luaunit.assertEquals(1, position)
end

function TestDecoder:test_decode_message_delimited()
  local typ = schema.Request
  local message = '1c'..'0a0b536572766963654e616d65120d50726f6365647572654e616d65'
  local request = decoder.decode_delimited(platform.unhexlify(message), types:as_type('KRPC.Request'))
  luaunit.assertEquals('ServiceName', request.service)
  luaunit.assertEquals('ProcedureName', request.procedure)
end

function TestDecoder:test_decode_value_delimited()
  local value = decoder.decode_delimited(platform.unhexlify('02'..'ac02'), types:as_type('int32'))
  luaunit.assertEquals(300, value)
end

function TestDecoder:test_decode_class()
  local typ = types:as_type('Class(ServiceName.ClassName)')
  local value = decoder.decode(platform.unhexlify('ac02'), typ)
  luaunit.assertTrue(typ.lua_type:class_of(value))
  luaunit.assertEquals(300, value._object_id)
end

function TestDecoder:test_decode_class_none()
  local typ = types:as_type('Class(ServiceName.ClassName)')
  local value = decoder.decode(platform.unhexlify('00'), typ)
  luaunit.assertEquals(Types.none, value)
end

function TestDecoder:test_guid()
  luaunit.assertEquals(
    '6f271b39-00dd-4de4-9732-f0d3a68838df',
    decoder.guid(platform.unhexlify('391b276fdd00e44d9732f0d3a68838df')))
end

return TestDecoder
