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

-- The pb.struct_unpack format codes for a single precision and a double precision number.
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

-- The decoder for each value type, by type code. Decoding a value starts here, and a table
-- resolves it in one lookup, where testing a type's class walks its ancestry every time.
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

-- The tag every value of a collection is carried under: field number 1, length delimited, for
-- the items of a Tuple, a List and a Set and for the entries of a Dictionary. A DictionaryEntry
-- carries its key under the same tag and its value under field number 2. See the same list in
-- the encoder, which writes them, and test_decoder, which reads collections written by the
-- protocol buffer message layer back through these.
local _ITEMS = 10
local _ENTRY_KEY = 10
local _ENTRY_VALUE = 18

--- Read the values a collection carries, each a length delimited field under the given tag.
--
-- Read here and not by parsing a message, for the reason the encoder writes them here: the
-- protocol buffer library is written in lua, and parsing a message of a hundred values into a
-- container that type checks each one costs far more than reading the bytes does.
local function _decode_items(data, tag)
  local items = {}
  local count = 0
  local position = 0
  local length = data:len()
  while position < length do
    if data:byte(position+1) ~= tag then
      error('Unexpected field in an encoded collection')
    end
    local size, after = pb.varint_decoder(data, position+1)
    items[count+1] = data:sub(after+1, after+size)
    count = count + 1
    position = after + size
  end
  return items, count
end

--- Read the key and the value out of one entry of a dictionary.
--
-- The two are read by the tag in front of each, as a message carries its fields in whatever
-- order the writer chose.
local function _decode_entry(data)
  -- A key or a value that encodes to nothing, such as an empty collection or an empty
  -- string, is left out of the entry entirely and reads back empty
  local key = ''
  local value = ''
  local position = 0
  local length = data:len()
  while position < length do
    local tag = data:byte(position+1)
    local size, after = pb.varint_decoder(data, position+1)
    local field = data:sub(after+1, after+size)
    if tag == _ENTRY_KEY then
      key = field
    elseif tag == _ENTRY_VALUE then
      value = field
    else
      error('Unexpected field in an encoded dictionary entry')
    end
    position = after + size
  end
  return key, value
end

-- Decode one value at a position inside another value, which the given type describes. A
-- nullable position carries a presence bool before its value, and holds nothing else when that
-- bool is false.
local function _decode_item(data, typ)
  if not typ.nullable then
    return decoder.decode(data, typ)
  end
  -- The presence bool is one byte, as a bool value always is
  if data:len() == 0 then
    error('A nullable value carries no presence bool')
  end
  if not _value_decoders[Types.BOOL](data:sub(1, 1)) then
    return Types.none
  end
  return decoder.decode(data:sub(2), typ)
end

-- Decode a value at a slot, where a null is carried by the is_null flag of the message around
-- it. A value at a position inside another value carries its own presence bool, which
-- _decode_item reads.
function decoder.decode(data, typ)
  local code = typ.code
  local decode_value = _value_decoders[code]
  if decode_value then
    return decode_value(data, typ)
  end
  if code == Types.LIST then
    local items, count = _decode_items(data, _ITEMS)
    local value_type = typ.value_type
    local result = List{}
    for i = 1, count do
      result[i] = _decode_item(items[i], value_type)
    end
    return result
  elseif code == Types.DICTIONARY then
    local entries, count = _decode_items(data, _ITEMS)
    local key_type = typ.key_type
    local value_type = typ.value_type
    local result = Map{}
    for i = 1, count do
      local key, value = _decode_entry(entries[i])
      result[_decode_item(key, key_type)] = _decode_item(value, value_type)
    end
    return result
  elseif code == Types.SET then
    local items, count = _decode_items(data, _ITEMS)
    local value_type = typ.value_type
    local result = Set{}
    for i = 1, count do
      result[_decode_item(items[i], value_type)] = true
    end
    return result
  elseif code == Types.TUPLE then
    local items, count = _decode_items(data, _ITEMS)
    local value_types = typ.value_types
    local result = List{}
    for i = 1, count do
      result[i] = _decode_item(items[i], value_types[i])
    end
    return result
  elseif code == Types.STRUCT then
    -- Fields are only ever appended to a structure, so a value from a newer server may
    -- carry more items than this client knows about, and those are ignored
    local items, count = _decode_items(data, _ITEMS)
    local field_types = typ.field_types
    if count < #field_types then
      error('Value for ' .. typ._struct_name .. ' does not have all of its fields')
    end
    local values = {}
    for i = 1, #field_types do
      values[i] = _decode_item(items[i], field_types[i])
    end
    return typ.lua_type(unpack(values))
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
