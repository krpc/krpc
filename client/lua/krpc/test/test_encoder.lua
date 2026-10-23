local luaunit = require 'luaunit'
local class = require 'pl.class'
local encoder = require 'krpc.encoder'
local platform = require 'krpc.platform'
local Types = require 'krpc.types'
local protobuf = require 'krpc.protobuf'
local List = require 'pl.List'
local Map = require 'pl.Map'
local Set = require 'pl.Set'

local TestEncoder = class()

local types = Types()

function TestEncoder:test_encode_message()
  local call = { service = 'ServiceName', procedure = 'ProcedureName' }
  local data = encoder.encode(call, types:procedure_call_type())
  local expected = '0a0b536572766963654e616d65120d50726f6365647572654e616d65'
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

-- Take the size prefix off a message and check it counted the rest correctly.
local function without_size_prefix(data)
  local size = 0
  local shift = 1
  local position = 1
  while true do
    local byte = data:byte(position)
    position = position + 1
    size = size + (byte % 128) * shift
    if byte < 128 then
      break
    end
    shift = shift * 128
  end
  local body = data:sub(position)
  luaunit.assertEquals(size, body:len())
  return body
end

-- encode_request writes the field numbers and wire types of Message, Request, ProcedureCall and
-- Argument itself, so this decodes what it wrote as a Message and checks the call arrived
-- intact. A tag written wrongly, a length counted wrongly, or a field renumbered in the schema
-- all show up here.
function TestEncoder:test_encode_request_decodes_as_a_message()
  local long_name = string.rep('a', 200)
  local long_value = string.rep('\7', 300)
  local cases = {
    { 'ServiceName', 'ProcedureName', {} },
    { 'ServiceName', 'ProcedureName', { '\42' } },
    { 'ServiceName', 'ProcedureName', { '\1', '\2', '\3' } },
    { 'ServiceName', 'ProcedureName', { Types.none } },
    { 'ServiceName', 'ProcedureName', { '\1', Types.none, '\3' } },
    -- Lengths that take more than one byte to write, for the service and procedure names, for
    -- an argument's value, and for the request as a whole
    { long_name, 'ProcedureName', {} },
    { 'ServiceName', long_name, {} },
    { 'ServiceName', 'ProcedureName', { long_value } },
    { long_name, long_name, { long_value, long_value } },
    -- Enough arguments that their positions stop fitting in one byte
    { 'ServiceName', 'ProcedureName', (function()
        local many = {}
        for i = 1, 200 do many[i] = '\1' end
        return many
      end)() },
  }
  for _, case in ipairs(cases) do
    local service, procedure, arguments = case[1], case[2], case[3]
    local message = protobuf.decode(
      'Message', without_size_prefix(encoder.encode_request(300, service, procedure, arguments)))
    local request = message.request
    luaunit.assertEquals(300, request.id)
    luaunit.assertEquals(1, #request.calls)
    local call = request.calls[1]
    luaunit.assertEquals(service, call.service)
    luaunit.assertEquals(procedure, call.procedure)
    luaunit.assertEquals(#arguments, #call.arguments)
    for i = 1, #arguments do
      local argument = call.arguments[i]
      luaunit.assertEquals(i-1, argument.position)
      -- A field the argument leaves out decodes to its zero value
      if arguments[i] == Types.none then
        luaunit.assertEquals(true, argument.is_null)
        luaunit.assertEquals('', argument.value)
      else
        luaunit.assertEquals(arguments[i], argument.value)
        luaunit.assertEquals(false, argument.is_null)
      end
    end
  end
end

-- A list, a set, a tuple and a dictionary are written the same way the request is, so they are
-- decoded as messages for the same reason.
function TestEncoder:test_encode_collections_decode_as_messages()
  local long = string.rep('x', 300)

  local list_type = types:list_type(types:string_type())
  local values = List{'one', 'two', long}
  local list_message = protobuf.decode('List', encoder.encode(values, list_type))
  luaunit.assertEquals(3, #list_message.items)
  for i, item in ipairs(list_message.items) do
    luaunit.assertEquals(encoder.encode(values[i], types:string_type()), item)
  end

  local set_type = types:set_type(types:uint32_type())
  local set_message = protobuf.decode('Set', encoder.encode(Set{7}, set_type))
  luaunit.assertEquals(1, #set_message.items)
  luaunit.assertEquals(encoder.encode(7, types:uint32_type()), set_message.items[1])

  local tuple_type = types:tuple_type(List{types:uint32_type(), types:string_type()})
  local tuple_message = protobuf.decode('Tuple', encoder.encode(List{7, long}, tuple_type))
  luaunit.assertEquals(2, #tuple_message.items)
  luaunit.assertEquals(encoder.encode(7, types:uint32_type()), tuple_message.items[1])
  luaunit.assertEquals(encoder.encode(long, types:string_type()), tuple_message.items[2])

  local dictionary_type = types:dictionary_type(types:string_type(), types:uint32_type())
  local dictionary_message = protobuf.decode(
    'Dictionary', encoder.encode(Map{a = 1, b = 2}, dictionary_type))
  luaunit.assertEquals(2, #dictionary_message.entries)
  luaunit.assertEquals(encoder.encode('a', types:string_type()),
                       dictionary_message.entries[1].key)
  luaunit.assertEquals(encoder.encode(1, types:uint32_type()),
                       dictionary_message.entries[1].value)
  luaunit.assertEquals(encoder.encode('b', types:string_type()),
                       dictionary_message.entries[2].key)
  luaunit.assertEquals(encoder.encode(2, types:uint32_type()),
                       dictionary_message.entries[2].value)
end

function TestEncoder:test_encode_class()
  local typ = types:class_type('ServiceName', 'ClassName')
  local class_type = typ.lua_type
  local value = class_type(300)
  luaunit.assertEquals(300, value._object_id)
  local data = encoder.encode(value, typ)
  luaunit.assertEquals('ac02', platform.hexlify(data))
end

return TestEncoder
