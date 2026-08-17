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

function encoder.encode(x, typ)
  local code = typ.code
  local encode_value = _value_encoders[code]
  if encode_value then
    return encode_value(x)
  end
  if code == Types.LIST then
    local msg = schema.List()
    for item in x:iter() do
      msg.items:append(encoder.encode(item, typ.value_type))
    end
    return msg:SerializeToString()
  elseif code == Types.DICTIONARY then
    local msg = schema.Dictionary()
    for key,value in tablex.sort(x) do
      local entry = msg.entries:add()
      entry.key = encoder.encode(key, typ.key_type)
      entry.value = encoder.encode(value, typ.value_type)
    end
    return msg:SerializeToString()
  elseif code == Types.SET then
    local msg = schema.Set()
    for item in pairs(x) do
      msg.items:append(encoder.encode(item, typ.value_type))
    end
    return msg:SerializeToString()
  elseif code == Types.TUPLE then
    local msg = schema.Tuple()
    for _,item in ipairs(tablex.zip(x, typ.value_types)) do
      msg.items:append(encoder.encode(item[1], item[2]))
    end
    return msg:SerializeToString()
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

return encoder
