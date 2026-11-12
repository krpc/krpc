local class = require 'pl.class'
local List = require 'pl.List'
local Types = require 'krpc.types'
local encoder = require 'krpc.encoder'
local decoder = require 'krpc.decoder'
local Service = require 'krpc.service'
local to_snake_case = Service.to_snake_case
local create_service = Service.create_service
local register_definitions = Service.register_definitions

local protobuf = require 'krpc.protobuf'
local Transport = require 'krpc.transport'

local Connection = class()

function Connection:_init(transport)
  self._types = Types()
  self._transport = transport
  -- The id of the next request, which starts at 1
  self._next_id = 1
  -- The types of each server side function's parameters and value, by object id
  self._function_type_cache = {}
  -- The types of each procedure's parameters, and which have no default, by qualified name
  self._procedure_parameters = {}

  -- Set up the main KRPC service
  local services = self:_invoke('KRPC', 'GetServices', nil, nil, self._types:services_type()).services

  -- Register the types of every service before creating any of them, as a service's procedures
  -- are built from types that any service may define
  for _, service in ipairs(services) do
    Service.inherit_deprecation(service)
    register_definitions(self._types, service)
  end

  -- Set up services
  self._deprecated_services = {}
  for _, service in ipairs(services) do
    local name = to_snake_case(service.name)
    local svc = create_service(self, service)
    if service.deprecated then
      self._deprecated_services[name] = {
        value = svc,
        message = Service.deprecation_warning(service, service.name, service.name)
      }
    else
      self[name] = svc
    end
  end
end

-- A deprecated service is left out of the client's table, so that reading it reaches here and warns
Connection.__index = function(connection, key)
  local value = Connection[key]
  if value ~= nil then
    return value
  end
  local deprecated = rawget(connection, '_deprecated_services')
  local service = deprecated and deprecated[key]
  if service ~= nil then
    connection._types:warn_deprecated(service.message)
    return service.value
  end
end

--- Tell the server the connection is closing, then close it
function Connection:close()
  if self._error ~= nil then
    self._transport:close()
    return
  end
  self._error = 'Connection closed'
  pcall(function()
    self._transport:send(encoder.encode_in_message('Disconnect', {
      reason = protobuf.enum('Disconnect.Reason', 'CLOSED')
    }))
    self._transport:half_close_and_close()
  end)
  self._transport:close()
end

--- End the connection on a message the server should not have sent, or one that fails to
--- parse, sending the server a disconnect with the given reason, PROTOCOL_ERROR by default
function Connection:_protocol_error(text, reason)
  self._error = text
  pcall(function()
    self._transport:send(encoder.encode_in_message('Disconnect', {
      reason = protobuf.enum('Disconnect.Reason', reason or 'PROTOCOL_ERROR'),
      message = text
    }))
    self._transport:half_close_and_close()
  end)
  self._transport:close()
  error(text)
end

--- Read from or write to the connection. A failure ends the connection, and every later call
--- raises the same error.
function Connection:_io(operation, ...)
  local ok, result = pcall(operation, self._transport, ...)
  if not ok then
    self._error = tostring(result)
    self._transport:close()
    error(result, 0)
  end
  return result
end

--- Read messages until the response with the given id arrives, and return it and the value of
--- its result
function Connection:_receive_response(id, return_type)
  while true do
    local ok, data = pcall(self._transport.receive_message_data, self._transport)
    if not ok and data == Transport.MALFORMED_SIZE then
      self:_protocol_error(data, 'MALFORMED_MESSAGE')
    elseif not ok then
      self._error = tostring(data)
      self._transport:close()
      error(data, 0)
    end
    local message, value = decoder.decode_message_and_value(data, return_type)
    if message == nil then
      self:_protocol_error('Failed to parse a message from the server', 'MALFORMED_MESSAGE')
    end
    local response = message.response
    if response ~= nil then
      if response.id ~= id then
        self:_protocol_error('Response with an unknown id ' .. response.id .. ' from the server')
      end
      return response, value
    end
    local disconnect = message.disconnect
    if disconnect ~= nil then
      local reason = protobuf.enum('Disconnect.Reason', disconnect.reason)
      local text = 'The server ended the connection (' ..
        tostring(reason or disconnect.reason) .. ')'
      if disconnect.message ~= '' then
        text = text .. ': ' .. disconnect.message
      end
      self._error = text
      self._transport:close()
      error(text)
    end
    if message.request ~= nil or message.stream_update ~= nil or
       message.connection_request ~= nil or message.connection_response ~= nil then
      self:_protocol_error('Unexpected message from the server')
    end
    if not decoder.well_formed(data) then
      self:_protocol_error('Failed to parse a message from the server', 'MALFORMED_MESSAGE')
    end
    -- A case this client does not know is skipped
  end
end

--- Execute an RPC. A nil argument is left out, so that the parameter takes its default. At a
--- parameter with no default, a nil argument is a null if the parameter can be null, and an error
--- otherwise.
function Connection:_invoke(service, procedure, args, param_types, return_type, param_required)
  args = args or List{}
  param_types = param_types or List{}

  if self._error ~= nil then
    error(self._error)
  end

  -- Build the request
  local id = self._next_id
  self._next_id = id + 1
  local request = self:_build_request(id, service, procedure, args, param_types, param_required)

  -- Send the request, which is already the bytes to send
  self:_io(self._transport.send, request)
  local response, value = self:_receive_response(id, return_type)

  -- Check for an error response
  if response.error ~= nil then
    error(self:_error_message(response.error))
  end

  local result = response.results[1]
  if result.error ~= nil then
    error(self:_error_message(result.error))
  end

  -- Return the (optional) result
  if return_type then
    -- A null result is signaled out-of-band by is_null; the value field is unset
    if result.is_null then
      if not return_type.nullable then
        error(string.format('%s.%s() returned null, for a result that cannot be null', service, procedure))
      end
      return nil
    end
    return value
  end
  return nil
end

-- The name of a type in an error message, without the ? that marks a nullable type
local function type_name(typ)
  return (typ._string:gsub('%?$', ''))
end

--- Encode the arguments of a procedure call, coerced to the types of their parameters, with
--- Types.none for a null argument and nil for an argument left out
function Connection:_encode_arguments(service, procedure, args, param_types, param_required)
  local arguments = {}

  for i,typ in ipairs(param_types) do
    local value = args[i]
    -- A nil argument with no default is a null
    if value == nil and param_required and param_required[i] then
      value = Types.none
    end
    if rawequal(value, Types.none) and not typ.nullable then
      local given = args[i] == nil and 'nil' or 'null'
      error(string.format('%s.%s() argument %d must be a %s, got %s', service, procedure, i, type_name(typ), given))
    end
    -- A nil argument stays out of arguments, so the server uses the parameter's default
    if rawequal(value, Types.none) then
      -- A null argument is signaled out-of-band by is_null; the value field is left unset
      arguments[i] = Types.none
    elseif value ~= nil then
      local valid = false
      if type(typ.lua_type) == 'string' then
        valid = typ.lua_type == type(value)
      elseif type(typ.lua_type) == 'table' then
        valid = typ.lua_type:class_of(value)
      end
      if not valid then
        local given = type(value)
        -- Only ok is declared here, as the coerced value is read below, outside this block
        local ok
        ok,value = pcall(self._types.coerce_to, self._types, value, typ)
        if not ok then
          error(string.format('%s.%s() argument %d must be a %s, got a %s', service, procedure, i, type_name(typ), given))
        end
      end
      arguments[i] = encoder.encode(value, typ)
    end
  end

  return arguments
end

--- Build the bytes of the request carrying a procedure call, ready to send
function Connection:_build_request(id, service, procedure, args, param_types, param_required)
  local arguments = self:_encode_arguments(service, procedure, args, param_types, param_required)
  return encoder.encode_request(id, service, procedure, arguments, #param_types)
end

--- The call a procedure of a service or a class makes when called with the given arguments, as
--- a KRPC.ProcedureCall message to pass to KRPC.Expression.call. A property is read through its
--- getter, get_<name>, and set through its setter, set_<name>, each given the object or the
--- service as its first argument.
function Connection:get_call(procedure, ...)
  local info = Service.procedures[procedure]
  if info == nil then
    error('get_call expects a procedure of a service or a class', 2)
  end
  if info.deprecation ~= nil then
    self._types:warn_deprecated(info.deprecation)
  end
  local parameters = self._procedure_parameters[info.service .. '.' .. info.procedure]
  if parameters == nil then
    error('The server has no procedure ' .. info.service .. '.' .. info.procedure, 2)
  end
  local given = {...}
  if info.skip > 0 and not Service.is_service(given[1], info.service) then
    error('get_call expects the service ' .. info.service .. ' as the first argument of ' .. info.procedure, 2)
  end
  local args = {}
  for i = info.skip + 1, select('#', ...) do
    args[i - info.skip] = given[i]
  end
  local encoded = self:_encode_arguments(info.service, info.procedure, args, parameters.types,
                                         parameters.required)
  local arguments = {}
  for i = 1, #parameters.types do
    local value = encoded[i]
    if rawequal(value, Types.none) then
      arguments[#arguments+1] = { position = i-1, is_null = true }
    elseif value ~= nil then
      arguments[#arguments+1] = { position = i-1, value = value }
    end
  end
  return { service = info.service, procedure = info.procedure, arguments = arguments }
end

--- The type of a server side function, a KRPC.Expression object
function Connection:_expression_type()
  return self._types:class_type('KRPC', 'Expression')
end

--- Raise an error for a value that is not a KRPC.Expression object
function Connection:_check_function(func)
  if not (Types.ClassBase:class_of(func) and func._service_name == 'KRPC' and
          func._class_name == 'Expression') then
    error('Expected a KRPC.Expression object, got ' .. tostring(func), 3)
  end
end

-- The type codes of the types that a service defines, and of those that hold other types
local _DEFINED_TYPES = { [Types.CLASS] = true, [Types.ENUMERATION] = true, [Types.STRUCT] = true }
local _COLLECTION_TYPES = { [Types.TUPLE] = true, [Types.LIST] = true, [Types.SET] = true,
                            [Types.DICTIONARY] = true }

--- The protocol buffer type described by a KRPC.Type object. Each property is a call, so only
--- those the type code needs are read.
local function _type_message(remote_type)
  local code = remote_type.code.value
  local service, name, types = '', '', {}
  if _DEFINED_TYPES[code] then
    service = remote_type.service
    name = remote_type.name
  elseif _COLLECTION_TYPES[code] then
    for i, typ in ipairs(remote_type.types) do
      types[i] = _type_message(typ)
    end
  end
  return { code = code, service = service, name = name, types = types,
           nullable = remote_type.nullable }
end

--- The types of a server side function's parameters, which of them are required, and the type of
--- its value or false for a function with no value. Each is introspected on the server once per
--- function.
function Connection:_function_types(func)
  local cached = self._function_type_cache[func._object_id]
  if cached ~= nil then
    return cached.param_types, cached.param_required, cached.return_type
  end
  local param_types = List{}
  local param_required = {}
  for i, typ in ipairs(func.parameter_types) do
    param_types:append(self._types:as_type(_type_message(typ)))
    -- A function's parameter has no default
    param_required[i] = true
  end
  local return_type = false
  if func.has_return_type then
    return_type = self._types:as_type(_type_message(func.return_type))
  end
  self._function_type_cache[func._object_id] = {
    param_types = param_types, param_required = param_required, return_type = return_type
  }
  return param_types, param_required, return_type
end

--- Compile a server side function built with the expression API, a KRPC.Expression object, on
--- the server, so that its first run does not compile it, and return it.
function Connection:compile(func)
  self:_check_function(func)
  self:_invoke('KRPC', 'CompileFunction', List{func}, List{self:_expression_type()})
  return func
end

--- Run a server side function, a KRPC.Expression object, within a single physics tick, and
--- return its value. The arguments are passed to the function's parameters, in order. The
--- value is nil when the function has no value or its value is null.
function Connection:run(func, ...)
  self:_check_function(func)
  -- A function from another connection reads its types through this one
  func = self._types:coerce_to(func, self:_expression_type())
  local param_types, param_required, return_type = self:_function_types(func)
  local count = select('#', ...)
  if count ~= #param_types then
    error(string.format('The function takes %d argument%s, got %d', #param_types,
                        #param_types == 1 and '' or 's', count), 2)
  end
  local arguments = List(self:_encode_arguments('Connection', 'run', {...}, param_types, param_required))
  local bytes = self._types:bytes_type()
  local data = self:_invoke('KRPC', 'RunFunction', List{func, arguments},
                            List{self:_expression_type(), self._types:list_type(self._types:nullable(bytes))},
                            self._types:nullable(bytes))
  -- A null value has no data
  if data == nil or not return_type then
    return nil
  end
  return decoder.decode(data, return_type)
end

--- Construct an error description from a KRPC.Error object
function Connection:_error_message(err)
   local msg = err.description
  if err.service ~= '' and err.exception ~= '' then
    msg = err.service .. '.' .. err.exception .. ': ' .. msg
  end
  if err.stack_trace ~= '' then
    msg = msg .. '\nServer stack trace:\n' .. err.stack_trace
  end
  return msg
end

return Connection
