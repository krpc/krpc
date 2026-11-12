local class = require 'pl.class'
local seq = require 'pl.seq'
local stringx = require 'pl.stringx'
local bind1 = require 'pl.func'.bind1
local List = require 'pl.List'
local Set = require 'pl.Set'
local compat = require 'pl.compat'
local Types = require 'krpc.types'
local Attributes = require 'krpc.attributes'

local service = {}

-- The service and procedure each function a service or class is built with calls, the number of
-- leading parameters it does not send, and the warning for a deprecated one, by function.
-- Connection:get_call reads it. The keys are weak
-- and the values hold no types, as Lua 5.1 and LuaJIT have no ephemeron tables.
service.procedures = setmetatable({}, { __mode = 'k' })

local regex_multi_uppercase = '([A-Z]+)([A-Z][a-z0-9])'
local regex_single_uppercase = '([a-z0-9])([A-Z])'
local regex_underscores = '(.)_'

-- Lower case the ASCII letters in a string. string.lower goes through the C
-- library and so follows LC_CTYPE, which in Turkish locales maps I onto a
-- dotless i. These names have to match the identifiers the server sends
-- exactly, so the fold must not depend on the locale.
local function ascii_lower(str)
  return (str:gsub('[A-Z]', function(char)
    return string.char(string.byte(char) + 32)
  end))
end

--- Convert camel case to snake case, e.g. GetServices -> get_services
function service.to_snake_case(camel_case)
  local result = camel_case:gsub(regex_underscores, '%1__')
  result = result:gsub(regex_single_uppercase, '%1_%2')
  return ascii_lower(result:gsub(regex_multi_uppercase, '%1_%2'))
end

local KEYWORDS = Set{
  'and', 'break', 'do', 'else', 'elseif', 'end', 'false', 'for', 'function', 'if', 'in',
  'local', 'nil', 'not', 'or', 'repeat', 'return', 'then', 'true', 'until', 'while', 'goto'
}

-- Given a list of parameter names, append underscores to reserved keywords
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

local function _construct_func(svc, procedure_name, prefix_param_names, param_names,
                               param_types, param_required, return_type)
  local invoke, service_name = svc._invoke, svc._name
  prefix_param_names = prefix_param_names or List{}
  param_names = param_names or List{}
  param_types = param_types or List{}
  prefix_param_names = update_param_names(prefix_param_names)
  param_names = update_param_names(param_names)
  local body =
    'return invoke(' ..
    stringx.join(
      ',',
      {'service_name',
       'procedure_name',
       '{'..stringx.join(',', param_names)..'}',
       'param_types',
       'return_type',
       'param_required'
      }) ..
    ')'
  local func = 'return function (' .. stringx.join(',', prefix_param_names..param_names) .. ') ' .. body .. ' end'
  local wrapper =
    'return function (invoke,service_name,procedure_name,param_types,return_type,param_required) ' .. func .. ' end'
  local callable = assert(compat.load(wrapper, '_construct_func('..service_name..','..procedure_name..')'))()
  local result = callable(invoke,service_name,procedure_name,param_types,return_type,param_required)
  service.procedures[result] = {
    service = service_name, procedure = procedure_name, skip = #prefix_param_names
  }
  svc._connection._procedure_parameters[service_name .. '.' .. procedure_name] = {
    types = param_types, required = param_required
  }
  return result
end

--- Flatten a deprecation reason to plain text. The reason may hold the see-cref markup of
--- documentation strings. A reference becomes a dotted name, with the prefix of the current
--- service dropped and a member's name in snake case.
local function _parse_deprecation_reason(reason, service_name)
  local function cref(kind, name)
    if name:sub(1, #service_name + 1) == service_name .. '.' then
      name = name:sub(#service_name + 2)
    end
    if kind == 'M' then
      local path, member = name:match('^(.*%.)([^.]*)$')
      return (path or '') .. service.to_snake_case(member or name)
    end
    return name
  end
  reason = reason:gsub('<see cref=[\'"](%u):([^\'"]*)[\'"]%s*/>', cref)
  return (reason:gsub('<[^>]*>', ''))
end

--- The text of the warning for a use of a deprecated member
function service.deprecation_warning(member, service_name, qualified_name)
  local reason = _parse_deprecation_reason(member.deprecated_reason or '', service_name)
  if reason == '' then
    return qualified_name .. ' is deprecated'
  end
  return qualified_name .. ' is deprecated: ' .. reason
end

--- Deprecate each member of a deprecated service, class, enumeration or structure. A member
--- with no reason of its own takes the reason of its container.
function service.inherit_deprecation(svc)
  local function inherit(member, container)
    if container.deprecated then
      member.deprecated = true
      if (member.deprecated_reason or '') == '' then
        member.deprecated_reason = container.deprecated_reason
      end
    end
  end
  -- A class's reason comes before its service's
  local classes = {}
  for _,cls in ipairs(svc.classes) do
    classes[cls.name] = cls
  end
  for _,procedure in ipairs(svc.procedures) do
    if Attributes.is_a_class_member(procedure.name) then
      inherit(procedure, classes[Attributes.get_class_name(procedure.name)])
    end
  end
  for _,kind in ipairs{'classes', 'enumerations', 'structs', 'exceptions', 'procedures'} do
    for _,member in ipairs(svc[kind]) do
      inherit(member, svc)
    end
  end
  for _,enum in ipairs(svc.enumerations) do
    for _,value in ipairs(enum.values) do
      inherit(value, enum)
    end
  end
  for _,struct in ipairs(svc.structs) do
    for _,field in ipairs(struct.fields) do
      inherit(field, struct)
    end
  end
end

local ServiceBase = class(Types.DynamicType)

--- Whether a value is the service of the given name, from any connection
function service.is_service(value, name)
  return ServiceBase:class_of(value) and value._name == name
end

--- A function that warns of the use of a member, if it is deprecated, then calls the given one
function ServiceBase:_deprecating(member, qualified_name, func)
  if not member.deprecated then
    return func
  end
  local types = self._connection._types
  local message = service.deprecation_warning(member, self._name, self._name .. '.' .. qualified_name)
  local deprecating = function(...)
    types:warn_deprecated(message)
    return func(...)
  end
  -- So that get_call accepts the wrapper, and warns as the wrapper does
  local info = {}
  for k, v in pairs(service.procedures[func]) do
    info[k] = v
  end
  info.deprecation = message
  service.procedures[deprecating] = info
  return deprecating
end

--- Add a class, enumeration or structure type. A deprecated type is left out of the service's
--- class table, so that reading it reaches __index, set up by create_service
function ServiceBase:_add_service_type(member, lua_type)
  if member.deprecated then
    self._deprecated_members[member.name] = {
      value = lua_type,
      message = service.deprecation_warning(member, self._name, self._name .. '.' .. member.name)
    }
  else
    self[member.name] = lua_type
  end
end

function ServiceBase:_parse_procedure(procedure)

  local param_names = seq.copy(seq.map(function (x) return service.to_snake_case(x.name) end, procedure.parameters))
  local param_types = seq.copy(seq.map(function (x) return self._connection._types:as_type(x.type) end, procedure.parameters))
  local return_type = nil
  if procedure.return_type ~= nil then
    return_type = self._connection._types:as_type(procedure.return_type)
  end
  -- Checked before anything is built from the types, so that a procedure naming a structure
  -- whose definition was skipped is skipped along with it
  for _,typ in ipairs(param_types) do
    Types.check_type_is_known(typ)
  end
  if return_type ~= nil then
    Types.check_type_is_known(return_type)
  end
  local param_required = seq.copy(seq.map(function (x) return not x.has_default_value end, procedure.parameters))
  return param_names, param_types, param_required, return_type
end

--- Register the types that a service defines in the given type registry
function service.register_definitions(types, svc)
  for _,cls in ipairs(svc.classes) do
    types:class_type(svc.name, cls.name)
  end
  for _,enum in ipairs(svc.enumerations) do
    local values = {}
    local deprecated = {}
    for _,value in ipairs(enum.values) do
      local name = service.to_snake_case(value.name)
      values[name] = value.value
      if value.deprecated then
        deprecated[name] = service.deprecation_warning(value, svc.name, svc.name .. '.' .. enum.name .. '.' .. name)
      end
    end
    types:enumeration_type(svc.name, enum.name):set_values(values, enum.flags, deprecated, function(message)
      types:warn_deprecated(message)
    end)
  end
  for _,struct in ipairs(svc.structs) do
    -- A structure whose fields cannot all be resolved is left without any, so that whatever
    -- names it is skipped and the service still builds
    local known = true
    for _,field in ipairs(struct.fields) do
      known = known and Types.is_a_known_type(field.type)
    end
    if known then
      local fields = {}
      for i,field in ipairs(struct.fields) do
        fields[i] = { service.to_snake_case(field.name), types:as_type(field.type) }
      end
      types:struct_type(svc.name, struct.name):set_fields(fields)
    else
      io.stderr:write('Skipping the struct ' .. svc.name .. '.' .. struct.name .. ', as the ' ..
                      'type of one of its fields is not a type this client knows about\n')
    end
  end
end

-- The types a service defines are registered from its definitions, before any service is
-- created, so the following two take the types they were registered as and make them members
-- of the service

--- Add a class type
function ServiceBase:_add_service_class(cls)
  self:_add_service_type(cls, self._connection._types:class_type(self._name, cls.name).lua_type)
end

--- Add an enumeration type
function ServiceBase:_add_service_enumeration(enum)
  self:_add_service_type(enum, self._connection._types:enumeration_type(self._name, enum.name).lua_type)
end

--- Add a structure type
function ServiceBase:_add_service_struct(struct)
  self:_add_service_type(struct, self._connection._types:struct_type(self._name, struct.name).lua_type)
end

--- Add a procedure
function ServiceBase:_add_service_procedure(procedure)
  local param_names, param_types, param_required, return_type = self:_parse_procedure(procedure)
  local func = _construct_func(self, procedure.name, List{}, param_names, param_types, param_required, return_type)
  --build_request = ...
  local name = service.to_snake_case(procedure.name)
  return self:_add_static_method(name, self:_deprecating(procedure, name, func))
end

function ServiceBase:_add_service_property(name, getter, setter)
  name = service.to_snake_case(name)
  if getter then
    local getter_name = getter.name
    local _,_,_,return_type = self:_parse_procedure(getter)
    getter = self:_deprecating(getter, name,
      _construct_func(self, getter_name, List{'self'}, nil, nil, nil, return_type))
  end
  if setter then
    local setter_name = setter.name
    local param_names, param_types, param_required = self:_parse_procedure(setter)
    setter = self:_deprecating(setter, name,
      _construct_func(self, setter_name, List{'self'}, param_names, param_types, param_required, nil))
  end
  return self:_add_property(name, getter, setter)
end

--- Add a method to a class
function ServiceBase:_add_service_class_method(class_name, method_name, procedure)
  local class_cls = self._connection._types:class_type(self._name, class_name).lua_type
  local param_names, param_types, param_required, return_type = self:_parse_procedure(procedure)
  -- Rename this to self if it doesn't cause a name clash
  --if 'self' not in param_names:
  --    param_names[0] = 'self'
  local func = _construct_func(self, procedure.name, nil, param_names, param_types, param_required, return_type)
  --build_request = ...
  local name = service.to_snake_case(method_name)
  class_cls:_add_method(name, self:_deprecating(procedure, class_name .. '.' .. name, func))
end

--- Add a static method to a class
function ServiceBase:_add_service_class_static_method(class_name, method_name, procedure)
  local class_cls = self._connection._types:class_type(self._name, class_name).lua_type
  local param_names, param_types, param_required, return_type = self:_parse_procedure(procedure)
  local func = _construct_func(self, procedure.name, nil, param_names, param_types, param_required, return_type)
  --local build_request = ...
  local name = service.to_snake_case(method_name)
  class_cls:_add_static_method(name, self:_deprecating(procedure, class_name .. '.' .. name, func))
end

--- Add a property to a class
function ServiceBase:_add_service_class_property(class_name, property_name, getter, setter)
  local class_cls = self._connection._types:class_type(self._name, class_name).lua_type
  local qualified_name = class_name .. '.' .. service.to_snake_case(property_name)
  if getter then
    local getter_name = getter.name
    local param_names, param_types, param_required, return_type = self:_parse_procedure(getter)
    -- Rename this to self if it doesn't cause a name clash
    --if 'self' not in param_names:
    --  param_names[0] = 'self'
    getter = self:_deprecating(getter, qualified_name,
      _construct_func(self, getter_name, nil, param_names, param_types, param_required, return_type))
    --local build_request = ...
  end
  if setter then
    local param_names, param_types, param_required = self:_parse_procedure(setter)
    setter = self:_deprecating(setter, qualified_name,
      _construct_func(self, setter.name, nil, param_names, param_types, param_required, nil))
  end
  property_name = service.to_snake_case(property_name)
  return class_cls:_add_property(property_name, getter, setter)
end

--- Run one step of building a service, skipping it with a warning if it names a type this
--- client does not know about, so that the service is still created. A definition from a
--- newer server carries such a type where a member has been added from a later version of
--- the protocol.
local function _skipping_unknown_types(what, build)
  local ok, err = pcall(build)
  if not ok then
    if type(err) ~= 'string' or not err:find(Types.UNKNOWN_TYPE_ERROR, 1, true) then
      error(err)
    end
    io.stderr:write('Skipping ' .. what .. ': ' .. tostring(err) .. '\n')
  end
end

--- Create a new service type
function service.create_service(connection, svc)
  local cls = class(ServiceBase)
  cls._connection = connection
  cls._name = svc.name
  cls._invoke = bind1(connection._invoke, connection)

  -- The deprecated types of the service, which warn when read
  local deprecated_members = {}
  cls._deprecated_members = deprecated_members
  local index = cls.__index
  cls.__index = function(t, key)
    local member = deprecated_members[key]
    if member ~= nil then
      connection._types:warn_deprecated(member.message)
      return member.value
    end
    return index(t, key)
  end

  -- Add class types to svc
  for _,cls2 in ipairs(svc.classes) do
    cls:_add_service_class(cls2)
  end

  -- Add enumeration types to svc
  for _,enum in ipairs(svc.enumerations) do
    cls:_add_service_enumeration(enum)
  end

  -- Add structure types to svc
  for _,struct in ipairs(svc.structs) do
    if connection._types:struct_type(svc.name, struct.name).lua_type ~= nil then
      cls:_add_service_struct(struct)
    end
  end

  -- Add procedures. An internal procedure is for client libraries only
  for _,procedure in ipairs(svc.procedures) do
    if Attributes.is_a_procedure(procedure.name) and not procedure.internal then
      _skipping_unknown_types(svc.name .. '.' .. procedure.name, function ()
        cls:_add_service_procedure(procedure)
      end)
    end
  end

  -- Add properties
  local properties = {}
  for _,procedure in ipairs(svc.procedures) do
    if Attributes.is_a_property_accessor(procedure.name) then
      local name = Attributes.get_property_name(procedure.name)
      if not properties[name] then
        properties[name] = {}
      end
      if Attributes.is_a_property_getter(procedure.name) then
        properties[name]['get'] = procedure
      else
        properties[name]['set'] = procedure
      end
    end
  end
  for name, procedures in pairs(properties) do
    _skipping_unknown_types(svc.name .. '.' .. name, function ()
      cls:_add_service_property(name, procedures['get'], procedures['set'])
    end)
  end

  -- Add class methods
  for _,procedure in ipairs(svc.procedures) do
    if Attributes.is_a_class_method(procedure.name) then
      local class_name = Attributes.get_class_name(procedure.name)
      local method_name = Attributes.get_class_member_name(procedure.name)
      _skipping_unknown_types(svc.name .. '.' .. class_name .. '.' .. method_name, function ()
        cls:_add_service_class_method(class_name, method_name, procedure)
      end)
    end
  end

  -- Add static class methods
  for _,procedure in ipairs(svc.procedures) do
    if Attributes.is_a_class_static_method(procedure.name) then
      local class_name = Attributes.get_class_name(procedure.name)
      local method_name = Attributes.get_class_member_name(procedure.name)
      _skipping_unknown_types(svc.name .. '.' .. class_name .. '.' .. method_name, function ()
        cls:_add_service_class_static_method(class_name, method_name, procedure)
      end)
    end
  end

  -- Add class properties
  properties = {}
  for _,procedure in ipairs(svc.procedures) do
    if Attributes.is_a_class_property_accessor(procedure.name) then
      local class_name = Attributes.get_class_name(procedure.name)
      local property_name = Attributes.get_class_member_name(procedure.name)
      local key = class_name..'.'..property_name
      if not properties[key] then
        properties[key] = {}
      end
      if Attributes.is_a_class_property_getter(procedure.name) then
        properties[key]['get'] = procedure
      else
        properties[key]['set'] = procedure
      end
    end
  end
  for key, procedures in pairs(properties) do
    local class_name, _, property_name = stringx.partition(key, '.')
    _skipping_unknown_types(svc.name .. '.' .. key, function ()
      cls:_add_service_class_property(class_name, property_name, procedures['get'], procedures['set'])
    end)
  end

  return cls()
end

return service
