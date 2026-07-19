local luaunit = require 'luaunit'
local ServerTest = require 'krpc.test.servertest'
local class = require 'pl.class'
local seq = require 'pl.seq'
local stringx = require 'pl.stringx'
local tablex = require 'pl.tablex'
local List = require 'pl.List'
local Map = require 'pl.Map'
local Set = require 'pl.Set'
local krpc = require 'krpc.init'
local Types = require 'krpc.types'

local TestClient = class(ServerTest)

function TestClient:test_version()
  status = self.conn.krpc:get_status()
  luaunit.assertStrMatches(status.version, '%d+.%d+.%d+')
end

function TestClient:test_wrong_rpc_port()
  luaunit.assertError(
    krpc.connect, 'LuaClientTestWrongRpcPort',
    'localhost', self.get_rpc_port() ^ self.get_stream_port(), self.get_stream_port())
end

function TestClient:test_wrong_rpc_server()
  luaunit.assertErrorMsgContains(
    'Connection request was for the rpc server, but this is the stream server. ' ..
    'Did you connect to the wrong port number?',
    krpc.connect, 'LuaClientTestWrongRpcServer',
    'localhost', self.get_stream_port(), self.get_stream_port())
end

function TestClient:test_value_parameters()
  luaunit.assertEquals('3.14159', self.conn.test_service.float_to_string(3.14159))
  luaunit.assertEquals('3.14159', self.conn.test_service.double_to_string(3.14159))
  luaunit.assertEquals('42', self.conn.test_service.int32_to_string(42))
  luaunit.assertEquals('123456789000', self.conn.test_service.int64_to_string(123456789000))
  luaunit.assertEquals('True', self.conn.test_service.bool_to_string(true))
  luaunit.assertEquals('False', self.conn.test_service.bool_to_string(false))
  luaunit.assertEquals(12345, self.conn.test_service.string_to_int32('12345'))
  luaunit.assertEquals('deadbeef', self.conn.test_service.bytes_to_hex_string('\222\173\190\239'))
end

function TestClient:test_multiple_value_parameters()
  luaunit.assertEquals('3.14159', self.conn.test_service.add_multiple_values(0.14159, 1, 2))
end

function TestClient:test_auto_value_type_conversion()
  luaunit.assertEquals('42', self.conn.test_service.float_to_string(42))
  luaunit.assertEquals('6', self.conn.test_service.add_multiple_values(1, 2, 3))
  luaunit.assertErrorMsgContains(
    'TestService.FloatToString() argument 1 must be a number, got a string',
    self.conn.test_service.float_to_string, '42')
end

function TestClient:test_incorrect_parameter_type()
  luaunit.assertErrorMsgContains(
    'TestService.FloatToString() argument 1 must be a number, got a string',
    self.conn.test_service.float_to_string, 'foo')
  luaunit.assertErrorMsgContains(
    'TestService.AddMultipleValues() argument 2 must be a number, got a string',
    self.conn.test_service.add_multiple_values, 0.14159, 'foo', 2)
end

function TestClient:test_properties()
  self.conn.test_service:set_string_property('bar')
  luaunit.assertEquals('bar', self.conn.test_service:get_string_property())

  self.conn.test_service.string_property = 'foo'
  luaunit.assertEquals('foo', self.conn.test_service.string_property)

  luaunit.assertEquals('foo', self.conn.test_service.string_property_private_set)
  self.conn.test_service.string_property_private_get = 'foo'
  obj = self.conn.test_service.create_test_object('bar')
  self.conn.test_service.object_property = obj
  luaunit.assertEquals(obj, self.conn.test_service.object_property)
end

function TestClient:test_class_as_return_value()
  local obj = self.conn.test_service.create_test_object('jeb')
  luaunit.assertTrue(obj:is_a(self.conn.test_service.TestClass))
end

function TestClient:test_class_none_value()
  self.conn.test_service.echo_test_object(Types.none)
  luaunit.assertEquals(self.conn.test_service.echo_test_object(Types.none), Types.none)
  obj = self.conn.test_service.create_test_object('bob')
  luaunit.assertEquals('bobnull', obj:object_to_string(Types.none))
  self.conn.test_service.object_property = Types.none
  luaunit.assertEquals(self.conn.test_service.object_property, Types.none)
end

function TestClient:test_class_methods()
  local obj = self.conn.test_service.create_test_object('bob')
  luaunit.assertEquals('value=bob', obj:get_value())
  luaunit.assertEquals('bob3.14159', obj:float_to_string(3.14159))
  local obj2 = self.conn.test_service.create_test_object('bill')
  luaunit.assertEquals('bobbill', obj:object_to_string(obj2))
end

function TestClient:test_class_static_methods()
  luaunit.assertEquals('jeb', self.conn.test_service.TestClass.static_method())
  luaunit.assertEquals('jebbobbill', self.conn.test_service.TestClass.static_method('bob', 'bill'))
end

function TestClient:test_class_properties()
  local obj = self.conn.test_service.create_test_object('jeb')
  obj:set_int_property(0)
  luaunit.assertEquals(0, obj:get_int_property())
  obj.int_property = 0
  luaunit.assertEquals(0, obj.int_property)
  obj.int_property = 42
  luaunit.assertEquals(42, obj.int_property)
  local obj2 = self.conn.test_service.create_test_object('kermin')
  obj.object_property = obj2
  luaunit.assertEquals(obj2._object_id, obj.object_property._object_id)
  obj.string_property_private_get = 'bob'
  luaunit.assertEquals('bob', obj.string_property_private_set)
end

function TestClient:test_optional_arguments()
  luaunit.assertEquals('jebfoobarnull', self.conn.test_service.optional_arguments('jeb'))
  luaunit.assertEquals('jebbobbillnull', self.conn.test_service.optional_arguments('jeb', 'bob', 'bill'))
  local obj = self.conn.test_service.create_test_object('kermin')
  luaunit.assertEquals('jebbobbillkermin', self.conn.test_service.optional_arguments('jeb', 'bob', 'bill', obj))
end

function TestClient:test_blocking_procedure()
  luaunit.assertEquals(0, self.conn.test_service.blocking_procedure(0,0))
  luaunit.assertEquals(1, self.conn.test_service.blocking_procedure(1,0))
  luaunit.assertEquals(1+2, self.conn.test_service.blocking_procedure(2))
  local total = 0
  for i=1,42 do
    total = total + i
  end
  luaunit.assertEquals(total, self.conn.test_service.blocking_procedure(42))
end

function TestClient:test_too_many_arguments()
  -- FIXME: passing too many arguments isn't a bug in Lua!
  --luaunit.assertError(self.conn.test_service.optional_arguments, '1', '2', '3', '4', '5')
  --obj = self.conn.test_service.create_test_object('jeb')
  --luaunit.assertError(obj.optional_arguments, '1', '2', '3', '4', '5')
end

local function filter_private(xs)
  return seq.copy(seq.filter(
    xs:iter(),
    function (x) return not stringx.startswith(x, '_') end)
  )
end

function TestClient:test_client_members()
  luaunit.assertEquals(
    Set{'krpc', 'test_service'},
    Set(filter_private(tablex.keys(self.conn)))
  )
end

function TestClient:test_enumerations()
  local enum = self.conn.test_service.TestEnum

  luaunit.assertEquals(enum.value_b, self.conn.test_service.enum_return())
  luaunit.assertEquals(enum.value_a, self.conn.test_service.enum_echo(enum.value_a))
  luaunit.assertEquals(enum.value_b, self.conn.test_service.enum_echo(enum.value_b))
  luaunit.assertEquals(enum.value_c, self.conn.test_service.enum_echo(enum.value_c))

  luaunit.assertEquals(enum.value_a, self.conn.test_service.enum_default_arg(enum.value_a))
  luaunit.assertEquals(enum.value_c, self.conn.test_service.enum_default_arg())
  luaunit.assertEquals(enum.value_b, self.conn.test_service.enum_default_arg(enum.value_b))
end

function TestClient:test_invalid_enum()
  luaunit.assertError(ValueError, self.conn.test_service.TestEnum, 9999)
end

function TestClient:test_collections()
  luaunit.assertEquals(List{}, self.conn.test_service.increment_list(List{}))
  luaunit.assertEquals(List{1,2,3}, self.conn.test_service.increment_list(List{0,1,2}))
  luaunit.assertEquals(Map{}, self.conn.test_service.increment_dictionary(Map{}))
  luaunit.assertEquals(Map{a=1, b=2, c=3}, self.conn.test_service.increment_dictionary(Map{a=0, b=1, c=2}))
  luaunit.assertEquals(Set{}, self.conn.test_service.increment_set(Set{}))
  luaunit.assertEquals(Set{1,2,3}, self.conn.test_service.increment_set(Set{0,1,2}))
  luaunit.assertEquals(List{2,3}, self.conn.test_service.increment_tuple(List{1,2}))
  luaunit.assertError(self.conn.test_service.increment_list, Types.none)
  luaunit.assertError(self.conn.test_service.increment_set, Types.none)
  luaunit.assertError(self.conn.test_service.increment_dictionary, Types.none)
end

function TestClient:test_nested_collections()
  luaunit.assertEquals(Map{}, self.conn.test_service.increment_nested_collection(Map{}))
  luaunit.assertEquals(Map{a=List{1, 2}, b=List{}, c=List{3}},
                       self.conn.test_service.increment_nested_collection(Map{a=List{0, 1}, b=List{}, c=List{2}}))
end

function TestClient:test_collections_of_objects()
  local l = self.conn.test_service.add_to_object_list(List{}, "jeb")
  luaunit.assertEquals(1, l:len())
  luaunit.assertEquals("value=jeb", l[1]:get_value())
  local l = self.conn.test_service.add_to_object_list(l, "bob")
  luaunit.assertEquals(2, l:len())
  luaunit.assertEquals("value=jeb", l[1]:get_value())
  luaunit.assertEquals("value=bob", l[2]:get_value())
end

function TestClient:test_collections_default_values()
  luaunit.assertEquals(List{1, false}, self.conn.test_service.tuple_default())
  luaunit.assertEquals(List{1, 2, 3}, self.conn.test_service.list_default())
  luaunit.assertEquals(Set{1, 2, 3}, self.conn.test_service.set_default())
  local m = Map{}
  m:set(1, false)
  m:set(2, true)
  luaunit.assertEquals(m, self.conn.test_service.dictionary_default())
end

function TestClient:test_invalid_operation_exception()
  luaunit.assertErrorMsgContains(
    'KRPC.InvalidOperationException: Invalid operation',
    self.conn.test_service.throw_invalid_operation_exception)
end

function TestClient:test_argument_exception()
  luaunit.assertErrorMsgContains(
    'KRPC.ArgumentException: Invalid argument',
    self.conn.test_service.throw_argument_exception)
end

function TestClient:test_argument_null_exception()
  luaunit.assertErrorMsgContains(
    'KRPC.ArgumentNullException: Value cannot be null',
    self.conn.test_service.throw_argument_null_exception, "")
end

function TestClient:test_argument_out_of_range_exception()
  luaunit.assertErrorMsgContains(
    'KRPC.ArgumentOutOfRangeException: Specified argument was out of the range of valid values.',
    self.conn.test_service.throw_argument_out_of_range_exception, 0)
end

function TestClient:test_custom_exception()
  luaunit.assertErrorMsgContains(
    'TestService.CustomException: A custom kRPC exception',
    self.conn.test_service.throw_custom_exception)
end

-- Collect the public member names of an object: its own string keys, plus those
-- of class tables reachable through the metatable (penlight class instances have
-- their class table as metatable, with parent classes linked through _base).
-- Service members live on per-service class tables, so the instance's own keys
-- are not enough. Penlight's class machinery is excluded, along with
-- underscore-prefixed private members.
local CLASS_LIB_MEMBERS = Set{'is_a', 'class_of', 'cast', 'catch', 'lineinfo'}
local function public_members(obj)
  local members = Set{}
  local function add(t)
    for k,_ in pairs(t) do
      if type(k) == 'string' and k:sub(1,1) ~= '_' and not CLASS_LIB_MEMBERS[k] then
        members = members + Set{k}
      end
    end
  end
  add(obj)
  local cls = getmetatable(obj)
  if rawget(obj, '_init') ~= nil then
    cls = obj  -- obj is itself a class table; walk its parents
  end
  while type(cls) == 'table' do
    add(cls)
    cls = rawget(cls, '_base')
  end
  return members
end

function TestClient:test_krpc_service_members()
  local members = Set.values(public_members(self.conn.krpc))
  table.sort(members)
  luaunit.assertEquals(
    members,
    {'Expression',
     'GameScene',
     'Type',
     'add_event',
     'add_stream',
     'get_client_id',
     'get_client_name',
     'get_clients',
     'get_current_game_scene',
     'get_game_scene',
     'get_paused',
     'get_services',
     'get_status',
     'remove_stream',
     'set_game_scene',
     'set_paused',
     'set_stream_rate',
     'start_stream'})
end

function TestClient:test_test_service_service_members()
  local members = Set.values(public_members(self.conn.test_service))
  table.sort(members)
  luaunit.assertEquals(
    members,
    {'DeprecatedClass',
     'DeprecatedEnum',
     'TestClass',
     'TestEnum',
     'add_multiple_values',
     'add_to_object_list',
     'blocking_procedure',
     'bool_to_string',
     'bytes_to_hex_string',
     'counter',
     'create_test_object',
     'deprecated_procedure',
     'deprecated_procedure_no_message',
     'dictionary_default',
     'double_to_string',
     'echo_test_object',
     'enum_default_arg',
     'enum_echo',
     'enum_return',
     'float_to_string',
     'get_deprecated_property',
     'get_object_property',
     'get_string_property',
     'get_string_property_private_set',
     'increment_dictionary',
     'increment_list',
     'increment_nested_collection',
     'increment_set',
     'increment_tuple',
     'int32_to_string',
     'int64_to_string',
     'list_default',
     'on_timer',
     'on_timer_using_lambda',
     'optional_arguments',
     'reset_custom_exception_later',
     'reset_invalid_operation_exception_later',
     'return_null_when_not_allowed',
     'set_default',
     'set_deprecated_property',
     'set_object_property',
     'set_string_property',
     'set_string_property_private_get',
     'string_to_int32',
     'throw_argument_exception',
     'throw_argument_null_exception',
     'throw_argument_out_of_range_exception',
     'throw_custom_exception',
     'throw_custom_exception_later',
     'throw_invalid_operation_exception',
     'throw_invalid_operation_exception_later',
     'tuple_default'})
end

function TestClient:test_test_service_test_class_members()
  local members = Set.values(public_members(self.conn.test_service.TestClass))
  table.sort(members)
  luaunit.assertEquals(
    members,
    {'float_to_string',
     'get_int_property',
     'get_object_property',
     'get_string_property_private_set',
     'get_value',
     'object_to_string',
     'optional_arguments',
     'set_int_property',
     'set_object_property',
     'set_string_property_private_get',
     'static_method'})
end

function TestClient:test_test_service_enum_members()
  local members = Set.values(public_members(self.conn.test_service.TestEnum))
  table.sort(members)
  luaunit.assertEquals(
    members,
    {'value_a',
     'value_b',
     'value_c'})
end

function TestClient:test_test_service_enum_values()
  luaunit.assertEquals(0, self.conn.test_service.TestEnum.value_a.value)
  luaunit.assertEquals(1, self.conn.test_service.TestEnum.value_b.value)
  luaunit.assertEquals(2, self.conn.test_service.TestEnum.value_c.value)
end

function TestClient:test_line_endings()
  local strings = {
    'foo\nbar',
    'foo\rbar',
    'foo\n\rbar',
    'foo\r\nbar',
    'foo\16bar',
    'foo\19bar',
    'foo\16\19bar',
    'foo\19\16bar'
  }
  for _,s in ipairs(strings) do
    self.conn.test_service.string_property = s
    luaunit.assertEquals(s, self.conn.test_service.string_property)
  end
end

function TestClient:test_types_from_different_connections()
  local conn1 = self:connect()
  local conn2 = self:connect()
  luaunit.assertFalse(conn1.test_service.TestClass == conn2.test_service.TestClass)
  local obj2 = conn2.test_service.TestClass(0)
  local obj1 = conn1._types:coerce_to(obj2, conn1._types:class_type('TestService', 'TestClass'))
  luaunit.assertEquals(obj1, obj2)
  luaunit.assertTrue(conn1.test_service.TestClass:class_of(obj1))
  luaunit.assertTrue(conn2.test_service.TestClass:class_of(obj2))
end

return TestClient
