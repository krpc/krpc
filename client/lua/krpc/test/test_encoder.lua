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

-- encode_request writes the field numbers and wire types of Request, ProcedureCall and Argument
-- itself rather than going through the protocol buffer message layer, so this reads what it
-- wrote back through that layer and checks the call arrived intact. A tag written wrongly, a
-- length counted wrongly, or a field renumbered in the schema all show up here.
--
-- The comparison is on what the message carries rather than on its bytes, because the message
-- layer writes the fields of a message in the order a hash table happens to hold them, so the
-- bytes it produces for the same call differ from these and from each other.
function TestEncoder:test_encode_request_round_trips_through_message_layer()
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
    local request = schema.Request()
    request:ParseFromString(
      without_size_prefix(encoder.encode_request(service, procedure, arguments)))
    luaunit.assertEquals(1, #request.calls)
    local call = request.calls[1]
    luaunit.assertEquals(service, call.service)
    luaunit.assertEquals(procedure, call.procedure)
    luaunit.assertEquals(#arguments, #call.arguments)
    for i = 1, #arguments do
      local argument = call.arguments[i]
      luaunit.assertEquals(i-1, argument.position)
      -- HasField answers with the value a field holds, and so with nil where it holds none
      if arguments[i] == Types.none then
        luaunit.assertEquals(true, argument.is_null)
        luaunit.assertNil(argument:HasField('value'))
      else
        luaunit.assertEquals(arguments[i], argument.value)
        luaunit.assertNil(argument:HasField('is_null'))
      end
    end
  end
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
