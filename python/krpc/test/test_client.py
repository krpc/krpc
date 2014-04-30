#!/usr/bin/env python2

import unittest
import binascii
import subprocess
import time
import krpc
import krpc.test.Test as TestSchema

class TestClient(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.server = subprocess.Popen(['bin/TestServer/TestServer.exe'])
        time.sleep(0.25)

    def setUp(self):
        self.ksp = krpc.connect(name='TestClient')

    @classmethod
    def tearDownClass(cls):
        cls.server.kill()

    def test_value_parameters(self):
        self.assertEqual('3.14159', self.ksp.test_service.float_to_string(float(3.14159)))
        self.assertEqual('3.14159', self.ksp.test_service.double_to_string(float(3.14159)))
        self.assertEqual('42', self.ksp.test_service.int32_to_string(42))
        self.assertEqual('123456789000', self.ksp.test_service.int64_to_string(123456789000L))
        self.assertEqual('True', self.ksp.test_service.bool_to_string(True))
        self.assertEqual('False', self.ksp.test_service.bool_to_string(False))
        self.assertEqual(12345, self.ksp.test_service.string_to_int32('12345'))
        self.assertEqual('deadbeef', self.ksp.test_service.bytes_to_hex_string(b'\xde\xad\xbe\xef'))

    def test_multiple_value_parameters(self):
        self.assertEqual('3.14159', self.ksp.test_service.add_multiple_values(0.14159, 1, 2))

    def test_auto_value_type_conversion(self):
        self.assertEqual('42', self.ksp.test_service.float_to_string(42))
        self.assertEqual('42', self.ksp.test_service.float_to_string(42L))
        self.assertEqual('6', self.ksp.test_service.add_multiple_values(1L, 2L, 3L))
        self.assertRaises(TypeError, self.ksp.test_service.float_to_string, '42')

    def test_incorrect_parameter_type(self):
        self.assertRaises(TypeError, self.ksp.test_service.float_to_string, 'foo')
        self.assertRaises(TypeError, self.ksp.test_service.add_multiple_values, 0.14159, 'foo', 2)

    def test_properties(self):
        self.ksp.test_service.string_property = 'foo';
        self.assertEqual('foo', self.ksp.test_service.string_property)
        self.assertEqual('foo', self.ksp.test_service.string_property_private_set)
        self.ksp.test_service.string_property_private_get = 'foo'
        obj = self.ksp.test_service.create_test_object('bar')
        self.ksp.test_service.object_property = obj
        self.assertEqual (obj, self.ksp.test_service.object_property)

    def test_class_as_return_value(self):
        obj = self.ksp.test_service.create_test_object('jeb')
        self.assertEqual('TestClass', type(obj).__name__)

    def test_class_none_value(self):
        self.assertIsNone(self.ksp.test_service.echo_test_object(None))
        obj = self.ksp.test_service.create_test_object('bob')
        self.assertEqual('bobnull', obj.object_to_string(None))
        # Check following doesn't throw an exception
        self.ksp.test_service.object_property
        self.ksp.test_service.object_property = None
        self.assertIsNone (self.ksp.test_service.object_property)

    def test_class_methods(self):
        obj = self.ksp.test_service.create_test_object('bob')
        self.assertEqual('value=bob', obj.get_value())
        self.assertEqual('bob3.14159', obj.float_to_string(3.14159))
        obj2 = self.ksp.test_service.create_test_object('bill')
        self.assertEqual('bobbill', obj.object_to_string(obj2))

    def test_class_properties(self):
        obj = self.ksp.test_service.create_test_object('jeb')
        self.assertEqual(0, obj.int_property)
        obj.int_property = 42
        self.assertEqual(42, obj.int_property)
        obj2 = self.ksp.test_service.create_test_object('kermin')
        obj.object_property = obj2
        self.assertEqual(obj2._object_id, obj.object_property._object_id)

    def test_setattr_for_properties(self):
        # Check that properties are added to the dynamically generated service class,
        # not the base class krpc.Service
        self.assertRaises (AttributeError, getattr, krpc.service._Service, 'object_property')
        # Check following does not throw an exception
        getattr(self.ksp.test_service, 'object_property')

    def test_optional_arguments(self):
        self.assertEqual('jebfoobarbaz', self.ksp.test_service.optional_arguments('jeb'))
        self.assertEqual('jebbobbillbaz', self.ksp.test_service.optional_arguments('jeb', 'bob', 'bill'))

    def test_named_parameters(self):
        self.assertEqual('1234', self.ksp.test_service.optional_arguments(x='1', y='2', z='3', w='4'))
        self.assertEqual('2413', self.ksp.test_service.optional_arguments(z='1', x='2', w='3', y='4'))
        self.assertEqual('1243', self.ksp.test_service.optional_arguments('1', '2', w='3', z='4'))
        self.assertEqual('123baz', self.ksp.test_service.optional_arguments('1', '2', z='3'))
        self.assertEqual('12bar3', self.ksp.test_service.optional_arguments('1', '2', w='3'))
        self.assertRaises(TypeError, self.ksp.test_service.optional_arguments, '1', '2', '3', '4', w='5')
        self.assertRaises(TypeError, self.ksp.test_service.optional_arguments, '1', '2', '3', y='4')
        self.assertRaises(TypeError, self.ksp.test_service.optional_arguments, '1', foo='4')

        obj = self.ksp.test_service.create_test_object('jeb')
        self.assertEqual('1234', obj.optional_arguments(x='1', y='2', z='3', w='4'))
        self.assertEqual('2413', obj.optional_arguments(z='1', x='2', w='3', y='4'))
        self.assertEqual('1243', obj.optional_arguments('1', '2', w='3', z='4'))
        self.assertEqual('123baz', obj.optional_arguments('1', '2', z='3'))
        self.assertEqual('12bar3', obj.optional_arguments('1', '2', w='3'))
        self.assertRaises(TypeError, obj.optional_arguments, '1', '2', '3', '4', w='5')
        self.assertRaises(TypeError, obj.optional_arguments, '1', '2', '3', y='4')
        self.assertRaises(TypeError, obj.optional_arguments, '1', foo='4')

    def test_blocking_procedure(self):
        self.assertEqual(0, self.ksp.test_service.blocking_procedure(0,0))
        self.assertEqual(1, self.ksp.test_service.blocking_procedure(1,0))
        self.assertEqual(1+2, self.ksp.test_service.blocking_procedure(2))
        self.assertEqual(sum(x for x in range(1,43)), self.ksp.test_service.blocking_procedure(42))

    def test_too_many_arguments(self):
        self.assertRaises(TypeError, self.ksp.test_service.optional_arguments, '1', '2', '3', '4', '5')
        obj = self.ksp.test_service.create_test_object('jeb')
        self.assertRaises(TypeError, obj.optional_arguments, '1', '2', '3', '4', '5')

    def test_client_members(self):
        self.assertSetEqual(
            set(['krpc', 'test_service']),
            set(filter(lambda x: not x.startswith('_'), dir(self.ksp))))

    def test_krpc_service_members(self):
        self.assertSetEqual(
            set(['get_services', 'get_status']),
            set(filter(lambda x: not x.startswith('_'), dir(self.ksp.krpc))))

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
                'get__string_property',
                'set__string_property',

                'string_property_private_get',
                'set__string_property_private_get',

                'string_property_private_set',
                'get__string_property_private_set',

                'create_test_object',
                'echo_test_object',

                'object_property',
                'get__object_property',
                'set__object_property',

                'TestClass',

                'optional_arguments'
            ]),
            set(filter(lambda x: not x.startswith('_'), dir(self.ksp.test_service))))

    def test_test_service_test_class_members(self):
        self.assertSetEqual(
            set([
                'get_value',
                'float_to_string',
                'object_to_string',

                'int_property',
                'test_class__get__int_property',
                'test_class__set__int_property',

                'object_property',
                'test_class__get__object_property',
                'test_class__set__object_property',

                'optional_arguments'
            ]),
            set(filter(lambda x: not x.startswith('_'), dir(self.ksp.test_service.TestClass))))


    def test_enums(self):
        self.assertEqual(TestSchema.a, self.ksp.test_service.enum_return())
        self.assertEqual(TestSchema.a, self.ksp.test_service.enum_echo(TestSchema.a))
        self.assertEqual(TestSchema.b, self.ksp.test_service.enum_echo(TestSchema.b))
        self.assertEqual(TestSchema.c, self.ksp.test_service.enum_echo(TestSchema.c))

        self.assertEqual(TestSchema.a, self.ksp.test_service.enum_default_arg(TestSchema.a))
        self.assertEqual(TestSchema.c, self.ksp.test_service.enum_default_arg())
        self.assertEqual(TestSchema.b, self.ksp.test_service.enum_default_arg(TestSchema.b))

        enum = self.ksp.test_service.CSharpEnum
        self.assertEqual(enum.value_b, self.ksp.test_service.c_sharp_enum_return())
        self.assertEqual(enum.value_a, self.ksp.test_service.c_sharp_enum_echo(enum.value_a))
        self.assertEqual(enum.value_b, self.ksp.test_service.c_sharp_enum_echo(enum.value_b))
        self.assertEqual(enum.value_c, self.ksp.test_service.c_sharp_enum_echo(enum.value_c))

        self.assertEqual(enum.value_a, self.ksp.test_service.c_sharp_enum_default_arg(enum.value_a))
        self.assertEqual(enum.value_c, self.ksp.test_service.c_sharp_enum_default_arg())
        self.assertEqual(enum.value_b, self.ksp.test_service.c_sharp_enum_default_arg(enum.value_b))

    def test_invalid_enum(self):
        self.assertRaises(krpc.client.RPCError, self.ksp.test_service.c_sharp_enum_echo, 9999)

    def test_client_members(self):
        self.assertSetEqual(
            set(['krpc', 'test_service']),
            set(filter(lambda x: not x.startswith('_'), dir(self.ksp))))

    def test_krpc_service_members(self):
        self.assertSetEqual(
            set(['get_services', 'get_status']),
            set(filter(lambda x: not x.startswith('_'), dir(self.ksp.krpc))))

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
                'get__string_property',
                'set__string_property',

                'string_property_private_get',
                'set__string_property_private_get',

                'string_property_private_set',
                'get__string_property_private_set',

                'create_test_object',
                'echo_test_object',

                'object_property',
                'get__object_property',
                'set__object_property',

                'TestClass',

                'optional_arguments',

                'enum_return',
                'enum_echo',
                'enum_default_arg',
                'CSharpEnum',
                'c_sharp_enum_return',
                'c_sharp_enum_echo',
                'c_sharp_enum_default_arg',

                'blocking_procedure'
            ]),
            set(filter(lambda x: not x.startswith('_'), dir(self.ksp.test_service))))

    def test_test_service_test_class_members(self):
        self.assertSetEqual(
            set([
                'get_value',
                'float_to_string',
                'object_to_string',

                'int_property',
                'test_class__get__int_property',
                'test_class__set__int_property',

                'object_property',
                'test_class__get__object_property',
                'test_class__set__object_property',

                'optional_arguments'
            ]),
            set(filter(lambda x: not x.startswith('_'), dir(self.ksp.test_service.TestClass))))

    def test_test_service_enum_members(self):
        self.assertSetEqual(
            set(['value_a','value_b','value_c']),
            set(filter(lambda x: not x.startswith('_'), dir(self.ksp.test_service.CSharpEnum))))
        self.assertEqual (0, self.ksp.test_service.CSharpEnum.value_a)
        self.assertEqual (1, self.ksp.test_service.CSharpEnum.value_b)
        self.assertEqual (2, self.ksp.test_service.CSharpEnum.value_c)

if __name__ == '__main__':
    unittest.main()
