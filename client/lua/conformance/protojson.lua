-- Protocol buffer messages in the proto3 JSON mapping, for lua-protobuf, which has none. A message
-- is written on one line with lowerCamelCase names, and read with either form of name.

local mime = require 'mime'
local pb = require 'pb'

local protojson = {}

local INT64_TYPES = {int64 = true, sint64 = true, sfixed64 = true}
local UINT64_TYPES = {uint64 = true, fixed64 = true}
local FLOAT_TYPES = {double = '%.17g', float = '%.9g'}

local function camel_case(name)
  return (name:gsub('_(%l)', string.upper))
end

-- The kind of a field's type: a scalar type name, 'message' or 'enum'
local function type_kind(typ)
  if typ:sub(1, 1) ~= '.' then
    return typ
  end
  local _, _, kind = pb.type(typ)
  return kind
end

-- An unsigned 64-bit integer in decimal, from the signed integer that holds its bits
local function unsigned(value)
  if value >= 0 then
    return string.format('%d', value)
  end
  local quotient = (value >> 1) // 5
  return string.format('%d%d', quotient, value - quotient * 10)
end

local ESCAPES = {['"'] = '\\"', ['\\'] = '\\\\', ['\n'] = '\\n', ['\r'] = '\\r', ['\t'] = '\\t'}

local function quote(text)
  return '"' .. text:gsub('[%c"\\]', function (c)
    return ESCAPES[c] or string.format('\\u%04x', c:byte())
  end) .. '"'
end

local write_message

local function write_value(kind, typ, value)
  if kind == 'message' then
    return write_message(typ, value)
  elseif kind == 'enum' then
    return quote(type(value) == 'string' and value or pb.enum(typ, value))
  elseif kind == 'string' then
    return quote(value)
  elseif kind == 'bytes' then
    return quote(mime.b64(value) or '')
  elseif kind == 'bool' then
    return tostring(value)
  elseif INT64_TYPES[kind] then
    return quote(string.format('%d', value))
  elseif UINT64_TYPES[kind] then
    return quote(unsigned(value))
  elseif FLOAT_TYPES[kind] then
    if value ~= value then
      return '"NaN"'
    elseif value == math.huge then
      return '"Infinity"'
    elseif value == -math.huge then
      return '"-Infinity"'
    end
    return string.format(FLOAT_TYPES[kind], value)
  end
  return string.format('%d', value)
end

-- A table decoded with no default values holds only the fields present on the wire
function write_message(typ, message)
  local parts = {}
  for name, _, ftype, _, label in pb.fields(typ) do
    local value = message[name]
    if value ~= nil then
      local kind = type_kind(ftype)
      local json
      if label == 'repeated' or label == 'packed' then
        local items = {}
        for i, item in ipairs(value) do
          items[i] = write_value(kind, ftype, item)
        end
        json = '[' .. table.concat(items, ',') .. ']'
      else
        json = write_value(kind, ftype, value)
      end
      parts[#parts + 1] = quote(camel_case(name)) .. ':' .. json
    end
  end
  return '{' .. table.concat(parts, ',') .. '}'
end

-- A message as one line of JSON
function protojson.encode(typ, message)
  local data = pb.encode(typ, message)
  pb.option('no_default_values')
  local present = pb.decode(typ, data)
  pb.option('auto_default_values')
  return write_message(typ, present)
end

-- Reading JSON

local function parse_error(text, position, what)
  error(string.format('Invalid JSON at %d: %s in %s', position, what, text), 0)
end

local function skip_space(text, position)
  return text:find('[^ \t\r\n]', position) or #text + 1
end

local parse_value

local function parse_string(text, position)
  local parts = {}
  position = position + 1
  while true do
    local c = text:sub(position, position)
    if c == '"' then
      return table.concat(parts), position + 1
    elseif c == '\\' then
      local e = text:sub(position + 1, position + 1)
      if e == 'u' then
        local code = tonumber(text:sub(position + 2, position + 5), 16)
        position = position + 6
        -- A surrogate pair, for a character outside the basic plane
        if code and code >= 0xd800 and code < 0xdc00 and text:sub(position, position + 1) == '\\u' then
          local low = tonumber(text:sub(position + 2, position + 5), 16)
          code = 0x10000 + ((code - 0xd800) << 10) + (low - 0xdc00)
          position = position + 6
        end
        if code == nil then
          parse_error(text, position, 'bad escape')
        end
        parts[#parts + 1] = utf8.char(code)
      else
        local escaped = ({b = '\b', f = '\f', n = '\n', r = '\r', t = '\t'})[e] or e
        parts[#parts + 1] = escaped
        position = position + 2
      end
    elseif c == '' then
      parse_error(text, position, 'unterminated string')
    else
      local finish = text:find('["\\]', position) or #text + 1
      parts[#parts + 1] = text:sub(position, finish - 1)
      position = finish
    end
  end
end

function parse_value(text, position)
  position = skip_space(text, position)
  local c = text:sub(position, position)
  if c == '{' then
    local object = {}
    position = skip_space(text, position + 1)
    if text:sub(position, position) == '}' then
      return object, position + 1
    end
    while true do
      local key
      position = skip_space(text, position)
      if text:sub(position, position) ~= '"' then
        parse_error(text, position, 'expected a name')
      end
      key, position = parse_string(text, position)
      position = skip_space(text, position)
      if text:sub(position, position) ~= ':' then
        parse_error(text, position, 'expected a colon')
      end
      object[key], position = parse_value(text, position + 1)
      position = skip_space(text, position)
      c = text:sub(position, position)
      if c == '}' then
        return object, position + 1
      elseif c ~= ',' then
        parse_error(text, position, 'expected a comma')
      end
      position = position + 1
    end
  elseif c == '[' then
    local array = {}
    position = skip_space(text, position + 1)
    if text:sub(position, position) == ']' then
      return array, position + 1
    end
    while true do
      array[#array + 1], position = parse_value(text, position)
      position = skip_space(text, position)
      c = text:sub(position, position)
      if c == ']' then
        return array, position + 1
      elseif c ~= ',' then
        parse_error(text, position, 'expected a comma')
      end
      position = position + 1
    end
  elseif c == '"' then
    return parse_string(text, position)
  elseif text:sub(position, position + 3) == 'true' then
    return true, position + 4
  elseif text:sub(position, position + 4) == 'false' then
    return false, position + 5
  elseif text:sub(position, position + 3) == 'null' then
    return nil, position + 4
  end
  local number = text:match('^-?%d+%.?%d*[eE]?[-+]?%d*', position)
  if number == nil or number == '' then
    parse_error(text, position, 'unexpected character')
  end
  -- A number with a fraction or an exponent stays a float, so -0.0 keeps its sign
  if number:match('^-?%d+$') then
    return math.tointeger(tonumber(number)) or tonumber(number), position + #number
  end
  return tonumber(number), position + #number
end

-- A 64-bit integer from a JSON string or number, wrapping an unsigned value above the signed
-- range into the integer that holds its bits
local function integer(value)
  if type(value) == 'number' then
    return math.tointeger(value)
  end
  local negative, digits = value:match('^(-?)(%d+)$')
  if digits == nil then
    error('Invalid integer ' .. value, 0)
  end
  local result = 0
  for d in digits:gmatch('%d') do
    result = result * 10 + tonumber(d)
  end
  return negative == '-' and -result or result
end

local read_message

local function read_value(kind, typ, value)
  if kind == 'message' then
    return read_message(typ, value)
  elseif kind == 'bytes' then
    return mime.unb64(value) or ''
  elseif INT64_TYPES[kind] or UINT64_TYPES[kind] then
    return integer(value)
  elseif FLOAT_TYPES[kind] then
    if value == 'NaN' then
      return 0 / 0
    elseif value == 'Infinity' then
      return math.huge
    elseif value == '-Infinity' then
      return -math.huge
    end
    return tonumber(value)
  elseif kind == 'string' or kind == 'bool' or kind == 'enum' then
    return value
  end
  return integer(value)
end

function read_message(typ, object)
  local fields = {}
  for name, _, ftype, _, label in pb.fields(typ) do
    local field = {name = name, type = ftype, repeated = label == 'repeated' or label == 'packed'}
    fields[name] = field
    fields[camel_case(name)] = field
  end
  local message = {}
  for key, value in pairs(object) do
    local field = fields[key]
    if field == nil then
      error('Unknown field ' .. key .. ' in ' .. typ, 0)
    end
    local kind = type_kind(field.type)
    if field.repeated then
      local items = {}
      for i, item in ipairs(value) do
        items[i] = read_value(kind, field.type, item)
      end
      message[field.name] = items
    else
      message[field.name] = read_value(kind, field.type, value)
    end
  end
  return message
end

-- A message from one line of JSON, decoded as pb.decode decodes it
function protojson.decode(typ, line)
  local object, position = parse_value(line, 1)
  if skip_space(line, position) <= #line then
    parse_error(line, position, 'trailing characters')
  end
  return pb.decode(typ, pb.encode(typ, read_message(typ, object)))
end

return protojson
