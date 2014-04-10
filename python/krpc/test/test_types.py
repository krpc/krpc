#!/usr/bin/env python2

import unittest
from krpc.types import _Types as Types
from krpc.types import _ValueType as ValueType
from krpc.types import _MessageType as MessageType
from krpc.types import _ClassType as ClassType
from krpc.types import _EnumType as EnumType
from krpc.types import _BaseClass as BaseClass
import krpc.schema.KRPC

PROTOBUF_VALUE_TYPES = ['double', 'float', 'int32', 'int64', 'uint32', 'uint64', 'bool', 'string', 'bytes']
PYTHON_VALUE_TYPES = [float, int, long, bool, str, bytes]

PROTOBUF_TO_PYTHON_VALUE_TYPE = {
    'double': float,
    'float': float,
    'int32': int,
    'int64': long,
    'uint32': int,
    'uint64': long,
    'bool': bool,
    'string': str,
    'bytes': bytes
}

class TestTypes(unittest.TestCase):

    def test_value_types(self):
        types = Types()
        for protobuf_typ in PROTOBUF_VALUE_TYPES:
            python_typ = PROTOBUF_TO_PYTHON_VALUE_TYPE[protobuf_typ]
            typ = types.as_type(protobuf_typ)
            self.assertTrue(isinstance(typ, ValueType))
            self.assertEqual(protobuf_typ, typ.protobuf_type)
            self.assertEqual(python_typ, typ.python_type)

    def test_message_types(self):
        types = Types()
        typ = types.as_type('KRPC.Request')
        self.assertTrue(isinstance(typ, MessageType))
        self.assertEqual(krpc.schema.KRPC.Request, typ.python_type)
        self.assertEqual('KRPC.Request', typ.protobuf_type)

    def test_enum_types(self):
        types = Types()
        typ = types.as_type('Test.TestEnum')
        self.assertTrue(isinstance(typ, EnumType))
        self.assertEqual(int, typ.python_type)
        self.assertEqual('Test.TestEnum', typ.protobuf_type)

    def test_class_types(self):
        types = Types()
        typ = types.as_type('Class(ServiceName.ClassName)')
        self.assertTrue(isinstance(typ, ClassType))
        self.assertTrue(issubclass(typ.python_type, BaseClass))
        self.assertTrue('Class(ServiceName.ClassName)', typ.protobuf_type)
        instance = typ.python_type(42)
        self.assertEqual(42, instance._object_id)
        typ2 = types.as_type('Class(ServiceName.ClassName)')
        self.assertEqual(typ, typ2)

    def test_get_parameter_type(self):
        types = Types()
        self.assertEqual(float, types.get_parameter_type(0, 'float', []).python_type)
        self.assertEqual('int32', types.get_parameter_type(0, 'int32', []).protobuf_type)
        self.assertEqual('KRPC.Response', types.get_parameter_type(1, 'KRPC.Response', []).protobuf_type)
        class_parameter = types.get_parameter_type(0, 'uint64', ['ParameterType(0).Class(ServiceName.ClassName)'])
        self.assertEqual(types.as_type('Class(ServiceName.ClassName)'), class_parameter)
        self.assertTrue(isinstance(class_parameter, ClassType))
        self.assertTrue(issubclass(class_parameter.python_type, BaseClass))
        self.assertEqual('Class(ServiceName.ClassName)', class_parameter.protobuf_type)
        self.assertEqual('uint64', types.get_parameter_type(0, 'uint64', ['ParameterType(1).Class(ServiceName.ClassName)']).protobuf_type)
        self.assertEqual('KRPC.Test.TestEnum', types.get_parameter_type(0, 'KRPC.Test.TestEnum', []).protobuf_type)


    def test_get_return_type(self):
        types = Types()
        self.assertEqual('float', types.get_return_type('float', []).protobuf_type)
        self.assertEqual('int32', types.get_return_type( 'int32', []).protobuf_type)
        self.assertEqual('KRPC.Response', types.get_return_type('KRPC.Response', []).protobuf_type)
        self.assertEqual('KRPC.Test.TestEnum', types.get_return_type('KRPC.Test.TestEnum', []).protobuf_type)
        self.assertEqual('Class(ServiceName.ClassName)', types.get_return_type('uint64', ['ReturnType.Class(ServiceName.ClassName)']).protobuf_type)

    def test_coerce_to(self):
        types = Types()
        cases = [
            (42.0, 42,   'double'),
            (42.0, 42,   'float'),
            (42,   42.0, 'int32'),
            (42,   42L,  'int32'),
            (42L,  42.0, 'int64'),
            (42L,  42,   'int64'),
            (42,   42.0, 'uint32'),
            (42,   42L,  'uint32'),
            (42L,  42.0, 'uint64'),
            (42L,  42,   'uint64'),
        ]
        for expected, value, typ in cases:
            coerced_value = types.coerce_to(value, types.as_type(typ))
            self.assertEqual(expected, coerced_value)
            self.assertEqual(type(expected), type(coerced_value))

        self.assertRaises(ValueError, types.coerce_to, None, types.as_type('float'))
        self.assertRaises(ValueError, types.coerce_to, '', types.as_type('float'))
        self.assertRaises(ValueError, types.coerce_to, True, types.as_type('float'))


if __name__ == '__main__':
    unittest.main()
