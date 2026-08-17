local pb = require 'protobuf.pb'
local tablex = require 'pl.tablex'
local schema = require 'krpc.schema.KRPC'
local Types = require 'krpc.types'

local encoder = {}

-- Where the routines below leave the bytes they produce. They are handed to a writer, and one
-- writer shared by all of them costs nothing per value, where a closure made for the value
-- would be allocated and thrown away again for every one encoded.
local _written
local function _write(data)
  _written = data
end

-- What pb.struct_pack calls a single precision and a double precision number.
local FLOAT_FORMAT = string.byte('f')
local DOUBLE_FORMAT = string.byte('d')

-- The encoding of every varint that fits in one byte, which is most of the values a call
-- carries: an index, a count, a small identifier or a boolean.
local _small_varints = {}
for value = 0, 127 do
  _small_varints[value] = string.char(value)
end

local function _encode_varint(x)
  local small = _small_varints[x]
  if small then
    return small
  end
  if x < 0 then
    error('Value must be non-negative, got ' .. x)
  elseif x == math.huge then
    return '\255\255\255\255\255\255\255\255\127'
  else
    pb.varint_encoder(_write, x)
    return _written
  end
end

local function _encode_float(value)
  pb.struct_pack(_write, FLOAT_FORMAT, value)
  return _written
end

local function _encode_double(value)
  pb.struct_pack(_write, DOUBLE_FORMAT, value)
  return _written
end

-- How to encode a value, by the code of the type it has. Which of these a type wants is the
-- first thing encoding a value asks, and a table says so in one lookup where asking a type what
-- class it is walks its ancestry once per question.
local _value_encoders = {
  [Types.DOUBLE] = _encode_double,
  [Types.FLOAT] = _encode_float,
  [Types.SINT32] = function(x) return _encode_varint(pb.zig_zag_encode32(x)) end,
  [Types.SINT64] = function(x) return _encode_varint(pb.zig_zag_encode64(x)) end,
  [Types.UINT32] = _encode_varint,
  [Types.UINT64] = _encode_varint,
  [Types.BOOL] = function(x) return _encode_varint(x and 1 or 0) end,
  [Types.STRING] = function(x) return _encode_varint(x:len()) .. x end,
  [Types.BYTES] = function(x) return _encode_varint(x:len()) .. x end,
  [Types.ENUMERATION] = function(x) return _encode_varint(pb.zig_zag_encode32(x.value)) end,
  [Types.CLASS] = function(x) return _encode_varint(x._object_id) end,
}

-- The tags the messages written here are written with, each a field's number and wire type in
-- one byte. From krpc.proto:
--
--   Request.calls             1, length delimited
--   ProcedureCall.service     1, length delimited
--   ProcedureCall.procedure   2, length delimited
--   ProcedureCall.arguments   3, length delimited
--   Argument.position         1, varint
--   Argument.value            2, length delimited
--   Argument.is_null          3, varint
--   Tuple.items               1, length delimited
--   List.items                1, length delimited
--   Set.items                 1, length delimited
--   Dictionary.entries        1, length delimited
--   DictionaryEntry.key       1, length delimited
--   DictionaryEntry.value     2, length delimited
--
-- test_encoder reads back what is written with these through the protocol buffer message layer
-- and checks it arrived intact, so a field renumbered in the schema without these following, or
-- a tag written wrongly, fails there.
local _REQUEST_CALLS = '\10'
local _CALL_SERVICE = '\10'
local _CALL_PROCEDURE = '\18'
local _CALL_ARGUMENTS = '\26'
local _ARGUMENT_POSITION = '\8'
local _ARGUMENT_VALUE = '\18'
local _ARGUMENT_IS_NULL = '\24'
local _ITEMS = '\10'
local _ENTRIES = '\10'
local _ENTRY_KEY = '\10'
local _ENTRY_VALUE = '\18'

local _concat = table.concat

local function _delimited(tag, data)
  return tag .. _encode_varint(data:len()) .. data
end

function encoder.encode(x, typ)
  local code = typ.code
  local encode_value = _value_encoders[code]
  if encode_value then
    return encode_value(x)
  end
  if code == Types.LIST then
    local value_type = typ.value_type
    local parts = {}
    local count = 0
    for item in x:iter() do
      count = count + 1
      parts[count] = _delimited(_ITEMS, encoder.encode(item, value_type))
    end
    return _concat(parts)
  elseif code == Types.DICTIONARY then
    local key_type = typ.key_type
    local value_type = typ.value_type
    local parts = {}
    local count = 0
    for key,value in tablex.sort(x) do
      local entry = _delimited(_ENTRY_KEY, encoder.encode(key, key_type)) ..
                    _delimited(_ENTRY_VALUE, encoder.encode(value, value_type))
      count = count + 1
      parts[count] = _delimited(_ENTRIES, entry)
    end
    return _concat(parts)
  elseif code == Types.SET then
    local value_type = typ.value_type
    local parts = {}
    local count = 0
    for item in pairs(x) do
      count = count + 1
      parts[count] = _delimited(_ITEMS, encoder.encode(item, value_type))
    end
    return _concat(parts)
  elseif code == Types.TUPLE then
    local parts = {}
    local count = 0
    for _,item in ipairs(tablex.zip(x, typ.value_types)) do
      count = count + 1
      parts[count] = _delimited(_ITEMS, encoder.encode(item[1], item[2]))
    end
    return _concat(parts)
  elseif code == Types.STRUCT then
    -- A structure is encoded as the values of its fields in order, which is the same
    -- encoding as a tuple of those values
    local field_names = typ.field_names
    local field_types = typ.field_types
    local parts = {}
    for i = 1, #field_names do
      parts[i] = _delimited(_ITEMS, encoder.encode(x[field_names[i]], field_types[i]))
    end
    return _concat(parts)
  elseif typ:is_a(Types.MessageType) then
    return x:SerializeToString()
  end
  error('Cannot encode object of type ' .. tostring(typ))
end

function encoder.encode_message_with_size(message)
  -- Encode a message prefixed by its size
  local data = message:SerializeToString()
  local delimiter = _encode_varint(data:len())
  return delimiter .. data
end

--- Encode the request carrying one procedure call, prefixed by its size, ready to send.
--
-- The arguments are the already encoded value of each, in order, with Types.none where a call
-- passes nothing. Written here rather than built as a protocol buffer message and serialized:
-- the message layer allocates a table of fields and a pair of listeners for each of the three
-- messages a call needs, and then walks all of them twice, once to size them and once to write
-- them. That costs an order of magnitude more than the bytes it produces.
function encoder.encode_request(service, procedure, arguments)
  local parts = { _delimited(_CALL_SERVICE, service), _delimited(_CALL_PROCEDURE, procedure) }
  local count = 2
  for i = 1, #arguments do
    local value = arguments[i]
    local argument
    if value == Types.none then
      argument = _ARGUMENT_POSITION .. _encode_varint(i-1) .. _ARGUMENT_IS_NULL .. '\1'
    else
      argument = _ARGUMENT_POSITION .. _encode_varint(i-1) .. _delimited(_ARGUMENT_VALUE, value)
    end
    count = count + 1
    parts[count] = _delimited(_CALL_ARGUMENTS, argument)
  end
  local request = _delimited(_REQUEST_CALLS, _concat(parts))
  return _encode_varint(request:len()) .. request
end

return encoder
