local class = require 'pl.class'
local seq = require 'pl.seq'
local stringx = require 'pl.stringx'
local bind1 = require 'pl.func'.bind1
local List = require 'pl.List'
local Set = require 'pl.Set'
local Map = require 'pl.Map'
local Types = require 'krpc.types'
local Attributes = require 'krpc.attributes'
local decoder = require 'krpc.decoder'

local service = {}

local regex_multi_uppercase = '([A-Z]+)([A-Z][a-z0-9])'
local regex_single_uppercase = '([a-z0-9])([A-Z])'
local regex_underscores = '(.)_'

--- Convert camel case to snake case, e.g. GetServices -> get_services
function service.to_snake_case(camel_case)
  local result = camel_case:gsub(regex_underscores, '%1__')
  result = result:gsub(regex_single_uppercase, '%1_%2')
  return result:gsub(regex_multi_uppercase, '%1_%2'):lower()
end

local function get_names(xs)
  return seq.copy(seq.map(function (x) return service.to_snake_case(x.name) end, xs))
end

local function get_types(types, xs, attrs)
  local result = List{}
  for i,x in ipairs(xs) do
    result:append(types:get_parameter_type(i, x.type, List(attrs)))
  end
  return result
end

local KEYWORDS = Set{
  'and', 'break', 'do', 'else', 'elseif', 'end', 'false', 'for', 'function', 'if', 'in',
  'local', 'nil', 'not', 'or', 'repeat', 'return', 'then', 'true', 'until', 'while'
}

--- Given a list of parameter names, append underscores to reserved keywords
-- without causing parameter names to clash
local function update_param_names(names)
  local newnames = List{}
  for name in names:iter() do
    if KEYWORDS[name] then
      name = name .. '_'
    end
    while Set(names)[name] do
      name = name .. '_'
    end
    newnames:append(name)
  end
  return newnames
end

local function _construct_func(invoke, service_name, procedure_name, prefix_param_names, param_names,
                               param_types, param_required, param_default, return_type)
  prefix_param_names = prefix_param_names or List{}
  param_names = param_names or List{}
  prefix_param_names = update_param_names(prefix_param_names)
  param_names = update_param_names(param_names)
  local body =
    'return invoke(' ..
    stringx.join(
      ',',
      {'service_name',
       'procedure_name',
       'nil',
       'Map{'..stringx.join(',', seq.copy(seq.map(function (x) return x..'='..x end, param_names)))..'}',
       'param_names',
       'param_types',
       'return_type'
      }) ..
    ')'
  local func = 'return function (' .. stringx.join(',', prefix_param_names..param_names) .. ') ' .. body .. ' end'
  local wrapper =
    'return function (invoke,service_name,procedure_name,param_names,param_types,return_type,Map) ' .. func .. ' end'
  local callable = assert(loadstring(wrapper, '_construct_func('..service_name..','..procedure_name..')'))()
  return callable(invoke,service_name,procedure_name,param_names,param_types,return_type,Map)
end

local ServiceBase = class(Types.DynamicType)

function ServiceBase:_parse_procedure(procedure)
  local param_names = get_names(procedure.parameters)
  local param_types = get_types(self._client._types, procedure.parameters, procedure.attributes)
  local param_required = seq.copy(seq.map(function (x) return not x:HasField('default_value') end, procedure.parameters))
  local param_default = List{}
  for param,typ in seq.zip(procedure.parameters, param_types) do
    if param:HasField('default_value') then
      param_default:append(decoder.decode(param.default_value, typ))
    else
      param_default:append(nil)
    end
  end
  local return_type = nil
  if procedure:HasField('return_type') then
    return_type = self._client._types:get_return_type(procedure.return_type, List(procedure.attributes))
  end
  return param_names, param_types, param_required, param_default, return_type
end

--- Add a class type
function ServiceBase:_add_service_class(cls)
  local class_type = self._client._types:as_type('Class(' .. self._name .. '.' .. cls.name .. ')')
  self[cls.name] = class_type.lua_type
end

--- Add an enumeration type
function ServiceBase:_add_service_enumeration(enum)
  local name = enum.name
  local enum_type = self._client._types:as_type('Enum(' .. self._name .. '.' .. name .. ')')
  local values = {}
  for _,x in ipairs(enum.values) do
    values[service.to_snake_case(x.name)] = x.value
  end
  enum_type:set_values(values)
  self[name] = enum_type.lua_type
end

--- Add a procedure
function ServiceBase:_add_service_procedure(procedure)
  local param_names, param_types, param_required, param_default, return_type = self:_parse_procedure(procedure)
  local func = _construct_func(self._invoke, self._name, procedure.name, List{}, param_names, param_types, param_required, param_default, return_type)
  --build_request = ...
  local name = service.to_snake_case(procedure.name)
  return self:_add_static_method(name, func)
end

function ServiceBase:_add_service_property(name, getter, setter)
  if getter then
    local getter_name = getter.name
    local _,_,_,_,return_type = self:_parse_procedure(getter)
    getter = _construct_func(self._invoke, self._name, getter_name, List{'self'}, nil, nil, nil, nil, return_type)
  end
  if setter then
    local setter_name = setter.name
    local param_names, param_types, _,_,_ = self:_parse_procedure(setter)
    setter = _construct_func(self._invoke, self._name, setter_name, List{'self'}, param_names, param_types, List{true}, List{nil}, nil)
  end
  local name = service.to_snake_case(name)
  return self:_add_property(name, getter, setter)
end

--- Add a method to a class
function ServiceBase:_add_service_class_method(class_name, method_name, procedure)
  local class_cls = self._client._types:as_type('Class('..self._name..'.'..class_name..')').lua_type
  local param_names, param_types, param_required, param_default, return_type = self:_parse_procedure(procedure)
  -- Rename this to self if it doesn't cause a name clash
  --if 'self' not in param_names:
  --    param_names[0] = 'self'
  local func = _construct_func(self._invoke, self._name, procedure.name, nil, param_names, param_types, param_required, param_default, return_type)
  --build_request = ...
  local name = service.to_snake_case(method_name)
  class_cls:_add_method(name, func)
end

--- Add a static method to a class
function ServiceBase:_add_service_class_static_method(class_name, method_name, procedure)
  local class_cls = self._client._types:as_type('Class('..self._name..'.'..class_name..')').lua_type
  local param_names, param_types, param_required, param_default, return_type = self:_parse_procedure(procedure)
  local func = _construct_func(self._invoke, self._name, procedure.name, nil, param_names, param_types, param_required, param_default, return_type)
  --local build_request = ...
  local name = service.to_snake_case(method_name)
  class_cls:_add_static_method(name, func)
end

--- Add a property to a class
function ServiceBase:_add_service_class_property(class_name, property_name, getter, setter)
  local class_cls = self._client._types:as_type('Class('..self._name..'.'..class_name..')').lua_type
  if getter then
    local getter_name = getter.name
    local param_names, param_types, param_required, param_default, return_type = self:_parse_procedure(getter)
    -- Rename this to self if it doesn't cause a name clash
    --if 'self' not in param_names:
    --  param_names[0] = 'self'
    getter = _construct_func(self._invoke, self._name, getter_name, nil, param_names, param_types, List{true}, List{Types.none}, return_type)
    --local build_request = ...
  end
  if setter then
    local param_names, param_types, param_required, param_default, return_type = self:_parse_procedure(setter)
    setter = _construct_func(self._invoke, self._name, setter.name, nil, param_names, param_types, List{true,true}, List{Types.none, Types.none}, nil)
  end
  local property_name = service.to_snake_case(property_name)
  return class_cls:_add_property(property_name, getter, setter)
end

--- Create a new service type
function service.create_service(client, service)
  local cls = class(ServiceBase)
  cls._client = client
  cls._name = service.name
  cls._invoke = bind1(client._invoke, client)

  -- Add class types to service
  for _,cls2 in ipairs(service.classes) do
    cls:_add_service_class(cls2)
  end

  -- Add enumeration types to service
  for _,enum in ipairs(service.enumerations) do
    cls:_add_service_enumeration(enum)
  end

  -- Add procedures
  for _,procedure in ipairs(service.procedures) do
    if Attributes.is_a_procedure(procedure.attributes) then
      cls:_add_service_procedure(procedure)
    end
  end

  -- Add properties
  local properties = {}
  for _,procedure in ipairs(service.procedures) do
    if Attributes.is_a_property_accessor(procedure.attributes) then
      local name = Attributes.get_property_name(List(procedure.attributes))
      if not properties[name] then
        properties[name] = {}
      end
      if Attributes.is_a_property_getter(procedure.attributes) then
        properties[name]['get'] = procedure
      else
        properties[name]['set'] = procedure
      end
    end
  end
  for name, procedures in pairs(properties) do
    cls:_add_service_property(name, procedures['get'], procedures['set'])
  end

  -- Add class methods
  for _,procedure in ipairs(service.procedures) do
    if Attributes.is_a_class_method(List(procedure.attributes)) then
      local class_name = Attributes.get_class_name(List(procedure.attributes))
      local method_name = Attributes.get_class_method_name(List(procedure.attributes))
      cls:_add_service_class_method(class_name, method_name, procedure)
    end
  end

  -- Add static class methods
  for _,procedure in ipairs(service.procedures) do
    if Attributes.is_a_class_static_method(List(procedure.attributes)) then
      local class_name = Attributes.get_class_name(List(procedure.attributes))
      local method_name = Attributes.get_class_method_name(List(procedure.attributes))
      cls:_add_service_class_static_method(class_name, method_name, procedure)
    end
  end

  -- Add class properties
  local properties = {}
  for _,procedure in ipairs(service.procedures) do
    if Attributes.is_a_class_property_accessor(List(procedure.attributes)) then
      local class_name = Attributes.get_class_name(List(procedure.attributes))
      local property_name = Attributes.get_class_property_name(List(procedure.attributes))
      local key = class_name..'.'..property_name
      if not properties[key] then
        properties[key] = {}
      end
      if Attributes.is_a_class_property_getter(List(procedure.attributes)) then
        properties[key]['get'] = procedure
      else
        properties[key]['set'] = procedure
      end
    end
  end
  for key, procedures in pairs(properties) do
    local class_name, _, property_name = stringx.partition(key, '.')
    cls:_add_service_class_property(class_name, property_name, procedures['get'], procedures['set'])
  end

  return cls()
end

return service
