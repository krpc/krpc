local pb = require 'protobuf.pb'
local pb_decoder = require 'protobuf.decoder'
local List = require 'pl.List'
local Set = require 'pl.Set'
local Map = require 'pl.Map'
local tablex = require 'pl.tablex'
local platform = require 'krpc.platform'
local schema = require 'krpc.schema.KRPC'
local Types = require 'krpc.types'

local decoder = {}

local _types = Types()

decoder.OK_MESSAGE = '\79\75'

local function _decode_varint(data)
  if data == '\255\255\255\255\255\255\255\255\127' then
    return math.huge
  else
    return pb.varint_decoder(data, 0)
  end
end

local function _decode_signed_varint(data)
  if data == '\255\255\255\255\255\255\255\255\127' then
    return math.huge
  elseif data == '\128\128\128\128\128\128\128\128\128\1' then
    return -math.huge
  else
    return pb.signed_varint_decoder(data, 0)
  end
end

local function _decode_float(data)
  local field_dict = {}
  local key = 1
  local decoder = pb_decoder.FloatDecoder(1,False,False,key,nil)
  local pos = decoder(data, 0, data:len(), nil, field_dict)
  return field_dict[1]
end

local function _decode_double(data)
  local field_dict = {}
  local key = 1
  local decoder = pb_decoder.DoubleDecoder(1,False,False,key,nil)
  local pos = decoder(data, 0, data:len(), nil, field_dict)
  return field_dict[1]
end

local function _decode_message(data, typ)
  local message = typ()
  message:ParseFromString(data)
  return message
end

local function _decode_value(data, typ)
  code = typ.protobuf_type.code
  if code == Types.DOUBLE then
    return _decode_double(data)
  elseif code == Types.FLOAT then
    return _decode_float(data)
  elseif code == Types.INT32 or code == Types.INT64 then
    return _decode_signed_varint(data)
  elseif code == Types.UINT32 or code == Types.UINT64 then
    return _decode_varint(data)
  elseif code == Types.BOOL then
    local x = _decode_varint(data)
    return x ~= 0
  elseif code == Types.STRING or code == Types.BYTES then
    local size, position = decoder.decode_size_and_position(data)
    return data:sub(position+1, position+size+1)
  end
  error('Failed to decode data')
end

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
  if typ:is_a(Types.MessageType) then
    return _decode_message(data, typ.lua_type)
  elseif typ:is_a(Types.EnumerationType) then
    return typ.lua_type(_decode_value(data, _types:int32_type()))
  elseif typ:is_a(Types.ValueType) then
    return _decode_value(data, typ)
  elseif typ:is_a(Types.ClassType) then
    local object_id_typ = _types:uint64_type()
    local object_id = _decode_value(data, object_id_typ)
    if object_id == 0 then
      return Types.none
    else
      return typ.lua_type(object_id)
    end
  elseif typ:is_a(Types.ListType) then
    local msg = _decode_message(data, schema.List)
    local result = List{}
    for _,item in ipairs(msg.items) do
      result:append(decoder.decode(item, typ.value_type))
    end
    return result
  elseif typ:is_a(Types.DictionaryType) then
    local msg = _decode_message(data, schema.Dictionary)
    local result = Map{}
    for _,item in ipairs(msg.entries) do
       key = decoder.decode(item.key, typ.key_type)
       value = decoder.decode(item.value, typ.value_type)
      result[key] = value
    end
    return result
  elseif typ:is_a(Types.SetType) then
    local msg = _decode_message(data, schema.Set)
    local result = Set{}
    for _,item in ipairs(msg.items) do
      result[decoder.decode(item, typ.value_type)] = true
    end
    return result
  elseif typ:is_a(Types.TupleType) then
    local msg = _decode_message(data, schema.Tuple)
    local result = List{}
    for _,item in ipairs(tablex.zip(msg.items, typ.value_types)) do
      result:append(decoder.decode(item[1], item[2]))
    end
    return result
  else
    error('Cannot decode type ' .. tostring(typ))
  end
end

function decoder.decode_size_and_position(data)
  return pb.varint_decoder(data, 0)
end

function decoder.decode_delimited(data, typ)
  -- Decode a message or value with size information
  -- (used in a delimited communication stream)
  local size, position = decoder.decode_size_and_position(data)
  return decoder.decode(data:sub(position+1,position+size+1), typ)

end

return decoder
