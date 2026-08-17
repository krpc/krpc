local pb = require 'protobuf.pb'
local seq = require 'pl.seq'
local tablex = require 'pl.tablex'
local schema = require 'krpc.schema.KRPC'
local Types = require 'krpc.types'

local encoder = {}

local _types = Types()

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

local function _encode_value(x, typ)
  code = typ.protobuf_type.code
  if code == Types.DOUBLE then
    return _encode_double(x)
  elseif code == Types.FLOAT then
    return _encode_float(x)
  elseif code == Types.SINT32 then
    return _encode_varint(pb.zig_zag_encode32(x))
  elseif code == Types.SINT64 then
    return _encode_varint(pb.zig_zag_encode64(x))
  elseif code == Types.UINT32 or code == Types.UINT64 then
    return _encode_varint(x)
  elseif code == Types.BOOL then
    if x then
      return _encode_varint(1)
    else
      return _encode_varint(0)
    end
  elseif code == Types.STRING or code == Types.BYTES then
    return _encode_varint(x:len()) .. x
  end
  error('Failed to encode data')
end

function encoder.encode(x, typ)
  if typ:is_a(Types.MessageType) then
    return x:SerializeToString()
  elseif typ:is_a(Types.ValueType) then
    return _encode_value(x, typ)
  elseif typ:is_a(Types.EnumerationType) then
    return _encode_value(x.value, _types:sint32_type())
  elseif typ:is_a(Types.ClassType) then
    return _encode_value(x._object_id, _types:uint64_type())
  elseif typ:is_a(Types.ListType) then
    local msg = schema.List()
    for item in x:iter() do
      msg.items:append(encoder.encode(item, typ.value_type))
    end
    return msg:SerializeToString()
  elseif typ:is_a(Types.DictionaryType) then
    local msg = schema.Dictionary()
    local entry_type = schema.DictionaryEntry()
    for key,value in tablex.sort(x) do
      local entry = msg.entries:add()
      entry.key = encoder.encode(key, typ.key_type)
      entry.value = encoder.encode(value, typ.value_type)
    end
    return msg:SerializeToString()
  elseif typ:is_a(Types.SetType) then
    local msg = schema.Set()
    for item in pairs(x) do
      msg.items:append(encoder.encode(item, typ.value_type))
    end
    return msg:SerializeToString()
  elseif typ:is_a(Types.TupleType) then
    local msg = schema.Tuple()
    for _,item in ipairs(tablex.zip(x, typ.value_types)) do
      msg.items:append(encoder.encode(item[1], item[2]))
    end
    return msg:SerializeToString()
  else
    error('Cannot encode object of type ' .. tostring(typ))
  end
end

function encoder.encode_message_with_size(message)
  -- Encode a message prefixed by its size
  local data = message:SerializeToString()
  local delimiter = _encode_varint(data:len())
  return delimiter .. data
end

return encoder
