local List = require 'pl.List'
local Set = require 'pl.Set'
local Map = require 'pl.Map'
local utils = require 'pl.utils'
local compat = require 'krpc.compat'
local platform = require 'krpc.platform'
local protobuf = require 'krpc.protobuf'
local Types = require 'krpc.types'

local decoder = {}

decoder.OK_MESSAGE = '\79\75'

-- Returns nil for data with no bytes, which we read as the zero value, as a protobuf field left
-- out reads. Raises an error for data cut short.
local _unpack = protobuf.unpack
local _decode_message = protobuf.decode_inside_call

-- Without an integer type, the encoder writes math.huge as 2^63 - 1, which reads back as it
local _huge = not compat.native_integers and math.huge or nil

local function _decode_varint(data)
  -- A varint that fits in one byte is most of the values a call carries, and reading it needs
  -- no call into the C decoder
  local byte = data:byte(1)
  if byte and byte < 128 then
    return byte
  end
  if _huge and data == '\255\255\255\255\255\255\255\255\127' then
    return _huge
  end
  return _unpack(data, 'v') or 0
end

local function _decode_float(data)
  return _unpack(data, 'f') or 0.0
end

local function _decode_double(data)
  return _unpack(data, 'F') or 0.0
end

local function _decode_string(data)
  return _unpack(data, 's') or ''
end

local function _decode_signed(data)
  return _unpack(data, 'j') or 0
end

-- Decode a uint64. Without an integer type, the decoder reads a value above 2^63 - 1 as a
-- negative one, and adding 2^64 gives back the nearest double.
local function _decode_uint64(data)
  local value = _decode_varint(data)
  if value < 0 and not compat.native_integers then
    value = value + 18446744073709551616
  end
  return value
end

-- The decoder for each value type, by type code. Decoding a value starts here, and a table
-- resolves it in one lookup, where testing a type's class walks its ancestry every time.
local _value_decoders = {
  [Types.DOUBLE] = _decode_double,
  [Types.FLOAT] = _decode_float,
  [Types.SINT32] = _decode_signed,
  [Types.SINT64] = function(data) return _unpack(data, 'J') or 0 end,
  [Types.UINT32] = _decode_varint,
  [Types.UINT64] = _decode_uint64,
  [Types.BOOL] = function(data) return _decode_varint(data) ~= 0 end,
  [Types.STRING] = _decode_string,
  [Types.BYTES] = _decode_string,
  [Types.ENUMERATION] = function(data, typ)
    return typ.lua_type(_decode_signed(data))
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
-- the encoder, which writes them, and test_decoder, which reads collections encoded as messages
-- back through these.
local _ITEMS = 10
local _ENTRY_KEY = 10
local _ENTRY_VALUE = 18

--- Read the values a collection carries, each a length delimited field under the given tag.
--
-- Read here and not by decoding a message, for the reason the encoder writes them here.
local function _decode_items(data, tag)
  local items = {}
  local count = 0
  -- The position of the next tag, counted from 1
  local position = 1
  local length = data:len()
  while position <= length do
    if data:byte(position) ~= tag then
      error('Unexpected field in an encoded collection')
    end
    count = count + 1
    items[count], position = _unpack(data, '*s@', position + 1)
    if position == nil then
      error('Truncated encoded collection')
    end
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
  local position = 1
  local length = data:len()
  while position <= length do
    local tag = data:byte(position)
    local field
    field, position = _unpack(data, '*s@', position + 1)
    if position == nil then
      error('Truncated encoded dictionary entry')
    elseif tag == _ENTRY_KEY then
      key = field
    elseif tag == _ENTRY_VALUE then
      value = field
    else
      error('Unexpected field in an encoded dictionary entry')
    end
  end
  return key, value
end

-- Declared ahead of _decode_item, which calls it
local _decode

-- Decode one value at a position inside another value, which the given type describes. A
-- nullable position carries a presence bool before its value, and holds nothing else when that
-- bool is false.
local function _decode_item(data, typ)
  if not typ.nullable then
    return _decode(data, typ)
  end
  -- The presence bool is one byte, as a bool value always is
  if data:len() == 0 then
    error('A nullable value carries no presence bool')
  end
  if not _value_decoders[Types.BOOL](data:sub(1, 1)) then
    return Types.none
  end
  return _decode(data:sub(2), typ)
end

-- Decode a value at a slot, where a null is carried by the is_null flag of the message around
-- it. A value at a position inside another value carries its own presence bool, which
-- _decode_item reads.
function _decode(data, typ)
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
    return typ.lua_type(utils.unpack(values))
  elseif typ:is_a(Types.MessageType) then
    return _decode_message(typ.message_type, data)
  end
  error('Cannot decode type ' .. tostring(typ))
end

--- Decode a value of the given type, with the client's protobuf state swapped in once for the
--- whole of it
function decoder.decode(data, typ)
  return protobuf.call(_decode, data, typ)
end

-- The Message, and the value of the one result of the response it holds when the given type is
-- not nil and the result carries a value
local function _decode_message_and_value(data, typ)
  local ok, message = pcall(_decode_message, 'Message', data)
  if not ok then
    return nil
  end
  local response = message.response
  if response == nil or response.error ~= nil then
    return message
  end
  local result = response.results[1]
  if result == nil then
    return nil
  end
  if typ == nil or result.error ~= nil or result.is_null then
    return message
  end
  return message, _decode(result.value, typ)
end

--- Decode a Message and the value of the result of the response it holds, with the client's
--- protobuf state swapped in once for both. A field of the Message that the data leaves out
--- reads as nil, so a Message holding a case this client does not know has none of them set.
--- Returns nil for a Message that fails to parse, or a response with no result.
function decoder.decode_message_and_value(data, typ)
  return protobuf.call(_decode_message_and_value, data, typ)
end

--- Whether the data holds whole protobuf fields, read at the top level only. lua-protobuf stops
--- without an error at a field cut short, so a message that fails to parse reads as one with
--- no field set.
function decoder.well_formed(data)
  local length = #data
  local i = 1
  local function varint()
    local value = 0
    local shift = 1
    for _ = 1, 10 do
      local byte = data:byte(i)
      if byte == nil then
        return nil
      end
      i = i + 1
      value = value + (byte % 128) * shift
      if byte < 128 then
        return value
      end
      shift = shift * 128
    end
    return nil
  end
  while i <= length do
    local tag = varint()
    if tag == nil or tag < 8 then
      return false
    end
    local wire_type = tag % 8
    if wire_type == 0 then
      if varint() == nil then
        return false
      end
    elseif wire_type == 1 then
      i = i + 8
    elseif wire_type == 2 then
      local size = varint()
      if size == nil then
        return false
      end
      i = i + size
    elseif wire_type == 5 then
      i = i + 4
    else
      return false
    end
    if i > length + 1 then
      return false
    end
  end
  return true
end

--- Decode a message of the named type in the krpc.schema package
function decoder.decode_message(data, type_name)
  return protobuf.decode(type_name, data)
end

function decoder.decode_size(data)
  return protobuf.call(_decode_varint, data)
end

return decoder
