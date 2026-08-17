local class = require 'pl.class'
local Map = require 'pl.Map'
local List = require 'pl.List'
local schema = require 'krpc.schema.KRPC'
local Types = require 'krpc.types'
local Attributes = require 'krpc.attributes'
local encoder = require 'krpc.encoder'
local decoder = require 'krpc.decoder'
local service = require 'krpc.service'
local to_snake_case = service.to_snake_case
local create_service = service.create_service
local register_definitions = service.register_definitions

local Client = class()

function Client:_init(rpc_connection)
  self._types = Types()
  self._rpc_connection = rpc_connection

  -- Set up the main KRPC service
  local services = self:_invoke('KRPC', 'GetServices', nil, nil, self._types:services_type()).services

  -- Register the types of every service before creating any of them: a service's procedures
  -- are built from types that any service may define, and a default value cannot be decoded
  -- before the values of the enumeration it belongs to are known
  for _, service in ipairs(services) do
    register_definitions(self._types, service)
  end

  -- Set up services
  for _, service in ipairs(services) do
    self[to_snake_case(service.name)] = create_service(self, service)
  end
end

function Client:close()
  self._rpc_connection:close()
end

--- Execute an RPC
function Client:_invoke(service, procedure, args, param_types, return_type)
  args = args or List{}
  param_types = param_types or List{}

  -- Build the request
  local request = self:_build_request(service, procedure, args, param_types)

  -- Send the request
  self._rpc_connection:send_message(request)
  local response = self._rpc_connection:receive_message(schema.Response)

  -- Check for an error response
  if response:HasField('error') then
    error(self:_error_message(response.error))
  end

  local result = response.results[1]
  if result:HasField('error') then
    error(self:_error_message(result.error))
  end

  -- Decode the response and return the (optional) result
  if return_type then
    -- A null result is signaled out-of-band by is_null; the value field is unset
    if result.is_null then
      return Types.none
    end
    return decoder.decode(result.value, return_type)
  end
  return nil
end

--- Build a KRPC.Request object
function Client:_build_request(service, procedure, args, param_types)
  local request = schema.Request()
  local call = request.calls:add()
  call.service = service
  call.procedure = procedure

  for i,value in ipairs(args) do
    local typ = param_types[i]
    local arg = call.arguments:add()
    arg.position = i-1
    if value == Types.none then
      -- A null argument is signaled out-of-band by is_null; the value field is left unset
      arg.is_null = true
    else
      local valid = false
      if type(typ.lua_type) == 'string' then
        valid = typ.lua_type == type(value)
      elseif type(typ.lua_type) == 'table' then
        valid = typ.lua_type:class_of(value)
      end
      if not valid then
        -- Only ok is declared here; value is the loop variable and must stay so, as the
        -- coerced value is read below, outside this block
        local ok
        ok,value = pcall(self._types.coerce_to, self._types, value, typ)
        if not ok then
          error(string.format('%s.%s() argument %d must be a %s, got a %s', service, procedure, i, typ.lua_type, type(value)))
        end
      end
      arg.value = encoder.encode(value, typ)
    end
  end

  return request
end

--- Construct an error description from a KRPC.Error object
function Client:_error_message(err)
   local msg = err.description
  if err:HasField('service') and err:HasField('name') then
    msg = err.service .. '.' .. err.name .. ': ' .. msg
  end
  if err:HasField('stack_trace') then
    msg = msg .. '\nServer stack trace:\n' .. err.stack_trace
  end
  return msg
end

return Client
