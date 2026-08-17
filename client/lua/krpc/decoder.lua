local pb = require 'protobuf.pb'
local List = require 'pl.List'
local Set = require 'pl.Set'
local Map = require 'pl.Map'
local tablex = require 'pl.tablex'
local platform = require 'krpc.platform'
local schema = require 'krpc.schema.KRPC'
local Types = require 'krpc.types'

local decoder = {}

decoder.OK_MESSAGE = '\79\75'

local function _decode_varint(data)
  -- A varint that fits in one byte is most of the values a call carries, and reading it needs
  -- neither the C decoder nor the position it hands back alongside the value.
  local byte = data:byte(1)
  if byte and byte < 128 then
    return byte
  end
  if data == '\255\255\255\255\255\255\255\255\127' then
    return math.huge
  end
  local value = pb.varint_decoder(data, 0)
  return value
end

-- What pb.struct_unpack calls a single precision and a double precision number.
local FLOAT_FORMAT = string.byte('f')
local DOUBLE_FORMAT = string.byte('d')

local function _decode_float(data)
  return pb.struct_unpack(FLOAT_FORMAT, data, 0)
end

local function _decode_double(data)
  return pb.struct_unpack(DOUBLE_FORMAT, data, 0)
end

local function _decode_string(data)
  local size, position = pb.varint_decoder(data, 0)
  return data:sub(position+1, position+size+1)
end

-- How to decode a value, by the code of the type it has. Which of these a type wants is the
-- first thing decoding a value asks, and a table says so in one lookup where asking a type what
-- class it is walks its ancestry once per question.
local _value_decoders = {
  [Types.DOUBLE] = _decode_double,
  [Types.FLOAT] = _decode_float,
  [Types.SINT32] = function(data) return pb.zig_zag_decode32(_decode_varint(data)) end,
  [Types.SINT64] = function(data) return pb.zig_zag_decode64(_decode_varint(data)) end,
  [Types.UINT32] = _decode_varint,
  [Types.UINT64] = _decode_varint,
  [Types.BOOL] = function(data) return _decode_varint(data) ~= 0 end,
  [Types.STRING] = _decode_string,
  [Types.BYTES] = _decode_string,
  [Types.ENUMERATION] = function(data, typ)
    return typ.lua_type(pb.zig_zag_decode32(_decode_varint(data)))
  end,
  [Types.CLASS] = function(data, typ) return typ.lua_type(_decode_varint(data)) end,
}

function decoder.guid(data)
  local parts = {
    platform.hexlify(data:sub(1,4):reverse()),
    platform.hexlify(data:sub(5,6):reverse()),
    platform.hexlify(data:sub(7,8):reverse()),
    platform.hexlify(data:sub(9,10)),
    platform.hexlify(data:sub(11,16))
  }
  return table.concat(parts, '-')
end

function decoder.decode(data, typ)
  local code = typ.code
  local decode_value = _value_decoders[code]
  if decode_value then
    return decode_value(data, typ)
  end
  if code == Types.LIST then
    local msg = decoder.decode_message(data, schema.List)
    local result = List{}
    local value_type = typ.value_type
    for _,item in ipairs(msg.items) do
      result:append(decoder.decode(item, value_type))
    end
    return result
  elseif code == Types.DICTIONARY then
    local msg = decoder.decode_message(data, schema.Dictionary)
    local result = Map{}
    for _,item in ipairs(msg.entries) do
      result[decoder.decode(item.key, typ.key_type)] =
        decoder.decode(item.value, typ.value_type)
    end
    return result
  elseif code == Types.SET then
    local msg = decoder.decode_message(data, schema.Set)
    local result = Set{}
    local value_type = typ.value_type
    for _,item in ipairs(msg.items) do
      result[decoder.decode(item, value_type)] = true
    end
    return result
  elseif code == Types.TUPLE then
    local msg = decoder.decode_message(data, schema.Tuple)
    local result = List{}
    for _,item in ipairs(tablex.zip(msg.items, typ.value_types)) do
      result:append(decoder.decode(item[1], item[2]))
    end
    return result
  elseif typ:is_a(Types.MessageType) then
    return decoder.decode_message(data, typ.lua_type)
  end
  error('Cannot decode type ' .. tostring(typ))
end

function decoder.decode_message(data, typ)
  local message = typ()
  message:ParseFromString(data)
  return message
end

function decoder.decode_size(data)
  local size, _ = pb.varint_decoder(data, 0)
  return size
end

return decoder
