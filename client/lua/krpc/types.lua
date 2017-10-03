local class = require 'pl.class'
local List = require 'pl.List'
local Set = require 'pl.Set'
local Map = require 'pl.Map'
local seq = require 'pl.seq'
local stringx = require 'pl.stringx'
local schema = require 'krpc.schema.KRPC'
local Attributes = require 'krpc.attributes'

local Types = class()

-- Note: make copies of the type code values here,
--       as accessing the generated protobuf code is rather verbose
Types.NONE = schema.TYPE_TYPECODE_NONE_ENUM.number
Types.DOUBLE = schema.TYPE_TYPECODE_DOUBLE_ENUM.number
Types.FLOAT = schema.TYPE_TYPECODE_FLOAT_ENUM.number
Types.SINT32 = schema.TYPE_TYPECODE_SINT32_ENUM.number
Types.SINT64 = schema.TYPE_TYPECODE_SINT64_ENUM.number
Types.UINT32 = schema.TYPE_TYPECODE_UINT32_ENUM.number
Types.UINT64 = schema.TYPE_TYPECODE_UINT64_ENUM.number
Types.BOOL = schema.TYPE_TYPECODE_BOOL_ENUM.number
Types.STRING = schema.TYPE_TYPECODE_STRING_ENUM.number
Types.BYTES = schema.TYPE_TYPECODE_BYTES_ENUM.number
Types.CLASS = schema.TYPE_TYPECODE_CLASS_ENUM.number
Types.ENUMERATION = schema.TYPE_TYPECODE_ENUMERATION_ENUM.number
Types.TUPLE = schema.TYPE_TYPECODE_TUPLE_ENUM.number
Types.LIST = schema.TYPE_TYPECODE_LIST_ENUM.number
Types.SET = schema.TYPE_TYPECODE_SET_ENUM.number
Types.DICTIONARY = schema.TYPE_TYPECODE_DICTIONARY_ENUM.number
Types.EVENT = schema.TYPE_TYPECODE_EVENT_ENUM.number
Types.PROCEDURE_CALL = schema.TYPE_TYPECODE_PROCEDURE_CALL_ENUM.number
Types.STREAM = schema.TYPE_TYPECODE_STREAM_ENUM.number
Types.SERVICES = schema.TYPE_TYPECODE_SERVICES_ENUM.number
Types.STATUS = schema.TYPE_TYPECODE_STATUS_ENUM.number

VALUE_TYPES = Map{}
VALUE_TYPES:set(Types.DOUBLE, 'number')
VALUE_TYPES:set(Types.FLOAT, 'number')
VALUE_TYPES:set(Types.SINT32, 'number')
VALUE_TYPES:set(Types.SINT64, 'number')
VALUE_TYPES:set(Types.UINT32, 'number')
VALUE_TYPES:set(Types.UINT64, 'number')
VALUE_TYPES:set(Types.BOOL, 'boolean')
VALUE_TYPES:set(Types.STRING, 'string')
VALUE_TYPES:set(Types.BYTES, 'string')

MESSAGE_TYPES = Map{}
MESSAGE_TYPES:set(Types.EVENT, schema.Event)
MESSAGE_TYPES:set(Types.PROCEDURE_CALL, schema.ProcedureCall)
MESSAGE_TYPES:set(Types.STREAM, schema.Stream)
MESSAGE_TYPES:set(Types.SERVICES, schema.Services)
MESSAGE_TYPES:set(Types.STATUS, schema.Status)

CODE_TO_STRING = Map{}
CODE_TO_STRING:set(Types.DOUBLE, 'double')
CODE_TO_STRING:set(Types.FLOAT, 'float')
CODE_TO_STRING:set(Types.SINT32, 'sint32')
CODE_TO_STRING:set(Types.SINT64, 'sint64')
CODE_TO_STRING:set(Types.UINT32, 'uint32')
CODE_TO_STRING:set(Types.UINT64, 'uint64')
CODE_TO_STRING:set(Types.BOOL, 'bool')
CODE_TO_STRING:set(Types.STRING, 'string')
CODE_TO_STRING:set(Types.BYTES, 'bytes')
CODE_TO_STRING:set(Types.EVENT, 'Event')
CODE_TO_STRING:set(Types.PROCEDURE_CALL, 'ProcedureCall')
CODE_TO_STRING:set(Types.STREAM, 'Stream')
CODE_TO_STRING:set(Types.SERVICES, 'Services')
CODE_TO_STRING:set(Types.STATUS, 'Status')

function Types:_init()
  self._types = Map{}
end

function _protobuf_type(code, service, name, types)
  local protobuf_type = schema.Type()
  protobuf_type.code = code
  if service ~= nil then
    protobuf_type.service = service
  end
  if name ~= nil then
    protobuf_type.name = name
  end
  if types ~= nil then
    for _, typ in ipairs(types) do
      local newtyp = protobuf_type.types:add()
      _set_protobuf_type(typ, newtyp)
    end
  end
  return protobuf_type
end

function _set_protobuf_type(src, dst)
  dst.code = src.code
  dst.service = src.service
  dst.name = src.name
  for _, typ in ipairs(src.types) do
    local newtyp = dst.types:add()
    _set_protobuf_type(typ, newtyp)
  end
end

function Types:as_type(protobuf_type)
  -- Return a type object given a protocol buffer type

  -- Get cached type
  local key = protobuf_type:SerializeToString()
  if self._types:get(key) then
    return self._types:get(key)
  end

  local typ
  if VALUE_TYPES:get(protobuf_type.code) then
    typ = Types.ValueType(protobuf_type)
  elseif protobuf_type.code == Types.CLASS then
    typ = Types.ClassType(protobuf_type)
  elseif protobuf_type.code == Types.ENUMERATION then
    typ = Types.EnumerationType(protobuf_type)
  elseif protobuf_type.code == Types.TUPLE then
    typ = Types.TupleType(protobuf_type, self)
  elseif protobuf_type.code == Types.LIST then
    typ = Types.ListType(protobuf_type, self)
  elseif protobuf_type.code == Types.SET then
    typ = Types.SetType(protobuf_type, self)
  elseif protobuf_type.code == Types.DICTIONARY then
    typ = Types.DictionaryType(protobuf_type, self)
  elseif MESSAGE_TYPES:get(protobuf_type.code) then
    typ = Types.MessageType(protobuf_type)
  else
    error('Invalid type')
  end

  self._types:set(key, typ)
  return typ
end

function Types:double_type()
  -- Get a double value type
  return self:as_type(_protobuf_type(Types.DOUBLE))
end

function Types:float_type()
  -- Get a float value type
  return self:as_type(_protobuf_type(Types.FLOAT))
end

function Types:sint32_type()
  -- Get an sint32 value type
  return self:as_type(_protobuf_type(Types.SINT32))
end

function Types:sint64_type()
  -- Get an sint64 value type
  return self:as_type(_protobuf_type(Types.SINT64))
end

function Types:uint32_type()
  -- Get a uint32 value type
  return self:as_type(_protobuf_type(Types.UINT32))
end

function Types:uint64_type()
  -- Get a uint64 value type
  return self:as_type(_protobuf_type(Types.UINT64))
end

function Types:bool_type()
  -- Get a bool value type
  return self:as_type(_protobuf_type(Types.BOOL))
end

function Types:string_type()
  -- Get a string value type
  return self:as_type(_protobuf_type(Types.STRING))
end

function Types:bytes_type()
  -- Get a bytes value type
  return self:as_type(_protobuf_type(Types.BYTES))
end

function Types:class_type(service, name)
  -- Get a class type
  return self:as_type(_protobuf_type(Types.CLASS, service, name))
end

function Types:enumeration_type(service, name)
  -- Get an enumeration type
  return self:as_type(_protobuf_type(Types.ENUMERATION, service, name))
end

function Types:tuple_type(value_types)
  -- Get a tuple type
  local types = seq.copy(seq.map(function (x) return x.protobuf_type end, value_types))
  return self:as_type(_protobuf_type(Types.TUPLE, nil, nil, types))
end

function Types:list_type(value_type)
  -- Get a list type
  return self:as_type(_protobuf_type(Types.LIST, nil, nil,
                                     { value_type.protobuf_type }))
end

function Types:set_type(value_type)
  -- Get a set type
  return self:as_type(_protobuf_type(Types.SET, nil, nil,
                                     { value_type.protobuf_type }))
end

function Types:dictionary_type(key_type, value_type)
  -- Get a dictionary type
  return self:as_type(_protobuf_type(Types.DICTIONARY, nil, nil,
                                     { key_type.protobuf_type, value_type.protobuf_type }))
end

function Types:procedure_call_type()
  -- Get a StreamMessage message type
  return self:as_type(_protobuf_type(Types.PROCEDURE_CALL))
end

function Types:stream_type()
  -- Get a Status message type
  return self:as_type(_protobuf_type(Types.STREAM))
end

function Types:services_type()
  -- Get a Services message type
  return self:as_type(_protobuf_type(Types.SERVICES))
end

function Types:status_type()
  -- Get a Status message type
  return self:as_type(_protobuf_type(Types.STATUS))
end

function Types:coerce_to(value, typ)
  -- Coerce a value to the specified type (specified by a type object).
  --        Raises an error if the coercion is not possible.
  if type(value) == typ.lua_type then
    return value
  end
  -- Types.none can be coerced to a ClassType
  if typ:is_a(Types.ClassType) and value == Types.none then
    return Types.none
  end
  -- Coerce identical class types from different client connections
  if typ:is_a(Types.ClassType) and Types.ClassBase:class_of(value) then
    if typ.lua_type._service_name == value._service_name and
       typ.lua_type._class_name == value._class_name then
      return typ.lua_type(value._object_id)
    end
  end
  -- Collection types
  -- Coerce tuples to lists
  if type(value) == 'table' and value._object_id ~= 0 and typ:is_a(Types.ListType) then
    local result = typ.lua_type()
    for _, x in ipairs(value) do
      result:append(self:coerce_to(x, typ.value_type))
    end
    return result
  end
  -- Coerce lists (with appropriate number of elements) to tuples
  if type(value) == 'table' and value._object_id ~= 0 and typ:is_a(Types.TupleType) and #(value) == #(typ.value_types) then
    local result = typ.lua_type()
    for i, x in ipairs(value) do
      result:append(self:coerce_to(x, typ.value_types[i]))
    end
    return result
  end
  error('Failed to coerce value ' .. tostring(value) .. ' of type ' .. type(value) .. ' to type ' .. tostring(typ))
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
  for k, v in pairs(values) do
    cls[k] = cls(v)
  end
  return cls
end

Types.TypeBase = class()

function Types.TypeBase:_init(protobuf_type, lua_type, type_string)
  self.protobuf_type = protobuf_type
  self.lua_type = lua_type
  self._string = type_string
end

function Types.TypeBase:__tostring()
  return '<type: ' .. self._string .. '>'
end

Types.ValueType = class(Types.TypeBase)

function Types.ValueType:_init(protobuf_type)
  if not VALUE_TYPES:get(protobuf_type.code) then
    error('Not a value type')
  end
  self:super(protobuf_type, VALUE_TYPES:get(protobuf_type.code), CODE_TO_STRING:get(protobuf_type.code))
end

Types.ClassType = class(Types.TypeBase)

function Types.ClassType:_init(protobuf_type)
  if protobuf_type.code ~= Types.CLASS then
    error('Not a class type')
  end
  if protobuf_type.service == '' then
    error('Class type has no service name')
  end
  if protobuf_type.name == '' then
    error('Class type has no class name')
  end
  self._service_name = protobuf_type.service
  self._class_name = protobuf_type.name
  local typ = _create_class_type(self._service_name, self._class_name)
  local type_string = 'Class(' .. protobuf_type.service .. '.' .. protobuf_type.name .. ')'
  self:super(protobuf_type, typ, type_string)
end

Types.EnumerationType = class(Types.TypeBase)

function Types.EnumerationType:_init(protobuf_type)
  if protobuf_type.code ~= Types.ENUMERATION then
    error('Not an enumeration type')
  end
  if protobuf_type.service == '' then
    error('Enumeration type has no service name')
  end
  if protobuf_type.name == '' then
    error('Enumeration type has no class name')
  end
  self._service_name = protobuf_type.service
  self._class_name = protobuf_type.name
  local type_string = 'Enum(' .. protobuf_type.service .. '.' .. protobuf_type.name .. ')'
  self:super(protobuf_type, nil, type_string)
end

function Types.EnumerationType:set_values(values)
  self.lua_type = _create_enum_type(self._service_name, self._enum_name, values)
end

Types.TupleType = class(Types.TypeBase)

function Types.TupleType:_init(protobuf_type, types)
  if protobuf_type.code ~= Types.TUPLE then
    error('Not a tuple type')
  end
  if #(protobuf_type.types) == 0 then
    error('Wrong number of sub-types for tuple type')
  end
  self.value_types = List{}
  for _, subtype in ipairs(protobuf_type.types) do
    self.value_types:append(types:as_type(subtype))
  end
  local protobuf_substrings = seq.copy(seq.map(function (x) return x._string end, self.value_types))
  local type_string = 'Tuple(' .. stringx.join(',', protobuf_substrings) .. ')'
  self:super(protobuf_type, List, type_string)
end

Types.ListType = class(Types.TypeBase)

function Types.ListType:_init(protobuf_type, types)
  if protobuf_type.code ~= Types.LIST then
    error('Not a list type')
  end
  if #(protobuf_type.types) ~= 1 then
    error('Wrong number of sub-types for list type')
  end
  self.value_type = types:as_type(protobuf_type.types[1])
  local type_string = 'List(' .. self.value_type._string .. ')'
  self:super(protobuf_type, List, type_string)
end

Types.SetType = class(Types.TypeBase)

function Types.SetType:_init(protobuf_type, types)
  if protobuf_type.code ~= Types.SET then
    error('Not a set type')
  end
  if #(protobuf_type.types) ~= 1 then
    error('Wrong number of sub-types for set type')
  end
  self.value_type = types:as_type(protobuf_type.types[1])
  local type_string = 'Set(' .. self.value_type._string .. ')'
  self:super(protobuf_type, Set, type_string)
end

Types.DictionaryType = class(Types.TypeBase)

function Types.DictionaryType:_init(protobuf_type, types)
  if protobuf_type.code ~= Types.DICTIONARY then
    error('Not a dictionary type')
  end
  if #(protobuf_type.types) ~= 2 then
    error('Wrong number of sub-types for dictionary type')
  end
  self.key_type = types:as_type(protobuf_type.types[1])
  self.value_type = types:as_type(protobuf_type.types[2])
  type_string = 'Dict(' .. self.key_type._string .. ',' .. self.value_type._string .. ')'
  self:super(protobuf_type, Map, type_string)
end

Types.MessageType = class(Types.TypeBase)

function Types.MessageType:_init(protobuf_type)
  if not MESSAGE_TYPES:get(protobuf_type.code) then
    error('Not a message type')
  end
  self:super(protobuf_type, MESSAGE_TYPES:get(protobuf_type.code), CODE_TO_STRING:get(protobuf_type.code))
end

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

local None = class()

function None:_init()
  self._object_id = 0
end

function None:__tostring()
  return 'none'
end

Types.none = None()

return Types
