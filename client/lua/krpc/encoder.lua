local pb = require 'protobuf.pb'
local pb_encoder = require 'protobuf.encoder'
local seq = require 'pl.seq'
local tablex = require 'pl.tablex'
local schema = require 'krpc.schema.KRPC'
local Types = require 'krpc.types'

local encoder = {}

local _types = Types()

local function _encode_varint(x)
  if x < 0 then
    error('Value must be non-negative, got ' .. x)
  elseif x == math.huge then
    return '\255\255\255\255\255\255\255\255\127'
  else
    local data = ''
    local function write(y)
      data = y
    end
    pb.varint_encoder(write, x)
    return data
  end
end

local function _encode_float(value)
  local data = ''
  local function write(x)
    data = data .. x
  end
  local encoder = pb_encoder.FloatEncoder(1,False,False)
  encoder(write, value)
  return data:sub(2) -- strips the tag value
end

local function _encode_double(value)
  local data = ''
  local function write(x)
    data = data .. x
  end
  local encoder = pb_encoder.DoubleEncoder(1,False,False)
  encoder(write, value)
  return data:sub(2) -- strips the tag value
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

function encoder.client_name(name)
  name = name or ''
  name = name:sub(1, encoder.CLIENT_NAME_LENGTH)
  return name .. string.rep('\0', encoder.CLIENT_NAME_LENGTH - name:len())
end

function encoder.encode(x, typ)
  if typ:is_a(Types.MessageType) then
    return x:SerializeToString()
  elseif typ:is_a(Types.ValueType) then
    return _encode_value(x, typ)
  elseif typ:is_a(Types.EnumerationType) then
    return _encode_value(x.value, _types:sint32_type())
  elseif typ:is_a(Types.ClassType) then
    local object_id = 0
    if x then
      object_id = x._object_id
    end
    return _encode_value(object_id, _types:uint64_type())
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
