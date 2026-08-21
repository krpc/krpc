local luaunit = require 'luaunit'
local class = require 'pl.class'
local List = require 'pl.List'
local schema = require 'krpc.schema.KRPC'
local Types = require 'krpc.types'
local encoder = require 'krpc.encoder'
local service = require 'krpc.service'

local TestServiceDefinitions = class()

--- As much of a client as creating a service needs, without a connection to a server
local FakeClient = class()

function FakeClient:_init()
  self._types = Types()
end

function FakeClient:_invoke()
  error('not connected to a server')
end

--- A service defining an enumeration and nothing else
local function enum_service(name)
  local svc = schema.Service()
  svc.name = name
  local enum = svc.enumerations:add()
  enum.name = 'Mode'
  for _,case in ipairs({{'Slow', 1}, {'Fast', 2}}) do
    local value = enum.values:add()
    value.name = case[1]
    value.value = case[2]
  end
  return svc
end

--- A service with a procedure whose parameter defaults to a member of another service's
--- enumeration, either directly or through a list
local function default_service(name, enum_service_name, in_a_list)
  local types = Types()
  local svc = schema.Service()
  svc.name = name
  local procedure = svc.procedures:add()
  procedure.name = 'Frob'
  local parameter = procedure.parameters:add()
  parameter.name = 'mode'
  local encoded = encoder.encode(2, types:sint32_type())
  if in_a_list then
    parameter.type.code = Types.LIST
    local value_type = parameter.type.types:add()
    value_type.code = Types.ENUMERATION
    value_type.service = enum_service_name
    value_type.name = 'Mode'
    local items = schema.List()
    items.items:append(encoded)
    encoded = items:SerializeToString()
  else
    parameter.type.code = Types.ENUMERATION
    parameter.type.service = enum_service_name
    parameter.type.name = 'Mode'
  end
  parameter.has_default_value = true
  parameter.default_value = encoded
  return svc
end

-- A type code from a later version of the protocol than this client knows about
local UNKNOWN_TYPE_CODE = 9999

--- A service defining a structure with a single field, whose type the given function sets
local function struct_service(name, set_field_type)
  local svc = schema.Service()
  svc.name = name
  local struct = svc.structs:add()
  struct.name = 'Thing'
  local field = struct.fields:add()
  field.name = 'Value'
  set_field_type(field.type)
  return svc
end

--- A service with a procedure taking a parameter of another service's structure
local function struct_using_service(name, struct_service_name)
  local svc = schema.Service()
  svc.name = name
  local procedure = svc.procedures:add()
  procedure.name = 'Frob'
  local parameter = procedure.parameters:add()
  parameter.name = 'thing'
  parameter.type.code = Types.STRUCT
  parameter.type.service = struct_service_name
  parameter.type.name = 'Thing'
  return svc
end

local function unknown_field_type(typ)
  typ.code = UNKNOWN_TYPE_CODE
end

local function struct_field_type(service_name)
  return function (typ)
    typ.code = Types.STRUCT
    typ.service = service_name
    typ.name = 'Thing'
  end
end

local function create_services(svcs)
  local client = FakeClient()
  for _,svc in ipairs(svcs) do
    service.register_definitions(client._types, svc)
  end
  local created = {}
  for _,svc in ipairs(svcs) do
    created[svc.name] = service.create_service(client, svc)
  end
  return client, created
end

--- Create the given services, and return the client along with the default value that
--- ServiceA decoded for the parameter of its procedure
local function create_services_and_default(svcs)
  local client, created = create_services(svcs)
  luaunit.assertNotNil(created['ServiceA'].frob)
  local procedure
  for _,svc in ipairs(svcs) do
    if svc.name == 'ServiceA' then
      procedure = svc.procedures[1]
    end
  end
  local _,_,_,param_default = created['ServiceA']:_parse_procedure(procedure)
  return client, param_default[1]
end

function TestServiceDefinitions:check_services(svcs, in_a_list)
  local client, default = create_services_and_default(svcs)
  local fast = client._types:enumeration_type('ServiceB', 'Mode').lua_type.fast
  luaunit.assertEquals(2, fast.value)
  if in_a_list then
    luaunit.assertEquals(List{fast}, default)
  else
    luaunit.assertEquals(fast, default)
  end
end

function TestServiceDefinitions:test_defined_before_it_is_used()
  self:check_services({enum_service('ServiceB'), default_service('ServiceA', 'ServiceB')})
end

function TestServiceDefinitions:test_defined_after_it_is_used()
  self:check_services({default_service('ServiceA', 'ServiceB'), enum_service('ServiceB')})
end

function TestServiceDefinitions:test_used_inside_a_collection()
  self:check_services({default_service('ServiceA', 'ServiceB', true), enum_service('ServiceB')}, true)
end

function TestServiceDefinitions:check_struct_is_skipped(svcs)
  local _, created = create_services(svcs)
  luaunit.assertNil(created['ServiceA'].frob)
end

function TestServiceDefinitions:test_procedure_naming_a_skipped_struct_is_skipped()
  local _, created = create_services({
    struct_service('ServiceB', unknown_field_type),
    struct_using_service('ServiceA', 'ServiceB')
  })
  luaunit.assertNil(created['ServiceA'].frob)
  luaunit.assertNil(created['ServiceB'].Thing)
end

function TestServiceDefinitions:test_struct_holding_a_skipped_struct_is_skipped()
  self:check_struct_is_skipped({
    struct_service('ServiceC', unknown_field_type),
    struct_service('ServiceB', struct_field_type('ServiceC')),
    struct_using_service('ServiceA', 'ServiceB')
  })
end

return TestServiceDefinitions
