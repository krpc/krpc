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
local Connection = require 'krpc.connection'

local Client = class()

function Client:_init(connection)
  self._types = Types()
  self._connection = connection
  -- The id of the next request, which starts at 1
  self._next_id = 1

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

--- Tell the server the connection is closing, then close it
function Client:close()
  if self._error ~= nil then
    self._connection:close()
    return
  end
  self._error = 'Connection closed'
  pcall(function()
    self._connection:send(encoder.encode_in_message('Disconnect', {
      reason = protobuf.enum('Disconnect.Reason', 'CLOSED')
    }))
    self._connection:half_close_and_close()
  end)
  self._connection:close()
end

--- End the connection on a message the server should not have sent, or one that fails to
--- parse, sending the server a disconnect with the given reason, PROTOCOL_ERROR by default
function Client:_protocol_error(text, reason)
  self._error = text
  pcall(function()
    self._connection:send(encoder.encode_in_message('Disconnect', {
      reason = protobuf.enum('Disconnect.Reason', reason or 'PROTOCOL_ERROR'),
      message = text
    }))
    self._connection:half_close_and_close()
  end)
  self._connection:close()
  error(text)
end

--- Read from or write to the connection. A failure ends the connection, and every later call
--- raises the same error.
function Client:_transport(operation, ...)
  local ok, result = pcall(operation, self._connection, ...)
  if not ok then
    self._error = tostring(result)
    self._connection:close()
    error(result, 0)
  end
  return result
end

--- Read messages until the response with the given id arrives, and return it and the value of
--- its result
function Client:_receive_response(id, return_type)
  while true do
    local ok, data = pcall(self._connection.receive_message_data, self._connection)
    if not ok and data == Connection.MALFORMED_SIZE then
      self:_protocol_error(data, 'MALFORMED_MESSAGE')
    elseif not ok then
      self._error = tostring(data)
      self._connection:close()
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
      self._connection:close()
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

--- Execute an RPC
function Client:_invoke(service, procedure, args, param_types, return_type)
  args = args or List{}
  param_types = param_types or List{}

  if self._error ~= nil then
    error(self._error)
  end

  -- Build the request
  local id = self._next_id
  self._next_id = id + 1
  local request = self:_build_request(id, service, procedure, args, param_types)

  -- Send the request, which is already the bytes to send
  self:_transport(self._connection.send, request)
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
      return Types.none
    end
    return value
  end
  return nil
end

--- Build the bytes of the request carrying a procedure call, ready to send
function Client:_build_request(id, service, procedure, args, param_types)
  local arguments = {}

  for i,value in ipairs(args) do
    local typ = param_types[i]
    if rawequal(value, Types.none) then
      -- A null argument is signaled out-of-band by is_null; the value field is left unset
      arguments[i] = Types.none
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
      arguments[i] = encoder.encode(value, typ)
    end
  end

  return encoder.encode_request(id, service, procedure, arguments)
end

--- Construct an error description from a KRPC.Error object
function Client:_error_message(err)
   local msg = err.description
  if err.service ~= '' and err.name ~= '' then
    msg = err.service .. '.' .. err.name .. ': ' .. msg
  end
  if err.stack_trace ~= '' then
    msg = msg .. '\nServer stack trace:\n' .. err.stack_trace
  end
  return msg
end

return Client
