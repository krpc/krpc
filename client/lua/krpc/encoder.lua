local pb = require 'protobuf.pb'
local pb_encoder = require 'protobuf.encoder'
local seq = require 'pl.seq'
local tablex = require 'pl.tablex'
local Types = require 'krpc.types'

local encoder = {}

local _types = Types()

encoder.RPC_HELLO_MESSAGE = '\72\69\76\76\79\45\82\80\67\0\0\0'
encoder.STREAM_HELLO_MESSAGE = '\72\69\76\76\79\45\83\84\82\69\65\77'

encoder.CLIENT_NAME_LENGTH = 32
encoder.CLIENT_IDENTIFIER_LENGTH = 16

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

local function _encode_signed_varint(x)
  if x == math.huge then
    return '\255\255\255\255\255\255\255\255\127'
  elseif x == -math.huge then
    return '\128\128\128\128\128\128\128\128\128\1'
  else
    local data = ''
    local function write(y)
      data = y
    end
    pb.signed_varint_encoder(write, x)
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
  typ = typ.protobuf_type
  if typ == 'uint32' or typ == 'uint64' then
    return _encode_varint(x)
  elseif typ == 'int32' or typ == 'int64' then
    return _encode_signed_varint(x)
  elseif typ == 'bool' then
    if x then
      return _encode_varint(1)
    else
      return _encode_varint(0)
    end
  elseif typ == 'string' or typ == 'bytes'then
    return _encode_varint(x:len()) .. x
  elseif typ == 'float' then
    return _encode_float(x)
  elseif typ == 'double' then
    return _encode_double(x)
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
  elseif typ:is_a(Types.EnumType) then
    return _encode_value(x.value, _types:as_type('int32'))
  elseif typ:is_a(Types.ClassType) then
    local object_id = 0
    if x then
      object_id = x._object_id
    end
    return _encode_value(object_id, _types:as_type('uint64'))
  elseif typ:is_a(Types.ListType) then
    local msg = _types:as_type('KRPC.List').lua_type()
    for item in x:iter() do
      msg.items:append(encoder.encode(item, typ.value_type))
    end
    return msg:SerializeToString()
  elseif typ:is_a(Types.DictionaryType) then
    local msg = _types:as_type('KRPC.Dictionary').lua_type()
    local entry_type = _types:as_type('KRPC.DictionaryEntry')
    for key,value in tablex.sort(x) do
      local entry = msg.entries:add()
      entry.key = encoder.encode(key, typ.key_type)
      entry.value = encoder.encode(value, typ.value_type)
    end
    return msg:SerializeToString()
  elseif typ:is_a(Types.SetType) then
    local msg = _types:as_type('KRPC.Set').lua_type()
    for item in pairs(x) do
      msg.items:append(encoder.encode(item, typ.value_type))
    end
    return msg:SerializeToString()
  elseif typ:is_a(Types.TupleType) then
    local msg = _types:as_type('KRPC.Tuple').lua_type()
    for _,item in ipairs(tablex.zip(x, typ.value_types)) do
      msg.items:append(encoder.encode(item[1], item[2]))
    end
    return msg:SerializeToString()
  else
    error('Cannot encode object of type ' .. tostring(typ))
  end
end

function encoder.encode_delimited(x, typ)
  -- Encode a message or value with size information
  -- (for use in a delimited communication stream)
  local data = encoder.encode(x, typ)
  local delimiter = _encode_varint(data:len())
  return delimiter .. data
end

return encoder
