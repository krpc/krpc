local luaunit = require 'luaunit'
local class = require 'pl.class'
local decoder = require 'krpc.decoder'
local platform = require 'krpc.platform'
local protobuf = require 'krpc.protobuf'
local Types = require 'krpc.types'
local encoder = require 'krpc.encoder'
local List = require 'pl.List'

local TestDecoder = class()

local types = Types()

function TestDecoder:test_decode_message()
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

-- A collection is read by the tags its values are carried under, which must match krpc.proto.
-- This hands it collections encoded as messages and checks the values come back.
function TestDecoder:test_decode_collections_encoded_as_messages()
  local long = string.rep('x', 300)

  local list_message = { items = {} }
  for i, value in ipairs({'one', 'two', long}) do
    list_message.items[i] = encoder.encode(value, types:string_type())
  end
  local values = decoder.decode(protobuf.encode('List', list_message),
                                types:list_type(types:string_type()))
  luaunit.assertEquals(3, #values)
  luaunit.assertEquals('one', values[1])
  luaunit.assertEquals('two', values[2])
  luaunit.assertEquals(long, values[3])

  local empty = decoder.decode(protobuf.encode('List', {}),
                               types:list_type(types:string_type()))
  luaunit.assertEquals(0, #empty)

  local set_message = { items = {
    encoder.encode(7, types:uint32_type()),
    encoder.encode(300, types:uint32_type())
  } }
  local set = decoder.decode(protobuf.encode('Set', set_message),
                             types:set_type(types:uint32_type()))
  luaunit.assertEquals(true, set[7])
  luaunit.assertEquals(true, set[300])

  local tuple_message = { items = {
    encoder.encode(7, types:uint32_type()),
    encoder.encode(long, types:string_type())
  } }
  local tuple = decoder.decode(
    protobuf.encode('Tuple', tuple_message),
    types:tuple_type(List{types:uint32_type(), types:string_type()}))
  luaunit.assertEquals(2, #tuple)
  luaunit.assertEquals(7, tuple[1])
  luaunit.assertEquals(long, tuple[2])

  local dictionary_message = { entries = {
    { key = encoder.encode('a', types:string_type()), value = encoder.encode(1, types:uint32_type()) },
    { key = encoder.encode('b', types:string_type()), value = encoder.encode(300, types:uint32_type()) }
  } }
  local dictionary = decoder.decode(
    protobuf.encode('Dictionary', dictionary_message),
    types:dictionary_type(types:string_type(), types:uint32_type()))
  luaunit.assertEquals(1, dictionary['a'])
  luaunit.assertEquals(300, dictionary['b'])

  -- A value that encodes to nothing, an empty list here, is left out of the entry entirely
  local empty_valued = { entries = {
    { key = encoder.encode('a', types:string_type()), value = '' }
  } }
  local with_empty = decoder.decode(
    protobuf.encode('Dictionary', empty_valued),
    types:dictionary_type(types:string_type(), types:list_type(types:uint32_type())))
  luaunit.assertEquals(0, #with_empty['a'])
end

-- Data with no bytes reads as the zero value, as a protobuf field left out does
function TestDecoder:test_decode_empty_value()
  luaunit.assertEquals(0, decoder.decode('', types:uint32_type()))
  luaunit.assertEquals(0, decoder.decode('', types:sint64_type()))
  luaunit.assertEquals(0, decoder.decode('', types:double_type()))
  luaunit.assertEquals('', decoder.decode('', types:string_type()))
  luaunit.assertEquals(0, #decoder.decode('', types:list_type(types:uint32_type())))
end

function TestDecoder:test_decode_truncated_value()
  local list_type = types:list_type(types:uint32_type())
  luaunit.assertErrorMsgContains('Truncated encoded collection', decoder.decode, '\10', list_type)
  luaunit.assertError(decoder.decode, '\10\5ab', list_type)
  luaunit.assertError(decoder.decode, '\255', types:uint32_type())
  local dictionary_type = types:dictionary_type(types:string_type(), types:uint32_type())
  luaunit.assertErrorMsgContains('Truncated encoded dictionary entry', decoder.decode,
                                 '\10\1\10', dictionary_type)
end

function TestDecoder:test_decode_class()
  local typ = types:class_type('ServiceName', 'ClassName')
  local value = decoder.decode(platform.unhexlify('ac02'), typ)
  luaunit.assertTrue(typ.lua_type:class_of(value))
  luaunit.assertEquals(300, value._object_id)
end

function TestDecoder:test_guid()
  luaunit.assertEquals(
    '6f271b39-00dd-4de4-9732-f0d3a68838df',
    decoder.guid(platform.unhexlify('391b276fdd00e44d9732f0d3a68838df')))
end

return TestDecoder
