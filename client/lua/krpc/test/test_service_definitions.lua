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

return TestServiceDefinitions
