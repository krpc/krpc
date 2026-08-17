local luaunit = require 'luaunit'
local class = require 'pl.class'
local decoder = require 'krpc.decoder'
local platform = require 'krpc.platform'
local schema = require 'krpc.schema.KRPC'
local Types = require 'krpc.types'
local encoder = require 'krpc.encoder'
local List = require 'pl.List'

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

-- A collection is read by the tags its values are carried under rather than by parsing a
-- message, so this hands it collections the protocol buffer message layer wrote and checks the
-- values come back. A tag read wrongly, or a field renumbered in the schema, fails here.
function TestDecoder:test_decode_collections_written_by_message_layer()
  local long = string.rep('x', 300)

  local list_message = schema.List()
  for _, value in ipairs({'one', 'two', long}) do
    list_message.items:append(encoder.encode(value, types:string_type()))
  end
  local values = decoder.decode(list_message:SerializeToString(),
                                types:list_type(types:string_type()))
  luaunit.assertEquals(3, #values)
  luaunit.assertEquals('one', values[1])
  luaunit.assertEquals('two', values[2])
  luaunit.assertEquals(long, values[3])

  local empty = decoder.decode(schema.List():SerializeToString(),
                               types:list_type(types:string_type()))
  luaunit.assertEquals(0, #empty)

  local set_message = schema.Set()
  set_message.items:append(encoder.encode(7, types:uint32_type()))
  set_message.items:append(encoder.encode(300, types:uint32_type()))
  local set = decoder.decode(set_message:SerializeToString(),
                             types:set_type(types:uint32_type()))
  luaunit.assertEquals(true, set[7])
  luaunit.assertEquals(true, set[300])

  local tuple_message = schema.Tuple()
  tuple_message.items:append(encoder.encode(7, types:uint32_type()))
  tuple_message.items:append(encoder.encode(long, types:string_type()))
  local tuple = decoder.decode(
    tuple_message:SerializeToString(),
    types:tuple_type(List{types:uint32_type(), types:string_type()}))
  luaunit.assertEquals(2, #tuple)
  luaunit.assertEquals(7, tuple[1])
  luaunit.assertEquals(long, tuple[2])

  local dictionary_message = schema.Dictionary()
  local entry = dictionary_message.entries:add()
  entry.key = encoder.encode('a', types:string_type())
  entry.value = encoder.encode(1, types:uint32_type())
  entry = dictionary_message.entries:add()
  entry.key = encoder.encode('b', types:string_type())
  entry.value = encoder.encode(300, types:uint32_type())
  local dictionary = decoder.decode(
    dictionary_message:SerializeToString(),
    types:dictionary_type(types:string_type(), types:uint32_type()))
  luaunit.assertEquals(1, dictionary['a'])
  luaunit.assertEquals(300, dictionary['b'])

  -- A value that encodes to nothing, an empty list here, is left out of the entry entirely
  local empty_valued = schema.Dictionary()
  entry = empty_valued.entries:add()
  entry.key = encoder.encode('a', types:string_type())
  entry.value = ''
  local with_empty = decoder.decode(
    empty_valued:SerializeToString(),
    types:dictionary_type(types:string_type(), types:list_type(types:uint32_type())))
  luaunit.assertEquals(0, #with_empty['a'])
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
