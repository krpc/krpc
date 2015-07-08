local class = require 'pl.class'
local seq = require 'pl.seq'
local stringx = require 'pl.stringx'
local bind1 = require 'pl.func'.bind1
local List = require 'pl.List'
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

local ServiceBase = class(Types.DynamicType)

function ServiceBase:_parse_procedure(procedure)
  local param_names = get_names(procedure.parameters)
  local param_types = get_types(self._client._types, procedure.parameters, procedure.attributes)
  local param_required = seq.copy(seq.map(function (x) return not x:HasField('default_argument') end, procedure.parameters))
  local param_default = List{}
  for param,typ in seq.zip(procedure.parameters, param_types) do
    if param:HasField('default_argument') then
      param_default:append(decoder.decode(param.default_argument, typ))
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

local function _construct_func(invoke, service_name, procedure_name, prefix_param_names, param_names,
                               param_types, param_required, param_default, return_type)
  prefix_param_names = prefix_param_names or List{}
  param_names = param_names or List{}
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

  --return function (...)
  --  local skip = prefix_param_names:len() -- ignore prefix parameters
  --  local args = List(table.pack(...)):slice(skip+1)
  --  print(args)
  --  return invoke(service_name, procedure_name, args, nil, param_names, param_types, return_type)
  --end
end

function ServiceBase:_add_service_class(cls)
  local class_type = self._client._types:as_type('Class(' .. self._name .. '.' .. cls.name .. ')')
  self[cls.name] = class_type.lua_type
end

function ServiceBase:_add_service_procedure(procedure)
  local param_names, param_types, param_required, param_default, return_type = self:_parse_procedure(procedure)
  local func = _construct_func(self._invoke, self._name, procedure.name, List{}, param_names, param_types, param_required, param_default, return_type)
  --build_request = ...
  name = service.to_snake_case(procedure.name)
  return self:_add_static_method(name, func)
end

function ServiceBase:_add_service_property(name, getter, setter)
  if getter then
    getter_name = getter.name
    _,_,_,_,return_type = self:_parse_procedure(getter)
    getter = _construct_func(self._invoke, self._name, getter_name, List{'self'}, nil, nil, nil, nil, return_type)
  end
  if setter then
    setter_name = setter.name
    param_names, param_types, _,_,_ = self:_parse_procedure(setter)
    setter = _construct_func(self._invoke, self._name, setter_name, List{'self'}, param_names, param_types, List{true}, List{nil}, nil)
  end
  name = service.to_snake_case(name)
  return self:_add_property(name, getter, setter)
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

  return cls()
end

return service
