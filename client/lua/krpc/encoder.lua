local buffer = require 'pb.buffer'
local tablex = require 'pl.tablex'
local compat = require 'krpc.compat'
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

local _tointeger = compat.tointeger

-- Encode an unsigned value. Without an integer type, math.huge stands for the largest value.
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

-- Without an integer type, pack reads a double as an int64, which holds neither 2^63 nor 2^64.
-- SINT64_MAX and UINT64_MAX round up to these, so we write the encoding of each maximum.
local _TWO_TO_63 = 9223372036854775808
local _TWO_TO_64 = 18446744073709551616
local _SINT64_MAX_ENCODED = _pack('v', -2)
local _UINT64_MAX_ENCODED = _pack('v', -1)

local function _encode_sint64(x)
  x = _tointeger(x)
  if x == _TWO_TO_63 and not compat.native_integers then
    return _SINT64_MAX_ENCODED
  end
  return _pack('J', x)
end

-- Encode a uint64. A value above 2^63 - 1 is encoded as the bits of the negative int64 it
-- wraps to.
local function _encode_uint64(x)
  x = _tointeger(x)
  if compat.native_integers then
    if x < 0 then
      return _pack('v', x)
    end
  elseif x == _TWO_TO_64 then
    return _UINT64_MAX_ENCODED
  elseif x >= _TWO_TO_63 and x < _TWO_TO_64 then
    return _pack('v', x - _TWO_TO_64)
  end
  return _encode_varint(x)
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
  [Types.SINT32] = function(x) return _pack('j', _tointeger(x)) end,
  [Types.SINT64] = _encode_sint64,
  [Types.UINT32] = function(x) return _encode_varint(_tointeger(x)) end,
  [Types.UINT64] = _encode_uint64,
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
local _MESSAGE_REQUEST = '\10'
local _MESSAGE_CONNECTION_REQUEST = '\34'
local _MESSAGE_DISCONNECT = '\50'
local _REQUEST_CALLS = '\10'
local _REQUEST_ID = '\16'
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
  local absent = x == nil or rawequal(x, Types.none)
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

local _MESSAGE_CASES = {
  ConnectionRequest = _MESSAGE_CONNECTION_REQUEST,
  Disconnect = _MESSAGE_DISCONNECT
}

--- Encode a Message holding a message of the named type, prefixed by its size, ready to send
function encoder.encode_in_message(type_name, message)
  local data = _delimited(_MESSAGE_CASES[type_name], protobuf.encode(type_name, message))
  return _encode_varint(data:len()) .. data
end

--- Encode a Message holding the request with the given id, which carries one procedure call,
-- prefixed by its size, ready to send.
--
-- The arguments are the already encoded value of each, in order, with Types.none where a call
-- passes nothing. Written here and not encoded from tables of the four messages a call needs,
-- which would be built for every call.
function encoder.encode_request(id, service, procedure, arguments)
  local parts = { _delimited(_CALL_SERVICE, service), _delimited(_CALL_PROCEDURE, procedure) }
  local count = 2
  for i = 1, #arguments do
    local value = arguments[i]
    local argument
    if rawequal(value, Types.none) then
      argument = _ARGUMENT_POSITION .. _encode_varint(i-1) .. _ARGUMENT_IS_NULL .. '\1'
    else
      argument = _ARGUMENT_POSITION .. _encode_varint(i-1) .. _delimited(_ARGUMENT_VALUE, value)
    end
    count = count + 1
    parts[count] = _delimited(_CALL_ARGUMENTS, argument)
  end
  local call = _concat(parts)
  local call_length = _encode_varint(call:len())
  local id_varint = _encode_varint(id)
  -- The lengths of the request and the message are worked out from their parts, so the call is
  -- copied once, into the bytes to send
  local request_length = 2 + id_varint:len() + call_length:len() + call:len()
  local request_length_varint = _encode_varint(request_length)
  local message_length = 1 + request_length_varint:len() + request_length
  return _concat({
    _encode_varint(message_length), _MESSAGE_REQUEST, request_length_varint,
    _REQUEST_ID, id_varint, _REQUEST_CALLS, call_length, call
  })
end

return encoder
