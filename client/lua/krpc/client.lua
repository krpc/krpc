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

local Client = class()

function Client:_init(rpc_connection, stream_connection)
  self._types = Types()
  self._rpc_connection = rpc_connection
  self._stream_connection = stream_connection
  self._request_type = self._types:as_type('KRPC.Request')
  self._response_type = self._types:as_type('KRPC.Response')

  -- Set up the main KRPC service
  local services = self:_invoke('KRPC', 'GetServices', nil, nil, nil, nil, self._types:as_type('KRPC.Services')).services

  -- Set up services
  for _,service in ipairs(services) do
    self[to_snake_case(service.name)] = create_service(self, service)
  end
end

function Client:close()
  self._rpc_connection:close()
  if self._stream_connection then
    self._stream_connection:close()
  end
end

--- Execute an RPC
function Client:_invoke(service, procedure, args, kwargs, param_names, param_types, return_type)
  args = args or List{}
  kwargs = kwargs or Map{}
  param_names = param_names or List{}
  param_types = param_types or List{}

  -- Build the request
  local request = self:_build_request(service, procedure, args, kwargs, param_names, param_types, return_type)

  -- Send the request
  self:_send_request(request)
  local response = self:_receive_response()

  -- Check for an error response
  if response:HasField('error') then
    error(response.error)
  end

  -- Decode the response and return the (optional) result
  local result = nil
  if return_type then
    result = decoder.decode(response.return_value, return_type)
  end
  return result
end

--- Build a KRPC.Request object
function Client:_build_request(service, procedure, args, kwargs, param_names, param_types, return_type)
  local request = schema.Request()
  request.service = service
  request.procedure = procedure

  local function encode_argument(i, value)
    local typ = param_types[i]
    local valid = false
    if type(typ.lua_type) == 'string' then
      valid = typ.lua_type == type(value)
    elseif type(typ.lua_type) == 'table' then
      valid = typ.lua_type:class_of(value)
    end
    if not valid then
      ok,coerced_value = pcall(self._types.coerce_to, self._types, value, typ)
      if not ok then
        error(string.format('%s.%s() argument %d must be a %s, got a %s', service, procedure, i, typ.lua_type, type(value)))
      end
    end
    return encoder.encode(value, typ)
  end

  if args:len() > param_types:len() then
    error(string.format('%s.%s() takes exactly %d arguments (%d given)',
                        service, procedure, param_types:len(), args:len()))
  end

  local nargs = args:len()
  for i,param in ipairs(param_names) do
    local arg
    local add = false
    if i <= nargs and args[i] ~= nil then
      arg = args[i]
      add = true
    elseif kwargs[param] ~= nil then
      arg = kwargs[param]
      add = true
    end
    if add then
      argument = request.arguments:add()
      argument.position = i-1
      argument.value = encode_argument(i, arg)
    end
  end

  return request
end

function Client:_send_request(request)
  data = encoder.encode_delimited(request, self._request_type)
  self._rpc_connection:send(data)
end

function Client:_receive_response()
  local size
  local data = ''
  while true do
    data = data .. self._rpc_connection:receive(1)
    local ok, result = pcall(decoder.decode_size_and_position, data)
    if ok then
      size = result
      break
    end
  end

  data = self._rpc_connection:receive(size)
  return decoder.decode(data, self._response_type)
end

return Client
