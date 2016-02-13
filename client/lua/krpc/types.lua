local class = require 'pl.class'
local List = require 'pl.List'
local Set = require 'pl.Set'
local Map = require 'pl.Map'
local stringx = require 'pl.stringx'
local Attributes = require 'krpc.attributes'

local Types = class()

local PROTOBUF_VALUE_TYPES = Set{
  'double', 'float', 'int32', 'int64', 'uint32', 'uint64', 'bool', 'string', 'bytes'}
local LUA_VALUE_TYPES = Set{'number', 'boolean', 'string'}
local PROTOBUF_TO_LUA_VALUE_TYPE = Map{
  double = 'number',
  float = 'number',
  int32 = 'number',
  int64 = 'number',
  uint32 = 'number',
  uint64 = 'number',
  bool = 'boolean',
  string = 'string',
  bytes = 'string'
}
local PROTOBUF_TO_MESSAGE_TYPE = Map{}

local _packages_loaded = Set{}
local _package_search_path = 'krpc.schema'

local function _parse_type_string(typ)
  -- Given a string, extract a substring up to the first comma. Parses parnetheses.
  -- Multiple calls can be used to separate a string by commas.
  if not typ then
    error('Invalid type string')
  end
  local result = ''
  local level = 0
  for x in List.iterate(typ) do
    if level == 0 and x == ',' then
      break
    end
    if x == '(' then
      level = level + 1
    end
    if x == ')' then
      level = level - 1
    end
    result = result .. x
  end
  if level ~= 0 then
    error('Invalid type string')
  end
  if result == typ then
    return result, None
  end
  if typ:sub(result:len()+1,result:len()+1) ~= ',' then
    error('Invalid type string')
  end
  return result, typ:sub(result:len()+2)
end

local function _load_types(package)
  if _packages_loaded[package] then
    return
  end
  _packages_loaded[package] = true
  local ok, module = pcall(require, _package_search_path .. '.' .. package)
  if ok then
    for k,name in ipairs(module.MESSAGE_TYPES) do
      PROTOBUF_TO_MESSAGE_TYPE[package .. '.' .. name] = module[name]
    end
  end
end

local None = class()

function None:_init()
  self._object_id = 0
end

function None:__tostring()
  return 'none'
end

Types.none = None()

Types.DynamicType = class(class.properties)

--- Add a method
function Types.DynamicType:_add_method(name, func)
  self[name] = func
  return self[name]
end

--- Add a static method
function Types.DynamicType:_add_static_method(name, func)
  self[name] = func
  return self[name]
end

--- Add a property
function Types.DynamicType:_add_property(name, getter, setter)
  if (not getter) and (not setter) then
    error('Either getter or setter must be provided')
  end
  if getter then
    self['get_' .. name] = getter
  end
  if setter then
    self['set_' .. name] = setter
  end
end

Types.ClassBase = class(Types.DynamicType)

function Types.ClassBase:_init(object_id)
  self._object_id = object_id
end

function Types.ClassBase:__eq(other)
  return self._object_id == other._object_id
end

function Types.ClassBase:__tostring()
  return string.format('<%s.%s remote object #%d>', self._service_name, self._class_name, self._object_id)
end

local function _create_class_type(service_name, class_name)
  local cls = class(Types.ClassBase)
  cls['_service_name'] = service_name
  cls['_class_name'] = class_name
  return cls
end

Types.Enum = class()

function Types.Enum:_init(value)
  self.value = value
end

local function _create_enum_type(service_name, class_name, values)
  local cls = class(Types.Enum)
  for k,v in pairs(values) do
    cls[k] = cls(v)
  end
  return cls
end

Types.TypeBase = class()

function Types.TypeBase:_init(protobuf_type, lua_type)
  self.protobuf_type = protobuf_type
  self.lua_type = lua_type
end

function Types.TypeBase:__tostring()
  return '<pbtype: \'' .. self.protobuf_type .. '\'>'
end

Types.ValueType = class(Types.TypeBase)

function Types.ValueType:_init(type_string)
  if not PROTOBUF_TO_LUA_VALUE_TYPE[type_string] then
    error('\'' .. type_string .. '\' is not a valid type string for a value type')
  end
  self:super(type_string, PROTOBUF_TO_LUA_VALUE_TYPE[type_string])
end

Types.MessageType = class(Types.TypeBase)

function Types.MessageType:_init(type_string)
  package,_,_ = stringx.rpartition(type_string, '.')
  _load_types(package)
  if not PROTOBUF_TO_MESSAGE_TYPE[type_string] then
    error('\'' .. type_string .. '\' is not a valid type string for a message type')
  end
  typ = PROTOBUF_TO_MESSAGE_TYPE[type_string]
  self:super(type_string, typ)
end

Types.ClassType = class(Types.TypeBase)

function Types.ClassType:_init(type_string)
  service_name, class_name = type_string:match('^Class%(([^%.]+)%.([^%.]+)%)$')
  if (not service_name) or (not class_name) then
    error('\'' .. type_string .. '\' is not a valid type string for a class type')
  end
  self._service_name = service_name
  self._class_name = class_name
  typ = _create_class_type(service_name, class_name)
  self:super(type_string, typ)
end

Types.EnumType = class(Types.TypeBase)

function Types.EnumType:_init(type_string)
  service_name, class_name = type_string:match('^Enum%(([^%.]+)%.([^%.]+)%)$')
  if (not service_name) or (not class_name) then
    error('\'' .. type_string .. '\' is not a valid type string for an enumeration type')
  end
  self._service_name = service_name
  self._class_name = class_name
  self:super(type_string, nil)
end

function Types.EnumType:set_values(values)
  self.lua_type = _create_enum_type(self._service_name, self._enum_name, values)
end

Types.ListType = class(Types.TypeBase)

function Types.ListType:_init(type_string, types)
  local typ = type_string:match('^List%((.+)%)$')
  if not typ then
    error('\'' .. type_string .. '\' is not a valid type string for a list type')
  end
  self.value_type = types:as_type(typ)
  self:super(type_string, List)
end

Types.DictionaryType = class(Types.TypeBase)

function Types.DictionaryType:_init(type_string, types)
  local typ = type_string:match('^Dictionary%((.+)%)$')
  if not typ then
    error('\'' .. type_string .. '\' is not a valid type string for a dictionary type')
  end

  local key_string, typ = _parse_type_string(typ)
  local value_string, typ = _parse_type_string(typ)
  if typ then
    error('\'' .. type_string .. '\' is not a valid type string for a dictionary type')
  end
  self.key_type = types:as_type(key_string)
  self.value_type = types:as_type(value_string)

  self:super(type_string, Map)
end

Types.SetType = class(Types.TypeBase)

function Types.SetType:_init(type_string, types)
  typ = type_string:match('^Set%((.+)%)$')
  if not typ then
    error('\'' .. type_string .. '\' is not a valid type string for a set type')
  end
  self.value_type = types:as_type(typ)
  self:super(type_string, Set)
end

Types.TupleType = class(Types.TypeBase)

function Types.TupleType:_init(type_string, types)
  typ = type_string:match('^Tuple%((.+)%)$')
  if not typ then
    error('\'' .. type_string .. '\' is not a valid type string for a tuple type')
  end

  self.value_types = List{}
  while typ do
    value_type, typ = _parse_type_string(typ)
    self.value_types:append(types:as_type(value_type))
  end
  self:super(type_string, List)
end

function Types:_init()
  self._types = Map{}
end

function Types:as_type(type_string)
  if self._types[type_string] then
    return self._types[type_string]
  end

  local typ

  if PROTOBUF_VALUE_TYPES[type_string] then
    typ = Types.ValueType(type_string)
  elseif stringx.startswith(type_string, 'Class(') or type_string == 'Class' then
    typ = Types.ClassType(type_string)
  elseif stringx.startswith(type_string, 'Enum(') or type_string == 'Enum' then
    typ = Types.EnumType(type_string)
  elseif stringx.startswith(type_string, 'List(') or type_string == 'List' then
    typ = Types.ListType(type_string, self)
  elseif stringx.startswith(type_string, 'Dictionary(') or type_string == 'Dictionary' then
    typ = Types.DictionaryType(type_string, self)
  elseif stringx.startswith(type_string, 'Set(') or type_string == 'Set' then
    typ = Types.SetType(type_string, self)
  elseif stringx.startswith(type_string, 'Tuple(') or type_string == 'Tuple' then
    typ = Types.TupleType(type_string, self)
  else
    -- A message type
    if not type_string:match('[A-Za-z0-9_\\.]+') then
      error('\'' .. type_string .. '\' is not a valid type string')
    end
    package,_,_ = stringx.rpartition(type_string, '.')
    _load_types(package)
    if PROTOBUF_TO_MESSAGE_TYPE[type_string] then
      typ = Types.MessageType(type_string)
    else
      error('\'' .. type_string .. '\' is not a valid type string')
    end
  end

  self._types[type_string] = typ
  return typ
end

--- Coerce a value to the specified type (specified by a type object)
--  Raises an error if the coercion is not possible """
function Types:coerce_to(value, typ)
  if type(typ.lua_type) == 'string' and typ(value) == typ.lua_type then
    return value
  elseif type(typ.lua_type) == 'table' and typ.lua_type:class_of(value) then
    return value
  end
  -- Types.none can be coerced to a ClassType
  if typ:is_a(Types.ClassType) and value == Types.none then
    return Types.none
  end
  -- Coerce identical class types from different client connections
  if typ:is_a(Types.ClassType) and Types.ClassBase:class_of(value) then
    --local value_type = type(value)
    if typ.lua_type._service_name == value._service_name and
       typ.lua_type._class_name == value._class_name then
      return typ.lua_type(value._object_id)
    end
  end
  error('Failed to coerce value ' .. tostring(value) .. ' of type ' .. type(value) .. ' to type ' .. tostring(typ))
end

function Types:get_parameter_type(pos, typ, attrs)
  local attrs = Attributes.get_parameter_type_attrs(pos, attrs)
  for attr in attrs:iter() do
    local ok,result = pcall(function () return self:as_type(attr) end)
    if ok then
      return result
    end
  end
  return self:as_type(typ)
end

function Types:get_return_type(typ, attrs)
  local attrs = Attributes.get_return_type_attrs(attrs)
  for attr in attrs:iter() do
    local ok,result = pcall(function () return self:as_type(attr) end)
    if ok then
      return result
    end
  end
  return self:as_type(typ)
end

return Types
