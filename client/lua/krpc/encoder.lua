local buffer = require 'pb.buffer'
local tablex = require 'pl.tablex'
local protobuf = require 'krpc.protobuf'
local Types = require 'krpc.types'

local encoder = {}

-- Called with a format and no buffer, pack returns the bytes it writes
local _pack = buffer.pack

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
    return _pack('v', x)
  end
end

local function _encode_float(value)
  return _pack('f', value)
end

local function _encode_double(value)
  return _pack('F', value)
end

-- The encoder for each value type, by type code. Encoding a value starts here, and a table
-- resolves it in one lookup, where testing a type's class walks its ancestry every time.
local _value_encoders = {
  [Types.DOUBLE] = _encode_double,
  [Types.FLOAT] = _encode_float,
  [Types.SINT32] = function(x) return _pack('j', x) end,
  [Types.SINT64] = function(x) return _pack('J', x) end,
  [Types.UINT32] = _encode_varint,
  [Types.UINT64] = _encode_varint,
  [Types.BOOL] = function(x) return _encode_varint(x and 1 or 0) end,
  [Types.STRING] = function(x) return _encode_varint(x:len()) .. x end,
  [Types.BYTES] = function(x) return _encode_varint(x:len()) .. x end,
  [Types.ENUMERATION] = function(x) return _pack('j', x.value) end,
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
-- These must match krpc.proto, which test_encoder checks by decoding what is written with them
-- as messages.
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

-- Encode one value at a position inside another value, which the given type describes. A
-- nullable position carries a presence bool before its value, and holds nothing else when that
-- bool is false.
local function _encode_item(x, typ)
  local absent = x == nil or x == Types.none
  if not typ.nullable then
    -- A position that cannot hold null takes a value of its type and nothing else
    if absent then
      error('A null cannot be encoded at a position that cannot hold one')
    end
    return encoder.encode(x, typ)
  end
  local encode_bool = _value_encoders[Types.BOOL]
  if absent then
    return encode_bool(false)
  end
  return encode_bool(true) .. encoder.encode(x, typ)
end

-- Encode a value at a slot, where a null is carried by the is_null flag of the message around
-- it. A value at a position inside another value carries its own presence bool, which
-- _encode_item writes.
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
      parts[count] = _delimited(_ITEMS, _encode_item(item, value_type))
    end
    return _concat(parts)
  elseif code == Types.DICTIONARY then
    local key_type = typ.key_type
    local value_type = typ.value_type
    local parts = {}
    local count = 0
    for key,value in tablex.sort(x) do
      local entry = _delimited(_ENTRY_KEY, _encode_item(key, key_type)) ..
                    _delimited(_ENTRY_VALUE, _encode_item(value, value_type))
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
      parts[count] = _delimited(_ITEMS, _encode_item(item, value_type))
    end
    return _concat(parts)
  elseif code == Types.TUPLE then
    local value_types = typ.value_types
    local parts = {}
    for i = 1, #value_types do
      parts[i] = _delimited(_ITEMS, _encode_item(x[i], value_types[i]))
    end
    return _concat(parts)
  elseif code == Types.STRUCT then
    -- A structure is encoded as the values of its fields in order, which is the same
    -- encoding as a tuple of those values
    local field_names = typ.field_names
    local field_types = typ.field_types
    local parts = {}
    for i = 1, #field_names do
      parts[i] = _delimited(_ITEMS, _encode_item(x[field_names[i]], field_types[i]))
    end
    return _concat(parts)
  elseif typ:is_a(Types.MessageType) then
    return protobuf.encode(typ.message_type, x)
  end
  error('Cannot encode object of type ' .. tostring(typ))
end

function encoder.encode_message_with_size(type_name, message)
  -- Encode a message of the named type, prefixed by its size
  local data = protobuf.encode(type_name, message)
  local delimiter = _encode_varint(data:len())
  return delimiter .. data
end

--- Encode the request carrying one procedure call, prefixed by its size, ready to send.
--
-- The arguments are the already encoded value of each, in order, with Types.none where a call
-- passes nothing. Written here and not encoded from tables of the three messages a call needs,
-- which would be built for every call.
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
