import unittest
import socket
import threading
import krpc
from krpc.test.servertestcase import ServerTestCase


class TestClient(ServerTestCase, unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        super(TestClient, cls).setUpClass()

    def test_get_status(self) -> None:
        status = self.conn.krpc.get_status()
        self.assertRegex(status.version, r'^[0-9]+\.[0-9]+\.[0-9]+$')
        self.assertGreater(status.bytes_read, 0)

    def test_wrong_rpc_port(self) -> None:
        with self.assertRaises(socket.error):
            krpc.connect(name='python_client_test_wrong_rpc_port',
                         address='localhost',
                         rpc_port=ServerTestCase.rpc_port() ^
                         ServerTestCase.stream_port(),
                         stream_port=ServerTestCase.stream_port())

    def test_wrong_stream_port(self) -> None:
        with self.assertRaises(socket.error):
            krpc.connect(name='python_client_test_wrong_stream_port',
                         address='localhost',
                         rpc_port=ServerTestCase.rpc_port(),
                         stream_port=ServerTestCase.rpc_port() ^
                         ServerTestCase.stream_port())

    def test_wrong_rpc_server(self) -> None:
        with self.assertRaises(krpc.error.ConnectionError) as cm:
            krpc.connect(name='python_client_test_wrong_rpc_server',
                         address='localhost',
                         rpc_port=ServerTestCase.stream_port(),
                         stream_port=ServerTestCase.stream_port())
        self.assertEqual('Connection request was for the rpc server, ' +
                         'but this is the stream server. ' +
                         'Did you connect to the wrong port number?',
                         str(cm.exception))

    def test_wrong_stream_server(self) -> None:
        with self.assertRaises(krpc.error.ConnectionError) as cm:
            krpc.connect(name='python_client_test_wrong_stream_server',
                         address='localhost',
                         rpc_port=ServerTestCase.rpc_port(),
                         stream_port=ServerTestCase.rpc_port())
        self.assertEqual(
            'Connection request was for the stream server, ' +
            'but this is the rpc server. ' +
            'Did you connect to the wrong port number?', str(cm.exception))

    def test_value_parameters(self) -> None:
        self.assertEqual('3.14159',
                         self.conn.test_service.float_to_string(3.14159))
        self.assertEqual('3.14159',
                         self.conn.test_service.double_to_string(3.14159))
        self.assertEqual('42',
                         self.conn.test_service.int32_to_string(42))
        self.assertEqual('123456789000',
                         self.conn.test_service.int64_to_string(123456789000))
        self.assertEqual('True',
                         self.conn.test_service.bool_to_string(True))
        self.assertEqual('False',
                         self.conn.test_service.bool_to_string(False))
        self.assertEqual(12345,
                         self.conn.test_service.string_to_int32('12345'))
        self.assertEqual('deadbeef',
                         self.conn.test_service.bytes_to_hex_string(
                             b'\xde\xad\xbe\xef'))

    def test_multiple_value_parameters(self) -> None:
        self.assertEqual('3.14159',
                         self.conn.test_service.add_multiple_values(
                             0.14159, 1, 2))

    def test_auto_value_type_conversion(self) -> None:
        self.assertEqual('42', self.conn.test_service.float_to_string(42))
        self.assertEqual('42', self.conn.test_service.float_to_string(42))
        self.assertEqual(
            '6', self.conn.test_service.add_multiple_values(1, 2, 3))
        self.assertRaises(
            TypeError, self.conn.test_service.float_to_string, '42')

    def test_incorrect_parameter_type(self) -> None:
        self.assertRaises(
            TypeError, self.conn.test_service.float_to_string, 'foo')
        self.assertRaises(
            TypeError, self.conn.test_service.add_multiple_values,
            0.14159, 'foo', 2)

    def test_properties(self) -> None:
        self.conn.test_service.string_property = 'foo'
        self.assertEqual('foo', self.conn.test_service.string_property)
        self.assertEqual('foo',
                         self.conn.test_service.string_property_private_set)
        self.conn.test_service.string_property_private_get = 'foo'
        obj = self.conn.test_service.create_test_object('bar')
        self.conn.test_service.object_property = obj
        self.assertEqual(obj, self.conn.test_service.object_property)

    def test_class_as_return_value(self) -> None:
        obj = self.conn.test_service.create_test_object('jeb')
        self.assertEqual('TestClass', type(obj).__name__)

    def test_class_none_value(self) -> None:
        self.assertIsNone(self.conn.test_service.echo_test_object(None))
        obj = self.conn.test_service.create_test_object('bob')
        self.assertEqual('bobnull', obj.object_to_string(None))
        self.conn.test_service.object_property = None
        self.assertIsNone(self.conn.test_service.object_property)

    def test_class_none_value_when_not_allowed(self) -> None:
        with self.assertRaises(krpc.error.RPCError) as cm:
            self.conn.test_service.return_null_when_not_allowed()
        self.assertTrue(str(cm.exception).startswith(
            'Incorrect value returned by '
            'TestService.ReturnNullWhenNotAllowed. '
            'Expected a non-null value of type '
            'TestServer.TestService+TestClass, '
            'got null, but the procedure is not marked as nullable.'
        ))

    def test_class_methods(self) -> None:
        obj = self.conn.test_service.create_test_object('bob')
        self.assertEqual('value=bob', obj.get_value())
        self.assertEqual('bob3.14159', obj.float_to_string(3.14159))
        obj2 = self.conn.test_service.create_test_object('bill')
        self.assertEqual('bobbill', obj.object_to_string(obj2))

    def test_class_static_methods(self) -> None:
        self.assertEqual('jeb',
                         self.conn.test_service.TestClass.static_method())
        self.assertEqual('jebbobbill',
                         self.conn.test_service.TestClass.static_method(
                             'bob', 'bill'))

    def test_class_properties(self) -> None:
        obj = self.conn.test_service.create_test_object('jeb')
        obj.int_property = 0
        self.assertEqual(0, obj.int_property)
        obj.int_property = 42
        self.assertEqual(42, obj.int_property)
        obj2 = self.conn.test_service.create_test_object('kermin')
        obj.object_property = obj2
        self.assertEqual(obj2._object_id, obj.object_property._object_id)
        obj.string_property_private_get = "bob"
        self.assertEqual("bob", obj.string_property_private_set)

    def test_optional_arguments(self) -> None:
        self.assertEqual('jebfoobarnull',
                         self.conn.test_service.optional_arguments('jeb'))
        self.assertEqual('jebbobbillnull',
                         self.conn.test_service.optional_arguments(
                             'jeb', 'bob', 'bill'))
        obj = self.conn.test_service.create_test_object('kermin')
        self.assertEqual('jebbobbillkermin',
                         self.conn.test_service.optional_arguments(
                             'jeb', 'bob', 'bill', obj))

    def test_named_parameters(self) -> None:
        obj3 = self.conn.test_service.create_test_object('3')
        obj4 = self.conn.test_service.create_test_object('4')
        obj5 = self.conn.test_service.create_test_object('5')
        self.assertEqual('1234',
                         self.conn.test_service.optional_arguments(
                             x='1', y='2', z='3', obj=obj4))
        self.assertEqual('2413',
                         self.conn.test_service.optional_arguments(
                             z='1', x='2', obj=obj3, y='4'))
        self.assertEqual('1243',
                         self.conn.test_service.optional_arguments(
                             '1', '2', obj=obj3, z='4'))
        self.assertEqual('123null',
                         self.conn.test_service.optional_arguments(
                             '1', '2', z='3'))
        self.assertEqual('12bar3',
                         self.conn.test_service.optional_arguments(
                             '1', '2', obj=obj3))
        self.assertRaises(TypeError, self.conn.test_service.optional_arguments,
                          '1', '2', '3', '4', obj=obj5)
        self.assertRaises(TypeError, self.conn.test_service.optional_arguments,
                          '1', '2', '3', y='4')
        self.assertRaises(TypeError, self.conn.test_service.optional_arguments,
                          '1', foo='4')

        obj = self.conn.test_service.create_test_object('jeb')
        self.assertEqual('1234',
                         obj.optional_arguments(x='1', y='2', z='3', obj=obj4))
        self.assertEqual('2413',
                         obj.optional_arguments(z='1', x='2', obj=obj3, y='4'))
        self.assertEqual('1243',
                         obj.optional_arguments('1', '2', obj=obj3, z='4'))
        self.assertEqual('123null',
                         obj.optional_arguments('1', '2', z='3'))
        self.assertEqual('12bar3',
                         obj.optional_arguments('1', '2', obj=obj3))
        self.assertRaises(TypeError, obj.optional_arguments,
                          '1', '2', '3', '4', obj=obj5)
        self.assertRaises(TypeError, obj.optional_arguments,
                          '1', '2', '3', y='4')
        self.assertRaises(TypeError, obj.optional_arguments,
                          '1', foo='4')

    def test_blocking_procedure(self) -> None:
        self.assertEqual(0, self.conn.test_service.blocking_procedure(0, 0))
        self.assertEqual(1, self.conn.test_service.blocking_procedure(1, 0))
        self.assertEqual(1+2, self.conn.test_service.blocking_procedure(2))
        self.assertEqual(sum(x for x in range(1, 43)),
                         self.conn.test_service.blocking_procedure(42))

    def test_too_many_arguments(self) -> None:
        self.assertRaises(TypeError, self.conn.test_service.optional_arguments,
                          '1', '2', '3', '4', '5')
        obj = self.conn.test_service.create_test_object('jeb')
        self.assertRaises(TypeError, obj.optional_arguments,
                          '1', '2', '3', '4', '5')

    def test_too_few_arguments(self) -> None:
        self.assertRaises(TypeError, self.conn.test_service.optional_arguments)
        obj = self.conn.test_service.create_test_object('jeb')
        self.assertRaises(TypeError, obj.optional_arguments)

    def test_enums(self) -> None:
        enum = self.conn.test_service.TestEnum
        self.assertEqual(enum.value_b, self.conn.test_service.enum_return())
        self.assertEqual(enum.value_a,
                         self.conn.test_service.enum_echo(enum.value_a))
        self.assertEqual(enum.value_b,
                         self.conn.test_service.enum_echo(enum.value_b))
        self.assertEqual(enum.value_c,
                         self.conn.test_service.enum_echo(enum.value_c))

        self.assertEqual(enum.value_a,
                         self.conn.test_service.enum_default_arg(enum.value_a))
        self.assertEqual(enum.value_c,
                         self.conn.test_service.enum_default_arg())
        self.assertEqual(enum.value_b,
                         self.conn.test_service.enum_default_arg(enum.value_b))

    def test_invalid_enum(self) -> None:
        self.assertRaises(ValueError, self.conn.test_service.TestEnum, 9999)

    def test_collections(self) -> None:
        self.assertEqual(
            [], self.conn.test_service.increment_list([]))
        self.assertEqual(
            [1, 2, 3], self.conn.test_service.increment_list([0, 1, 2]))
        self.assertEqual(
            {}, self.conn.test_service.increment_dictionary({}))
        self.assertEqual(
            {'a': 1, 'b': 2, 'c': 3},
            self.conn.test_service.increment_dictionary(
                {'a': 0, 'b': 1, 'c': 2}))
        self.assertEqual(
            set(), self.conn.test_service.increment_set(set()))
        self.assertEqual(
            set([1, 2, 3]),
            self.conn.test_service.increment_set(set([0, 1, 2])))
        self.assertEqual(
            (2, 3), self.conn.test_service.increment_tuple((1, 2)))
        self.assertRaises(
            TypeError, self.conn.test_service.increment_list, None)
        self.assertRaises(
            TypeError, self.conn.test_service.increment_set, None)
        self.assertRaises(
            TypeError, self.conn.test_service.increment_dictionary, None)

    def test_nested_collections(self) -> None:
        self.assertEqual(
            {}, self.conn.test_service.increment_nested_collection({}))
        self.assertEqual(
            {'a': [1, 2], 'b': [], 'c': [3]},
            self.conn.test_service.increment_nested_collection(
                {'a': [0, 1], 'b': [], 'c': [2]}))

    def test_collections_of_objects(self) -> None:
        objs = self.conn.test_service.add_to_object_list([], "jeb")
        self.assertEqual(1, len(objs))
        self.assertEqual("value=jeb", objs[0].get_value())
        objs = self.conn.test_service.add_to_object_list(objs, "bob")
        self.assertEqual(2, len(objs))
        self.assertEqual("value=jeb", objs[0].get_value())
        self.assertEqual("value=bob", objs[1].get_value())

    def test_colllections_default_values(self) -> None:
        self.assertEqual((1, False), self.conn.test_service.tuple_default())
        self.assertEqual([1, 2, 3], self.conn.test_service.list_default())
        self.assertEqual(set([1, 2, 3]), self.conn.test_service.set_default())
        self.assertEqual({1: False, 2: True},
                         self.conn.test_service.dictionary_default())

    def test_invalid_operation_exception(self) -> None:
        with self.assertRaises(RuntimeError) as cm:
            self.conn.test_service.throw_invalid_operation_exception()
        self.assertTrue(str(cm.exception).startswith('Invalid operation'))

    def test_argument_exception(self) -> None:
        with self.assertRaises(ValueError) as cm:
            self.conn.test_service.throw_argument_exception()
        self.assertTrue(str(cm.exception).startswith('Invalid argument'))

    def test_argument_null_exception(self) -> None:
        with self.assertRaises(ValueError) as cm:
            self.conn.test_service.throw_argument_null_exception("")
        self.assertTrue(
            str(cm.exception).startswith('Value cannot be null.\n' +
                                         'Parameter name: foo'))

    def test_argument_out_of_range_exception(self) -> None:
        with self.assertRaises(ValueError) as cm:
            self.conn.test_service.throw_argument_out_of_range_exception(0)
        self.assertTrue(str(cm.exception).startswith(
            'Specified argument was out of the range of valid values.\n' +
            'Parameter name: foo'))

    def test_custom_exception(self) -> None:
        with self.assertRaises(self.conn.test_service.CustomException) as cm:
            self.conn.test_service.throw_custom_exception()
        self.assertTrue(
            str(cm.exception).startswith('A custom kRPC exception'))

    def test_client_members(self) -> None:
        self.assertSetEqual(
            set(['krpc', 'test_service', 'stream', 'add_stream',
                 'stream_update_condition', 'wait_for_stream_update',
                 'add_stream_update_callback', 'remove_stream_update_callback',
                 'get_call', 'close', 'ui', 'drawing', 'kerbal_alarm_clock',
                 'lidar', 'infernal_robotics', 'remote_tech', 'space_center',
                 'docking_camera']),
            set(x for x in dir(self.conn) if not x.startswith('_')))

    def test_krpc_service_members(self) -> None:
        self.assertSetEqual(
            set(['get_client_id', 'get_client_name', 'get_services',
                 'get_status', 'add_stream', 'start_stream',
                 'set_stream_rate', 'remove_stream',
                 'add_event', 'current_game_scene', 'GameScene', 'paused',
                 'clients', 'Expression', 'Type', 'InvalidOperationException',
                 'ArgumentException', 'ArgumentNullException',
                 'ArgumentOutOfRangeException']),
            set(x for x in dir(self.conn.krpc) if not x.startswith('_')))

    def test_test_service_service_members(self) -> None:
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

                'return_null_when_not_allowed',

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

                'CustomException',
                'throw_custom_exception',
                'reset_custom_exception_later',
                'throw_custom_exception_later',

                'throw_invalid_operation_exception',
                'throw_invalid_operation_exception_later',
                'reset_invalid_operation_exception_later',
                'throw_argument_exception',
                'throw_argument_null_exception',
                'throw_argument_out_of_range_exception',

                'on_timer',
                'on_timer_using_lambda'
            ]),
            set(x for x in dir(self.conn.test_service)
                if not x.startswith('_')))

    def test_test_service_test_class_members(self) -> None:
        self.assertSetEqual(
            set([
                'get_value',
                'float_to_string',
                'object_to_string',

                'int_property',

                'object_property',

                'string_property_private_get',
                'string_property_private_set',

                'optional_arguments',
                'static_method'
            ]),
            set(x for x in dir(self.conn.test_service.TestClass)
                if not x.startswith('_')))

    def test_test_service_enum_members(self) -> None:
        self.assertSetEqual(
            set(['value_a', 'value_b', 'value_c']),
            set(x for x in dir(self.conn.test_service.TestEnum)
                if not x.startswith('_')))
        self.assertEqual(0, self.conn.test_service.TestEnum.value_a.value)
        self.assertEqual(1, self.conn.test_service.TestEnum.value_b.value)
        self.assertEqual(2, self.conn.test_service.TestEnum.value_c.value)

    def test_line_endings(self) -> None:
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

    def test_types_from_different_connections(self) -> None:
        conn1 = self.connect()
        conn2 = self.connect()
        # self.assertNotEqual(
        #     conn1.test_service.TestClass, conn2.test_service.TestClass)
        obj2 = conn2.test_service.TestClass(self.conn, 0)
        obj1 = conn1._types.coerce_to(
            obj2, conn1._types.class_type('TestService', 'TestClass'))
        self.assertEqual(obj1, obj2)
        # self.assertNotEqual(type(obj1), type(obj2))
        # self.assertEqual(type(obj1), conn1.test_service.TestClass)
        # self.assertEqual(type(obj2), conn2.test_service.TestClass)

    # def test_thread_safe(self) -> None:
    #     thread_count = 32
    #     repeats = 100
    #
    #     latch = threading.Condition()
    #     count = thread_count
    #
    #     def thread_main() -> None:
    #         for _ in range(repeats):
    #             self.assertEqual(
    #                 "False", self.conn.test_service.bool_to_string(False))
    #             self.assertEqual(
    #                 12345, self.conn.test_service.string_to_int32("12345"))
    #         with latch:
    #             count -= 1
    #             if count <= 0:
    #                 latch.notify_all()
    #
    #     for _ in range(thread_count):
    #         thread = threading.Thread(target=thread_main)
    #         thread.daemon = True
    #         thread.start()
    #
    #     with latch:
    #         while count > 0:
    #             latch.wait(10)
    #     self.assertEqual(0, count)


if __name__ == '__main__':
    unittest.main()
