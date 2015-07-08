local luaunit = require 'luaunit'
local ServerTest = require 'krpc.test.servertest'
local class = require 'pl.class'
local stringx = require 'pl.stringx'
local krpc = require 'krpc.init'
local Types = require 'krpc.types'

local TestClient = class(ServerTest)

function TestClient:test_version()
  status = self.conn.krpc:get_status()
  local version = stringx.rstrip(io.open('../VERSION.txt', 'r'):read('*all'))
  luaunit.assertEquals(version, status.version)
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
  luaunit.assertEquals('deadbeef', self.conn.test_service.bytes_to_hex_string('\xde\xad\xbe\xef'))
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
  --obj = self.conn.test_service.create_test_object('bob')
  --luaunit.assertEquals('bobnull', obj.object_to_string(nil))
  --self.conn.test_service.object_property = nil
  --luaunit.assertEquals(self.conn.test_service.object_property, nil)
end

--def test_class_methods(self):
--    obj = self.conn.test_service.create_test_object('bob')
--    self.assertEqual('value=bob', obj.get_value())
--    self.assertEqual('bob3.14159', obj.float_to_string(3.14159))
--    obj2 = self.conn.test_service.create_test_object('bill')
--    self.assertEqual('bobbill', obj.object_to_string(obj2))

--def test_class_static_methods(self):
--    self.assertEqual('jeb', self.conn.test_service.TestClass.static_method())
--    self.assertEqual('jebbobbill', self.conn.test_service.TestClass.static_method('bob', 'bill'))

--def test_class_properties(self):
--    obj = self.conn.test_service.create_test_object('jeb')
--    obj.int_property = 0
--    self.assertEqual(0, obj.int_property)
--    obj.int_property = 42
--    self.assertEqual(42, obj.int_property)
--    obj2 = self.conn.test_service.create_test_object('kermin')
--    obj.object_property = obj2
--    self.assertEqual(obj2._object_id, obj.object_property._object_id)
--
function TestClient:test_optional_arguments()
  luaunit.assertEquals('jebfoobarbaz', self.conn.test_service.optional_arguments('jeb'))
  luaunit.assertEquals('jebbobbillbaz', self.conn.test_service.optional_arguments('jeb', 'bob', 'bill'))
end

--def test_named_parameters(self):
--    self.assertEqual('1234', self.conn.test_service.optional_arguments(x='1', y='2', z='3', another_parameter='4'))
--    self.assertEqual('2413', self.conn.test_service.optional_arguments(z='1', x='2', another_parameter='3', y='4'))
--    self.assertEqual('1243', self.conn.test_service.optional_arguments('1', '2', another_parameter='3', z='4'))
--    self.assertEqual('123baz', self.conn.test_service.optional_arguments('1', '2', z='3'))
--    self.assertEqual('12bar3', self.conn.test_service.optional_arguments('1', '2', another_parameter='3'))
--    self.assertRaises(TypeError, self.conn.test_service.optional_arguments, '1', '2', '3', '4', another_parameter='5')
--    self.assertRaises(TypeError, self.conn.test_service.optional_arguments, '1', '2', '3', y='4')
--    self.assertRaises(TypeError, self.conn.test_service.optional_arguments, '1', foo='4')
--
--    obj = self.conn.test_service.create_test_object('jeb')
--    self.assertEqual('1234', obj.optional_arguments(x='1', y='2', z='3', another_parameter='4'))
--    self.assertEqual('2413', obj.optional_arguments(z='1', x='2', another_parameter='3', y='4'))
--    self.assertEqual('1243', obj.optional_arguments('1', '2', another_parameter='3', z='4'))
--    self.assertEqual('123baz', obj.optional_arguments('1', '2', z='3'))
--    self.assertEqual('12bar3', obj.optional_arguments('1', '2', another_parameter='3'))
--    self.assertRaises(TypeError, obj.optional_arguments, '1', '2', '3', '4', another_parameter='5')
--    self.assertRaises(TypeError, obj.optional_arguments, '1', '2', '3', y='4')
--    self.assertRaises(TypeError, obj.optional_arguments, '1', foo='4')

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

--def test_client_members(self):
--    self.assertSetEqual(
--        set(['krpc', 'test_service']),
--        set(filter(lambda x: not x.startswith('_'), dir(self.conn))))

--def test_krpc_service_members(self):
--    self.assertSetEqual(
--        set(['get_services', 'get_status']),
--        set(filter(lambda x: not x.startswith('_'), dir(self.conn.krpc))))

--def test_protobuf_enums(self):
--    self.assertEqual(TestSchema.a, self.conn.test_service.enum_return())
--    self.assertEqual(TestSchema.a, self.conn.test_service.enum_echo(TestSchema.a))
--    self.assertEqual(TestSchema.b, self.conn.test_service.enum_echo(TestSchema.b))
--    self.assertEqual(TestSchema.c, self.conn.test_service.enum_echo(TestSchema.c))
--
--    self.assertEqual(TestSchema.a, self.conn.test_service.enum_default_arg(TestSchema.a))
--    self.assertEqual(TestSchema.c, self.conn.test_service.enum_default_arg())
--    self.assertEqual(TestSchema.b, self.conn.test_service.enum_default_arg(TestSchema.b))

--def test_enums(self):
--    enum = self.conn.test_service.CSharpEnum
--    self.assertEqual(enum.value_b, self.conn.test_service.c_sharp_enum_return())
--    self.assertEqual(enum.value_a, self.conn.test_service.c_sharp_enum_echo(enum.value_a))
--    self.assertEqual(enum.value_b, self.conn.test_service.c_sharp_enum_echo(enum.value_b))
--    self.assertEqual(enum.value_c, self.conn.test_service.c_sharp_enum_echo(enum.value_c))
--
--    self.assertEqual(enum.value_a, self.conn.test_service.c_sharp_enum_default_arg(enum.value_a))
--    self.assertEqual(enum.value_c, self.conn.test_service.c_sharp_enum_default_arg())
--    self.assertEqual(enum.value_b, self.conn.test_service.c_sharp_enum_default_arg(enum.value_b))
--
--def test_invalid_enum(self):
--    self.assertRaises(ValueError, self.conn.test_service.CSharpEnum, 9999)
--
--def test_collections(self):
--    self.assertEqual([], self.conn.test_service.increment_list([]))
--    self.assertEqual([1,2,3], self.conn.test_service.increment_list([0,1,2]))
--    self.assertEqual({}, self.conn.test_service.increment_dictionary({}))
--    self.assertEqual({'a': 1, 'b': 2, 'c': 3}, self.conn.test_service.increment_dictionary({'a': 0, 'b': 1, 'c': 2}))
--    self.assertEqual(set(), self.conn.test_service.increment_set(set()))
--    self.assertEqual(set([1,2,3]), self.conn.test_service.increment_set(set([0,1,2])))
--    self.assertEqual((2,3), self.conn.test_service.increment_tuple((1,2)))
--    self.assertRaises(TypeError, self.conn.test_service.increment_list, None)
--    self.assertRaises(TypeError, self.conn.test_service.increment_set, None)
--    self.assertRaises(TypeError, self.conn.test_service.increment_dictionary, None)
--
--def test_nested_collections(self):
--    self.assertEqual({}, self.conn.test_service.increment_nested_collection({}))
--    self.assertEqual({'a': [1, 2], 'b': [], 'c': [3]},
--                     self.conn.test_service.increment_nested_collection({'a': [0, 1], 'b': [], 'c': [2]}))
--
--def test_collections_of_objects(self):
--    l = self.conn.test_service.add_to_object_list([], "jeb")
--    self.assertEqual(1, len(l))
--    self.assertEqual("value=jeb", l[0].get_value())
--    l = self.conn.test_service.add_to_object_list(l, "bob")
--    self.assertEqual(2, len(l))
--    self.assertEqual("value=jeb", l[0].get_value())
--    self.assertEqual("value=bob", l[1].get_value())
--
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
--            'enum_return',
--            'enum_echo',
--            'enum_default_arg',
--            'CSharpEnum',
--            'c_sharp_enum_return',
--            'c_sharp_enum_echo',
--            'c_sharp_enum_default_arg',
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
--        set(filter(lambda x: not x.startswith('_'), dir(self.conn.test_service.CSharpEnum))))
--    self.assertEqual (0, self.conn.test_service.CSharpEnum.value_a.value)
--    self.assertEqual (1, self.conn.test_service.CSharpEnum.value_b.value)
--    self.assertEqual (2, self.conn.test_service.CSharpEnum.value_c.value)

function TestClient:test_line_endings()
  local strings = {
    'foo\nbar',
    'foo\rbar',
    'foo\n\rbar',
    'foo\r\nbar',
    'foo\x10bar',
    'foo\x13bar',
    'foo\x10\x13bar',
    'foo\x13\x10bar'
  }
  for _,s in ipairs(strings) do
    self.conn.test_service.string_property = s
    luaunit.assertEquals(s, self.conn.test_service.string_property)
  end
end

--def test_types_from_different_connections(self):
--    conn1 = self.connect()
--    conn2 = self.connect()
--    self.assertNotEqual(conn1.test_service.TestClass, conn2.test_service.TestClass)
--    obj2 = conn2.test_service.TestClass(0)
--    obj1 = conn1._types.coerce_to(obj2, conn1._types.as_type('Class(TestService.TestClass)'))
--    self.assertEqual(obj1, obj2)
--    self.assertNotEqual(type(obj1), type(obj2))
--    self.assertEqual(type(obj1), conn1.test_service.TestClass)
--    self.assertEqual(type(obj2), conn2.test_service.TestClass)

return TestClient
