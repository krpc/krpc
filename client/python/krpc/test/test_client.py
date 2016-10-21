import unittest
import threading
import krpc
from krpc.test.servertestcase import ServerTestCase


class TestClient(ServerTestCase, unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        super(TestClient, cls).setUpClass()

    def test_get_status(self):
        status = self.conn.krpc.get_status()
        self.assertRegexpMatches(status.version, r'^[0-9]+\.[0-9]+\.[0-9]+$')
        self.assertGreater(status.bytes_read, 0)

    def test_current_game_scene(self):
        self.assertEqual(self.conn.krpc.GameScene.space_center, self.conn.krpc.current_game_scene)

    def test_error(self):
        with self.assertRaises(krpc.client.RPCError) as cm:
            self.conn.test_service.throw_argument_exception()
        self.assertTrue('Invalid argument' in str(cm.exception))

        with self.assertRaises(krpc.client.RPCError) as cm:
            self.conn.test_service.throw_invalid_operation_exception()
        self.assertTrue('Invalid operation' in str(cm.exception))

    def test_value_parameters(self):
        self.assertEqual('3.14159', self.conn.test_service.float_to_string(float(3.14159)))
        self.assertEqual('3.14159', self.conn.test_service.double_to_string(float(3.14159)))
        self.assertEqual('42', self.conn.test_service.int32_to_string(42))
        self.assertEqual('123456789000', self.conn.test_service.int64_to_string(123456789000L))
        self.assertEqual('True', self.conn.test_service.bool_to_string(True))
        self.assertEqual('False', self.conn.test_service.bool_to_string(False))
        self.assertEqual(12345, self.conn.test_service.string_to_int32('12345'))
        self.assertEqual('deadbeef', self.conn.test_service.bytes_to_hex_string(b'\xde\xad\xbe\xef'))

    def test_multiple_value_parameters(self):
        self.assertEqual('3.14159', self.conn.test_service.add_multiple_values(0.14159, 1, 2))

    def test_auto_value_type_conversion(self):
        self.assertEqual('42', self.conn.test_service.float_to_string(42))
        self.assertEqual('42', self.conn.test_service.float_to_string(42L))
        self.assertEqual('6', self.conn.test_service.add_multiple_values(1L, 2L, 3L))
        self.assertRaises(TypeError, self.conn.test_service.float_to_string, '42')

    def test_incorrect_parameter_type(self):
        self.assertRaises(TypeError, self.conn.test_service.float_to_string, 'foo')
        self.assertRaises(TypeError, self.conn.test_service.add_multiple_values, 0.14159, 'foo', 2)

    def test_properties(self):
        self.conn.test_service.string_property = 'foo'
        self.assertEqual('foo', self.conn.test_service.string_property)
        self.assertEqual('foo', self.conn.test_service.string_property_private_set)
        self.conn.test_service.string_property_private_get = 'foo'
        obj = self.conn.test_service.create_test_object('bar')
        self.conn.test_service.object_property = obj
        self.assertEqual(obj, self.conn.test_service.object_property)

    def test_class_as_return_value(self):
        obj = self.conn.test_service.create_test_object('jeb')
        self.assertEqual('TestClass', type(obj).__name__)

    def test_class_none_value(self):
        self.assertIsNone(self.conn.test_service.echo_test_object(None))
        obj = self.conn.test_service.create_test_object('bob')
        self.assertEqual('bobnull', obj.object_to_string(None))
        self.conn.test_service.object_property = None
        self.assertIsNone(self.conn.test_service.object_property)

    def test_class_methods(self):
        obj = self.conn.test_service.create_test_object('bob')
        self.assertEqual('value=bob', obj.get_value())
        self.assertEqual('bob3.14159', obj.float_to_string(3.14159))
        obj2 = self.conn.test_service.create_test_object('bill')
        self.assertEqual('bobbill', obj.object_to_string(obj2))

    def test_class_static_methods(self):
        self.assertEqual('jeb', self.conn.test_service.TestClass.static_method())
        self.assertEqual('jebbobbill', self.conn.test_service.TestClass.static_method('bob', 'bill'))

    def test_class_properties(self):
        obj = self.conn.test_service.create_test_object('jeb')
        obj.int_property = 0
        self.assertEqual(0, obj.int_property)
        obj.int_property = 42
        self.assertEqual(42, obj.int_property)
        obj2 = self.conn.test_service.create_test_object('kermin')
        obj.object_property = obj2
        self.assertEqual(obj2._object_id, obj.object_property._object_id)

    def test_optional_arguments(self):
        self.assertEqual('jebfoobarbaz', self.conn.test_service.optional_arguments('jeb'))
        self.assertEqual('jebbobbillbaz', self.conn.test_service.optional_arguments('jeb', 'bob', 'bill'))

    def test_named_parameters(self):
        self.assertEqual('1234',
                         self.conn.test_service.optional_arguments(x='1', y='2', z='3', another_parameter='4'))
        self.assertEqual('2413',
                         self.conn.test_service.optional_arguments(z='1', x='2', another_parameter='3', y='4'))
        self.assertEqual('1243',
                         self.conn.test_service.optional_arguments('1', '2', another_parameter='3', z='4'))
        self.assertEqual('123baz',
                         self.conn.test_service.optional_arguments('1', '2', z='3'))
        self.assertEqual('12bar3',
                         self.conn.test_service.optional_arguments('1', '2', another_parameter='3'))
        self.assertRaises(TypeError,
                          self.conn.test_service.optional_arguments, '1', '2', '3', '4', another_parameter='5')
        self.assertRaises(TypeError,
                          self.conn.test_service.optional_arguments, '1', '2', '3', y='4')
        self.assertRaises(TypeError,
                          self.conn.test_service.optional_arguments, '1', foo='4')

        obj = self.conn.test_service.create_test_object('jeb')
        self.assertEqual('1234', obj.optional_arguments(x='1', y='2', z='3', another_parameter='4'))
        self.assertEqual('2413', obj.optional_arguments(z='1', x='2', another_parameter='3', y='4'))
        self.assertEqual('1243', obj.optional_arguments('1', '2', another_parameter='3', z='4'))
        self.assertEqual('123baz', obj.optional_arguments('1', '2', z='3'))
        self.assertEqual('12bar3', obj.optional_arguments('1', '2', another_parameter='3'))
        self.assertRaises(TypeError, obj.optional_arguments, '1', '2', '3', '4', another_parameter='5')
        self.assertRaises(TypeError, obj.optional_arguments, '1', '2', '3', y='4')
        self.assertRaises(TypeError, obj.optional_arguments, '1', foo='4')

    def test_blocking_procedure(self):
        self.assertEqual(0, self.conn.test_service.blocking_procedure(0, 0))
        self.assertEqual(1, self.conn.test_service.blocking_procedure(1, 0))
        self.assertEqual(1+2, self.conn.test_service.blocking_procedure(2))
        self.assertEqual(sum(x for x in range(1, 43)), self.conn.test_service.blocking_procedure(42))

    def test_too_many_arguments(self):
        self.assertRaises(TypeError, self.conn.test_service.optional_arguments, '1', '2', '3', '4', '5')
        obj = self.conn.test_service.create_test_object('jeb')
        self.assertRaises(TypeError, obj.optional_arguments, '1', '2', '3', '4', '5')

    def test_too_few_arguments(self):
        self.assertRaises(TypeError, self.conn.test_service.optional_arguments)
        obj = self.conn.test_service.create_test_object('jeb')
        self.assertRaises(TypeError, obj.optional_arguments)

    def test_enums(self):
        enum = self.conn.test_service.TestEnum
        self.assertEqual(enum.value_b, self.conn.test_service.enum_return())
        self.assertEqual(enum.value_a, self.conn.test_service.enum_echo(enum.value_a))
        self.assertEqual(enum.value_b, self.conn.test_service.enum_echo(enum.value_b))
        self.assertEqual(enum.value_c, self.conn.test_service.enum_echo(enum.value_c))

        self.assertEqual(enum.value_a, self.conn.test_service.enum_default_arg(enum.value_a))
        self.assertEqual(enum.value_c, self.conn.test_service.enum_default_arg())
        self.assertEqual(enum.value_b, self.conn.test_service.enum_default_arg(enum.value_b))

    def test_invalid_enum(self):
        self.assertRaises(ValueError, self.conn.test_service.TestEnum, 9999)

    def test_collections(self):
        self.assertEqual([], self.conn.test_service.increment_list([]))
        self.assertEqual([1, 2, 3], self.conn.test_service.increment_list([0, 1, 2]))
        self.assertEqual({}, self.conn.test_service.increment_dictionary({}))
        self.assertEqual({'a': 1, 'b': 2, 'c': 3},
                         self.conn.test_service.increment_dictionary({'a': 0, 'b': 1, 'c': 2}))
        self.assertEqual(set(), self.conn.test_service.increment_set(set()))
        self.assertEqual(set([1, 2, 3]), self.conn.test_service.increment_set(set([0, 1, 2])))
        self.assertEqual((2, 3), self.conn.test_service.increment_tuple((1, 2)))
        self.assertRaises(TypeError, self.conn.test_service.increment_list, None)
        self.assertRaises(TypeError, self.conn.test_service.increment_set, None)
        self.assertRaises(TypeError, self.conn.test_service.increment_dictionary, None)

    def test_nested_collections(self):
        self.assertEqual({}, self.conn.test_service.increment_nested_collection({}))
        self.assertEqual({'a': [1, 2], 'b': [], 'c': [3]},
                         self.conn.test_service.increment_nested_collection({'a': [0, 1], 'b': [], 'c': [2]}))

    def test_collections_of_objects(self):
        objs = self.conn.test_service.add_to_object_list([], "jeb")
        self.assertEqual(1, len(objs))
        self.assertEqual("value=jeb", objs[0].get_value())
        objs = self.conn.test_service.add_to_object_list(objs, "bob")
        self.assertEqual(2, len(objs))
        self.assertEqual("value=jeb", objs[0].get_value())
        self.assertEqual("value=bob", objs[1].get_value())

    def test_colllections_default_values(self):
        self.assertEqual((1, False), self.conn.test_service.tuple_default())
        self.assertEqual([1, 2, 3], self.conn.test_service.list_default())
        self.assertEqual(set([1, 2, 3]), self.conn.test_service.set_default())
        self.assertEqual({1: False, 2: True}, self.conn.test_service.dictionary_default())

    def test_client_members(self):
        self.assertSetEqual(
            set(['krpc', 'test_service', 'add_stream', 'stream', 'close']),
            set(x for x in dir(self.conn) if not x.startswith('_')))

    def test_krpc_service_members(self):
        self.assertSetEqual(
            set(['get_services', 'get_status', 'add_stream', 'remove_stream',
                 'current_game_scene', 'GameScene', 'clients']),
            set(x for x in dir(self.conn.krpc) if not x.startswith('_')))

    def test_test_service_service_members(self):
        self.assertSetEqual(
            set([
                'float_to_string',
                'double_to_string',
                'int32_to_string',
                'int64_to_string',
                'bool_to_string',
                'string_to_int32',
                'bytes_to_hex_string',
                'add_multiple_values',

                'string_property',

                'string_property_private_get',

                'string_property_private_set',

                'create_test_object',
                'echo_test_object',

                'object_property',

                'TestClass',

                'optional_arguments',

                'TestEnum',
                'enum_return',
                'enum_echo',
                'enum_default_arg',

                'blocking_procedure',

                'increment_list',
                'increment_dictionary',
                'increment_set',
                'increment_tuple',
                'increment_nested_collection',
                'tuple_default',
                'dictionary_default',
                'list_default',
                'set_default',
                'add_to_object_list',

                'counter',

                'throw_argument_exception',
                'throw_invalid_operation_exception'
            ]),
            set(x for x in dir(self.conn.test_service) if not x.startswith('_')))

    def test_test_service_test_class_members(self):
        self.assertSetEqual(
            set([
                'get_value',
                'float_to_string',
                'object_to_string',

                'int_property',

                'object_property',

                'optional_arguments',
                'static_method'
            ]),
            set(x for x in dir(self.conn.test_service.TestClass) if not x.startswith('_')))

    def test_test_service_enum_members(self):
        self.assertSetEqual(
            set(['value_a', 'value_b', 'value_c']),
            set(x for x in dir(self.conn.test_service.TestEnum) if not x.startswith('_')))
        self.assertEqual(0, self.conn.test_service.TestEnum.value_a.value)
        self.assertEqual(1, self.conn.test_service.TestEnum.value_b.value)
        self.assertEqual(2, self.conn.test_service.TestEnum.value_c.value)

    def test_line_endings(self):
        strings = [
            'foo\nbar',
            'foo\rbar',
            'foo\n\rbar',
            'foo\r\nbar',
            'foo\x10bar',
            'foo\x13bar',
            'foo\x10\x13bar',
            'foo\x13\x10bar'
        ]
        for string in strings:
            self.conn.test_service.string_property = string
            self.assertEqual(string, self.conn.test_service.string_property)

    def test_types_from_different_connections(self):
        conn1 = self.connect()
        conn2 = self.connect()
        self.assertNotEqual(conn1.test_service.TestClass, conn2.test_service.TestClass)
        obj2 = conn2.test_service.TestClass(0)
        obj1 = conn1._types.coerce_to(obj2, conn1._types.as_type('Class(TestService.TestClass)'))
        self.assertEqual(obj1, obj2)
        self.assertNotEqual(type(obj1), type(obj2))
        self.assertEqual(type(obj1), conn1.test_service.TestClass)
        self.assertEqual(type(obj2), conn2.test_service.TestClass)

    def test_thread_safe(self):
        thread_count = 32
        repeats = 100

        latch = [threading.Condition(), thread_count]

        def thread_main(latch):
            for _ in range(repeats):
                self.assertEqual("False", self.conn.test_service.bool_to_string(False))
                self.assertEqual(12345, self.conn.test_service.string_to_int32("12345"))
            with latch[0]:
                latch[1] -= 1
                if latch[1] <= 0:
                    latch[0].notifyAll()

        for _ in range(thread_count):
            thread = threading.Thread(target=thread_main, args=(latch,))
            thread.daemon = True
            thread.start()

        with latch[0]:
            while latch[1] > 0:
                latch[0].wait(10)
        self.assertEqual(0, latch[1])

if __name__ == '__main__':
    unittest.main()
