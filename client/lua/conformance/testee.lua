-- The conformance testee for the Lua client. Reads commands from the conformance driver on
-- standard input, runs them against the client in order, and writes the replies to standard
-- output. See conformance/testee.proto for the protocol.
--
-- Calls go through the service objects the client builds from the server's definitions.

local pb = require 'pb'
local protojson = require 'conformance.protojson'
local List = require 'pl.List'
local Set = require 'pl.Set'
local Map = require 'pl.Map'
local krpc = require 'krpc.init'
local Attributes = require 'krpc.attributes'
local Service = require 'krpc.service'
local Types = require 'krpc.types'
local encoder = require 'krpc.encoder'
local decoder = require 'krpc.decoder'
local protobuf = require 'krpc.protobuf'
local Connection = require 'krpc.connection'
local LocalConnection = require 'krpc.localconnection'

local to_snake_case = Service.to_snake_case

assert(pb.load(require 'conformance.testee_schema'))

-- Each message is its JSON form on a line of its own
local function write(message)
  io.stdout:write(protojson.encode('krpc.conformance.Output', message), '\n')
  io.stdout:flush()
end

-- The next command from the driver, or nil at the end of the input
local function read_command()
  local line = io.stdin:read('l')
  if line == nil then
    return nil
  end
  -- A carriage return before the newline, written in text mode
  return protojson.decode('krpc.conformance.Command', (line:gsub('\r$', '')))
end

-- The open connections, by the name the driver gives them, and the one the current command
-- runs on. The default connection has the empty name.
local connections = {}
local conn = nil
-- The objects the testee holds, by handle. Each object a call returns gets a new handle, so two
-- handles can refer to one server object.
local objects = {}
local next_handle = 1

-- An enum, struct or class type of a service
local function service_type(value)
  return conn[to_snake_case(value.service)][value.name]
end

local function from_value(value)
  local case = value.value
  if case == nil or case == 'null' then
    return Types.none
  end
  if case == 'object' then
    if (value.object.handle or 0) == 0 then
      return service_type(value.object)(value.object.id)
    end
    return objects[value.object.handle]
  elseif case == 'enum' then
    return service_type(value.enum)(value.enum.value or 0)
  elseif case == 'list' or case == 'tuple' then
    local result = List{}
    for i, x in ipairs(value[case].items or {}) do
      result[i] = from_value(x)
    end
    return result
  elseif case == 'set' then
    local result = Set{}
    for _, x in ipairs(value.set.items or {}) do
      result[from_value(x)] = true
    end
    return result
  elseif case == 'dictionary' then
    local result = Map{}
    for _, x in ipairs(value.dictionary.entries or {}) do
      result[from_value(x.key)] = from_value(x.value)
    end
    return result
  elseif case == 'struct' then
    local fields = {}
    for i, x in ipairs(value.struct.fields or {}) do
      fields[i] = from_value(x)
    end
    return service_type(value.struct)(table.unpack(fields, 1, #(value.struct.fields or {})))
  end
  return value[case]
end

local function to_value(value)
  if value == nil or rawequal(value, Types.none) then
    return { null = true }
  end
  local kind = type(value)
  if kind == 'boolean' then
    return { bool = value }
  elseif kind == 'string' then
    -- Bytes are a string too, so a string that is not UTF-8 goes as bytes
    if utf8.len(value) == nil then
      return { bytes = value }
    end
    return { string = value }
  elseif kind == 'number' then
    -- The driver compares numbers by value, whatever their type
    if math.type(value) == 'integer' then
      return { sint64 = value }
    end
    return { double = value }
  elseif kind == 'table' and value._object_id ~= nil then
    local handle = next_handle
    next_handle = next_handle + 1
    objects[handle] = value
    return { object = { handle = handle, id = value._object_id } }
  elseif Types.Enum:class_of(value) then
    return { enum = { value = value.value } }
  elseif Types.StructBase:class_of(value) then
    local fields = {}
    for i, name in ipairs(value._field_names) do
      fields[i] = to_value(value[name])
    end
    return { struct = { name = value._struct_name, fields = fields } }
  elseif Set:class_of(value) then
    local items = {}
    for _, x in ipairs(Set.values(value)) do
      items[#items + 1] = to_value(x)
    end
    return { set = { items = items } }
  elseif Map:class_of(value) then
    local entries = {}
    for k, v in pairs(value) do
      entries[#entries + 1] = { key = to_value(k), value = to_value(v) }
    end
    return { dictionary = { entries = entries } }
  elseif List:class_of(value) then
    -- A tuple is a List as well
    local items = {}
    for i, x in ipairs(value) do
      items[i] = to_value(x)
    end
    return { list = { items = items } }
  end
  error('No conformance value for a ' .. kind, 0)
end

-- The public names of an object, including those on the class tables it inherits from, without
-- the class library's own
local CLASS_LIB_MEMBERS = { is_a = true, class_of = true, cast = true, catch = true, lineinfo = true }
local function public_members(obj)
  local members = {}
  local function add(t)
    for k, _ in pairs(t) do
      if type(k) == 'string' and k:sub(1, 1) ~= '_' and not CLASS_LIB_MEMBERS[k] then
        members[k] = true
      end
    end
  end
  add(obj)
  local cls = getmetatable(obj)
  if rawget(obj, '_init') ~= nil then
    cls = obj
  end
  while type(cls) == 'table' do
    add(cls)
    cls = rawget(cls, '_base')
  end
  return members
end

-- The comma separated codec type names in a list of them, which may nest
local function split_codec_types(names)
  local result = {}
  local depth = 0
  local start = 1
  for i = 1, #names do
    local char = names:sub(i, i)
    if char == '(' then
      depth = depth + 1
    elseif char == ')' then
      depth = depth - 1
    elseif char == ',' and depth == 0 then
      result[#result + 1] = names:sub(start, i - 1)
      start = i + 1
    end
  end
  result[#result + 1] = names:sub(start)
  return result
end

-- The client's type for a codec type name, such as "list(nullable uint32)"
local function codec_type(name)
  local types = conn._types
  if name:sub(1, 9) == 'nullable ' then
    return types:nullable(codec_type(name:sub(10)))
  end
  local kind, rest = name:match('^(%a+)%((.*)%)$')
  if kind ~= nil then
    local items = {}
    for i, x in ipairs(split_codec_types(rest)) do
      items[i] = codec_type(x)
    end
    if kind == 'list' then
      return types:list_type(items[1])
    elseif kind == 'set' then
      return types:set_type(items[1])
    elseif kind == 'dictionary' then
      return types:dictionary_type(items[1], items[2])
    end
    return types:tuple_type(items)
  end
  if name == 'TestClass' then
    return types:class_type('TestService', name)
  elseif name == 'TestEnum' then
    return types:enumeration_type('TestService', name)
  elseif name:sub(1, 4) == 'Test' then
    return types:struct_type('TestService', name)
  end
  return types[name .. '_type'](types)
end

local function members(command)
  local names = {}
  if (command.service or '') == '' then
    -- The services are the connection's own public keys
    for k, v in pairs(conn) do
      if type(k) == 'string' and k:sub(1, 1) ~= '_' and type(v) == 'table' then
        names[#names + 1] = k
      end
    end
  else
    local obj = conn[to_snake_case(command.service)]
    if (command.name or '') ~= '' then
      obj = obj[command.name]
    end
    for k, _ in pairs(public_members(obj)) do
      names[#names + 1] = k
    end
  end
  table.sort(names)
  local items = {}
  for i, name in ipairs(names) do
    items[i] = { string = name }
  end
  return { list = { items = items } }
end

-- Call a procedure, given the name the server gives it
local function invoke(command)
  -- An instance member is called through its instance, so only the other members need the
  -- service object
  local function service()
    local result = conn[to_snake_case(command.service)]
    if result == nil then
      error('The testee cannot call service ' .. command.service, 0)
    end
    return result
  end
  local name = command.procedure
  local args = {}
  for i, arg in ipairs(command.arguments or {}) do
    args[i] = from_value(arg)
  end
  if Attributes.is_a_class_member(name) then
    local member = to_snake_case(Attributes.get_class_member_name(name))
    if Attributes.is_a_class_static_method(name) then
      if command.through ~= nil then
        return from_value(command.through)[member](table.unpack(args))
      end
      return service()[Attributes.get_class_name(name)][member](table.unpack(args))
    elseif Attributes.is_a_class_property_getter(name) then
      return args[1][member]
    elseif Attributes.is_a_class_property_setter(name) then
      args[1][member] = args[2]
      return nil
    end
    local obj = args[1]
    return obj[member](obj, table.unpack(args, 2))
  elseif Attributes.is_a_property_getter(name) then
    return service()[to_snake_case(Attributes.get_property_name(name))]
  elseif Attributes.is_a_property_setter(name) then
    service()[to_snake_case(Attributes.get_property_name(name))] = args[1]
    return nil
  end
  return service()[to_snake_case(name)](table.unpack(args))
end

-- Connecting the default connection closes every connection, and connecting a named one
-- closes only the connection of that name
local function connect(command, connection)
  if connection == '' then
    for _, x in pairs(connections) do
      x:close()
    end
    connections = {}
    objects = {}
  elseif connections[connection] ~= nil then
    connections[connection]:close()
  end
  connections[connection] = nil
  conn = nil
  local handshake_timeout = nil
  if command._handshake_timeout ~= nil then
    handshake_timeout = command.handshake_timeout
  end
  local name = command.name
  if command.protocol == 'localsocket' then
    conn = krpc.connect_local(name, command.path ~= '' and command.path or nil, handshake_timeout)
  elseif command.protocol == 'tcpip' then
    local address = command.address ~= '' and command.address or nil
    local port = command.port > 0 and command.port or nil
    conn = krpc.connect(name, address, port, handshake_timeout)
  else
    error('Unknown protocol ' .. command.protocol, 0)
  end
  connections[connection] = conn
end

local function connected()
  if conn == nil then
    error('Not connected', 0)
  end
  return conn
end

-- The krpc.schema message a procedure returns, or nil for a procedure that returns another type
local function result_message_type(command)
  for _, service in ipairs(conn.krpc:get_services().services) do
    if service.name == command.service then
      for _, procedure in ipairs(service.procedures) do
        if procedure.name == command.procedure and procedure.return_type ~= nil then
          local typ = conn._types:as_type(procedure.return_type)
          if typ:is_a(Types.MessageType) then
            return typ.message_type
          end
        end
      end
    end
  end
  return nil
end

local function dispatch(command)
  local case = command.command
  if case == 'connect' then
    connect(command.connect, command.connection or '')
    return {}
  elseif case == 'close' then
    connected():close()
    return {}
  elseif case == 'fail_write' then
    local message = command.fail_write.message
    -- Replaces each connection class's send for one write, which fails as a failed socket
    -- write does. A subclass holds a copy of its base's methods, so each class gets one.
    local classes = { Connection, LocalConnection }
    local sends = { Connection.send, LocalConnection.send }
    local function fail()
      for i, class in ipairs(classes) do
        class.send = sends[i]
      end
      error('Socket error: ' .. message, 0)
    end
    for _, class in ipairs(classes) do
      class.send = fail
    end
    return {}
  elseif case == 'invoke' then
    connected()
    local result = invoke(command.invoke)
    if result == nil then
      return {}
    end
    -- A decoded message is a table with no class
    if type(result) == 'table' and getmetatable(result) == nil then
      local message_type = result_message_type(command.invoke)
      if message_type ~= nil then
        return { message = protobuf.encode(message_type, result) }
      end
    end
    return to_value(result)
  elseif case == 'hash' then
    -- Lua has no hash of a table's value
    return { null = true }
  elseif case == 'object_id' then
    return { uint64 = krpc.get_object_id(from_value(command.object_id.a)) }
  elseif case == 'encode' then
    connected()
    local typ = codec_type(command.encode.type)
    return { bytes = encoder.encode(from_value(command.encode.value), typ) }
  elseif case == 'decode' then
    connected()
    return to_value(decoder.decode(command.decode.data or '', codec_type(command.decode.type)))
  elseif case == 'members' then
    connected()
    return members(command.members)
  elseif case == 'order' then
    local a = from_value(command.order.a)
    local b = from_value(command.order.b)
    local sign = (b < a and 1 or 0) - (a < b and 1 or 0)
    if (a <= b) ~= (sign <= 0) or (a >= b) ~= (sign >= 0) or (a == b) ~= (sign == 0) or
       (a ~= b) ~= (sign ~= 0) then
      error('The comparison operators disagree on the order', 0)
    end
    return { sint32 = sign }
  elseif case == 'compare' then
    return { bool = from_value(command.compare.a) == from_value(command.compare.b) }
  end
  error('Unknown command ' .. tostring(case), 0)
end

local function run(command)
  conn = connections[command.connection or '']
  local ok, result = pcall(dispatch, command)
  local reply = { id = command.id }
  if ok then
    reply.value = result
  else
    -- A failed connect, and a call once the connection has ended, fail with the connection
    local kind = 'RPC'
    if command.command == 'connect' or (conn ~= nil and conn._error ~= nil) then
      kind = 'CONNECTION'
    end
    reply.error = { kind = kind, message = tostring(result) }
  end
  write({ reply = reply })
end

write({ hello = {
  client = 'lua',
  profiles = { 'core', 'dynamic', 'defaults', 'connections', 'ordering' },
  protocols = { 'tcpip', 'localsocket' },
} })

while true do
  local command = read_command()
  if command == nil then
    break
  end
  run(command)
end
for _, x in pairs(connections) do
  x:close()
end
