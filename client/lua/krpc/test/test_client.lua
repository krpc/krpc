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

function TestClient:test_current_game_scene()
  luaunit.assertEquals(self.conn.krpc.GameScene.space_center, self.conn.krpc:get_current_game_scene())
end

function TestClient:test_error()
  luaunit.assertErrorMsgContains('Invalid argument', self.conn.test_service.throw_argument_exception)
  luaunit.assertErrorMsgContains('Invalid operation', self.conn.test_service.throw_invalid_operation_exception)
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
end

function TestClient:test_optional_arguments()
  luaunit.assertEquals('jebfoobarbaz', self.conn.test_service.optional_arguments('jeb'))
  luaunit.assertEquals('jebbobbillbaz', self.conn.test_service.optional_arguments('jeb', 'bob', 'bill'))
end

function TestClient:test_named_parameters()
  -- FIXME: Lua doens't support this. Have to pass arguments as a single table
  --self.conn.test_service.optional_arguments(x='1', y='2', z='3', another_parameter='4')
  --self.assertEqual('1234', self.conn.test_service.optional_arguments(x='1', y='2', z='3', another_parameter='4'))
  --self.assertEqual('2413', self.conn.test_service.optional_arguments(z='1', x='2', another_parameter='3', y='4'))
  --self.assertEqual('1243', self.conn.test_service.optional_arguments('1', '2', another_parameter='3', z='4'))
  --self.assertEqual('123baz', self.conn.test_service.optional_arguments('1', '2', z='3'))
  --self.assertEqual('12bar3', self.conn.test_service.optional_arguments('1', '2', another_parameter='3'))
  --self.assertRaises(TypeError, self.conn.test_service.optional_arguments, '1', '2', '3', '4', another_parameter='5')
  --self.assertRaises(TypeError, self.conn.test_service.optional_arguments, '1', '2', '3', y='4')
  --self.assertRaises(TypeError, self.conn.test_service.optional_arguments, '1', foo='4')
  --
  --obj = self.conn.test_service.create_test_object('jeb')
  --self.assertEqual('1234', obj.optional_arguments(x='1', y='2', z='3', another_parameter='4'))
  --self.assertEqual('2413', obj.optional_arguments(z='1', x='2', another_parameter='3', y='4'))
  --self.assertEqual('1243', obj.optional_arguments('1', '2', another_parameter='3', z='4'))
  --self.assertEqual('123baz', obj.optional_arguments('1', '2', z='3'))
  --self.assertEqual('12bar3', obj.optional_arguments('1', '2', another_parameter='3'))
  --self.assertRaises(TypeError, obj.optional_arguments, '1', '2', '3', '4', another_parameter='5')
  --self.assertRaises(TypeError, obj.optional_arguments, '1', '2', '3', y='4')
  --self.assertRaises(TypeError, obj.optional_arguments, '1', foo='4')
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

function TestClient:test_enums()
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

-- FIXME: enable tests
--def test_client_members(self):
--    self.assertSetEqual(
--        set(['krpc', 'test_service', 'add_stream', 'stream', 'close']),
--        set(filter(lambda x: not x.startswith('_'), dir(self.conn))))
--
--def test_krpc_service_members(self):
--    self.assertSetEqual(
--        set(['get_services', 'get_status', 'add_stream', 'remove_stream']),
--        set(filter(lambda x: not x.startswith('_'), dir(self.conn.krpc))))
--
--def test_test_service_service_members(self):
--    self.assertSetEqual(
--        set([
--            'float_to_string',
--            'double_to_string',
--            'int32_to_string',
--            'int64_to_string',
--            'bool_to_string',
--            'string_to_int32',
--            'bytes_to_hex_string',
--            'add_multiple_values',
--
--            'string_property',
--
--            'string_property_private_get',
--
--            'string_property_private_set',
--
--            'create_test_object',
--            'echo_test_object',
--
--            'object_property',
--
--            'TestClass',
--
--            'optional_arguments',
--
--            'TestEnum',
--            'enum_return',
--            'enum_echo',
--            'enum_default_arg',
--
--            'blocking_procedure',
--
--            'increment_list',
--            'increment_dictionary',
--            'increment_set',
--            'increment_tuple',
--            'increment_nested_collection',
--            'add_to_object_list',
--
--            'counter',
--
--            'throw_argument_exception',
--            'throw_invalid_operation_exception'
--        ]),
--        set(filter(lambda x: not x.startswith('_'), dir(self.conn.test_service))))
--
--def test_test_service_test_class_members(self):
--    self.assertSetEqual(
--        set([
--            'get_value',
--            'float_to_string',
--            'object_to_string',
--
--            'int_property',
--
--            'object_property',
--
--            'optional_arguments',
--            'static_method'
--        ]),
--        set(filter(lambda x: not x.startswith('_'), dir(self.conn.test_service.TestClass))))

--def test_test_service_enum_members(self):
--    self.assertSetEqual(
--        set(['value_a','value_b','value_c']),
--        set(filter(lambda x: not x.startswith('_'), dir(self.conn.test_service.TestEnum))))
--    self.assertEqual (0, self.conn.test_service.TestEnum.value_a.value)
--    self.assertEqual (1, self.conn.test_service.TestEnum.value_b.value)
--    self.assertEqual (2, self.conn.test_service.TestEnum.value_c.value)

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
  local obj1 = conn1._types:coerce_to(obj2, conn1._types:as_type('Class(TestService.TestClass)'))
  luaunit.assertEquals(obj1, obj2)
  luaunit.assertTrue(conn1.test_service.TestClass:class_of(obj1))
  luaunit.assertTrue(conn2.test_service.TestClass:class_of(obj2))
end

return TestClient
