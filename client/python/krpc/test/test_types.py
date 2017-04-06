import unittest
from enum import Enum
from krpc.types import Types, ValueType, MessageType, ClassType, EnumType
from krpc.types import ListType, DictionaryType, SetType, TupleType
from krpc.types import ClassBase
import krpc.schema.KRPC_pb2 as KRPC

PROTOBUF_VALUE_TYPES = ['double', 'float', 'int32', 'int64', 'uint32',
                        'uint64', 'bool', 'string', 'bytes']
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
        self.assertRaises(ValueError, ValueType, 'invalid')

    def test_message_types(self):
        types = Types()
        typ = types.as_type('KRPC.Request')
        self.assertTrue(isinstance(typ, MessageType))
        self.assertEqual(KRPC.Request, typ.python_type)
        self.assertEqual('KRPC.Request', typ.protobuf_type)
        self.assertRaises(ValueError, types.as_type, 'KRPC.DoesntExist')
        self.assertRaises(ValueError, MessageType, '')
        self.assertRaises(ValueError, MessageType, 'invalid')
        self.assertRaises(ValueError, MessageType, '.')
        self.assertRaises(ValueError, MessageType, 'foo.bar')

    def test_enum_types(self):
        types = Types()
        typ = types.as_type('Enum(ServiceName.EnumName)')
        self.assertTrue(isinstance(typ, EnumType))
        self.assertIsNone(typ.python_type)
        self.assertEqual('Enum(ServiceName.EnumName)', typ.protobuf_type)
        typ.set_values({
            'a': {'value': 0, 'doc': ''},
            'b': {'value': 42, 'doc': ''},
            'c': {'value': 100, 'doc': ''}
        })
        self.assertTrue(issubclass(typ.python_type, Enum))
        self.assertEquals(0, typ.python_type.a.value)
        self.assertEquals(42, typ.python_type.b.value)
        self.assertEquals(100, typ.python_type.c.value)

    def test_class_types(self):
        types = Types()
        typ = types.as_type('Class(ServiceName.ClassName)')
        self.assertTrue(isinstance(typ, ClassType))
        self.assertTrue(issubclass(typ.python_type, ClassBase))
        self.assertEqual('Class(ServiceName.ClassName)', typ.protobuf_type)
        instance = typ.python_type(42)
        self.assertEqual(42, instance._object_id)
        self.assertEqual('ServiceName', instance._service_name)
        self.assertEqual('ClassName', instance._class_name)
        typ2 = types.as_type('Class(ServiceName.ClassName)')
        self.assertEqual(typ, typ2)

    def test_list_types(self):
        types = Types()
        typ = types.as_type('List(int32)')
        self.assertTrue(isinstance(typ, ListType))
        self.assertEqual(typ.python_type, list)
        self.assertEqual('List(int32)', typ.protobuf_type)
        self.assertTrue(isinstance(typ.value_type, ValueType))
        self.assertEqual('int32', typ.value_type.protobuf_type)
        self.assertEqual(int, typ.value_type.python_type)
        self.assertRaises(ValueError, types.as_type, 'List')
        self.assertRaises(ValueError, types.as_type, 'List(')
        self.assertRaises(ValueError, types.as_type, 'List()')
        self.assertRaises(ValueError, types.as_type, 'List(foo')
        self.assertRaises(ValueError, types.as_type, 'List(int32,string)')

    def test_dictionary_types(self):
        types = Types()
        typ = types.as_type('Dictionary(string,int32)')
        self.assertTrue(isinstance(typ, DictionaryType))
        self.assertEqual(typ.python_type, dict)
        self.assertEqual('Dictionary(string,int32)', typ.protobuf_type)
        self.assertTrue(isinstance(typ.key_type, ValueType))
        self.assertEqual('string', typ.key_type.protobuf_type)
        self.assertEqual(str, typ.key_type.python_type)
        self.assertTrue(isinstance(typ.value_type, ValueType))
        self.assertEqual('int32', typ.value_type.protobuf_type)
        self.assertEqual(int, typ.value_type.python_type)
        self.assertRaises(ValueError, types.as_type, 'Dictionary')
        self.assertRaises(ValueError, types.as_type, 'Dictionary(')
        self.assertRaises(ValueError, types.as_type, 'Dictionary()')
        self.assertRaises(ValueError, types.as_type, 'Dictionary(foo')
        self.assertRaises(ValueError, types.as_type, 'Dictionary(string)')
        self.assertRaises(ValueError, types.as_type, 'Dictionary(string,)')
        self.assertRaises(ValueError, types.as_type, 'Dictionary(,)')
        self.assertRaises(ValueError, types.as_type, 'Dictionary(,string)')
        self.assertRaises(ValueError, types.as_type, 'Dictionary(int,string))')

    def test_set_types(self):
        types = Types()
        typ = types.as_type('Set(string)')
        self.assertTrue(isinstance(typ, SetType))
        self.assertEqual(typ.python_type, set)
        self.assertEqual('Set(string)', typ.protobuf_type)
        self.assertTrue(isinstance(typ.value_type, ValueType))
        self.assertEqual('string', typ.value_type.protobuf_type)
        self.assertEqual(str, typ.value_type.python_type)
        self.assertRaises(ValueError, types.as_type, 'Set')
        self.assertRaises(ValueError, types.as_type, 'Set(')
        self.assertRaises(ValueError, types.as_type, 'Set()')
        self.assertRaises(ValueError, types.as_type, 'Set(string,)')
        self.assertRaises(ValueError, types.as_type, 'Set(,)')
        self.assertRaises(ValueError, types.as_type, 'Set(,string)')
        self.assertRaises(ValueError, types.as_type, 'Set(int,string))')

    def test_tuple_types(self):
        types = Types()
        typ = types.as_type('Tuple(bool)')
        self.assertTrue(isinstance(typ, TupleType))
        self.assertEqual(typ.python_type, tuple)
        self.assertEqual('Tuple(bool)', typ.protobuf_type)
        self.assertEqual(1, len(typ.value_types))
        self.assertTrue(isinstance(typ.value_types[0], ValueType))
        self.assertEqual('bool', typ.value_types[0].protobuf_type)
        self.assertEqual(bool, typ.value_types[0].python_type)
        typ = types.as_type('Tuple(int32,string)')
        self.assertTrue(isinstance(typ, TupleType))
        self.assertEqual(typ.python_type, tuple)
        self.assertEqual('Tuple(int32,string)', typ.protobuf_type)
        self.assertEqual(2, len(typ.value_types))
        self.assertTrue(isinstance(typ.value_types[0], ValueType))
        self.assertTrue(isinstance(typ.value_types[1], ValueType))
        self.assertEqual('int32', typ.value_types[0].protobuf_type)
        self.assertEqual('string', typ.value_types[1].protobuf_type)
        self.assertEqual(int, typ.value_types[0].python_type)
        self.assertEqual(str, typ.value_types[1].python_type)
        typ = types.as_type('Tuple(float,int64,string)')
        self.assertTrue(isinstance(typ, TupleType))
        self.assertEqual(typ.python_type, tuple)
        self.assertEqual('Tuple(float,int64,string)', typ.protobuf_type)
        self.assertEqual(3, len(typ.value_types))
        self.assertTrue(isinstance(typ.value_types[0], ValueType))
        self.assertTrue(isinstance(typ.value_types[1], ValueType))
        self.assertTrue(isinstance(typ.value_types[2], ValueType))
        self.assertEqual('float', typ.value_types[0].protobuf_type)
        self.assertEqual('int64', typ.value_types[1].protobuf_type)
        self.assertEqual('string', typ.value_types[2].protobuf_type)
        self.assertEqual(float, typ.value_types[0].python_type)
        self.assertEqual(long, typ.value_types[1].python_type)
        self.assertEqual(str, typ.value_types[2].python_type)
        self.assertRaises(ValueError, types.as_type, 'Tuple')
        self.assertRaises(ValueError, types.as_type, 'Tuple(')
        self.assertRaises(ValueError, types.as_type, 'Tuple()')
        self.assertRaises(ValueError, types.as_type, 'Tuple(foo')
        self.assertRaises(ValueError, types.as_type, 'Tuple(string,)')
        self.assertRaises(ValueError, types.as_type, 'Tuple(,)')
        self.assertRaises(ValueError, types.as_type, 'Tuple(,string)')
        self.assertRaises(ValueError, types.as_type, 'Tuple(int,string))')

    def test_get_parameter_type(self):
        types = Types()
        self.assertEqual(
            float, types.get_parameter_type(0, 'float', []).python_type)
        self.assertEqual(
            'int32', types.get_parameter_type(0, 'int32', []).protobuf_type)
        self.assertEqual(
            'KRPC.Response',
            types.get_parameter_type(1, 'KRPC.Response', []).protobuf_type)
        class_parameter = types.get_parameter_type(
            0, 'uint64', ['ParameterType(0).Class(ServiceName.ClassName)'])
        self.assertEqual(
            types.as_type('Class(ServiceName.ClassName)'), class_parameter)
        self.assertTrue(isinstance(class_parameter, ClassType))
        self.assertTrue(issubclass(class_parameter.python_type, ClassBase))
        self.assertEqual(
            'Class(ServiceName.ClassName)', class_parameter.protobuf_type)
        self.assertEqual(
            'uint64',
            types.get_parameter_type(
                0, 'uint64',
                ['ParameterType(1).Class(ServiceName.ClassName)'])
            .protobuf_type)

    def test_get_return_type(self):
        types = Types()
        self.assertEqual(
            'float', types.get_return_type('float', []).protobuf_type)
        self.assertEqual(
            'int32', types.get_return_type('int32', []).protobuf_type)
        self.assertEqual(
            'KRPC.Response',
            types.get_return_type('KRPC.Response', []).protobuf_type)
        self.assertEqual(
            'Class(ServiceName.ClassName)',
            types.get_return_type(
                'uint64',
                ['ReturnType.Class(ServiceName.ClassName)']).protobuf_type)

    def test_coerce_to(self):
        types = Types()
        cases = [
            (42.0, 42, 'double'),
            (42.0, 42, 'float'),
            (42, 42.0, 'int32'),
            (42, 42L, 'int32'),
            (42L, 42.0, 'int64'),
            (42L, 42, 'int64'),
            (42, 42.0, 'uint32'),
            (42, 42L, 'uint32'),
            (42L, 42.0, 'uint64'),
            (42L, 42, 'uint64'),
            (list(), tuple(), 'List(string)'),
            ((0, 1, 2), [0, 1, 2], 'Tuple(int32,int32,int32)'),
            ([0, 1, 2], (0, 1, 2), 'List(int32)'),
        ]
        for expected, value, typ in cases:
            coerced_value = types.coerce_to(value, types.as_type(typ))
            self.assertEqual(expected, coerced_value)
            self.assertEqual(type(expected), type(coerced_value))

        strings = [
            u'foo',
            u'\xe2\x84\xa2',
            u'Mystery Goo\xe2\x84\xa2 Containment Unit'
        ]
        for string in strings:
            self.assertEqual(
                string, types.coerce_to(string, types.as_type('string')))

        self.assertEqual(
            ['foo', 'bar'],
            types.coerce_to(['foo', 'bar'], types.as_type('List(string)')))

        self.assertRaises(ValueError, types.coerce_to,
                          None, types.as_type('float'))
        self.assertRaises(ValueError, types.coerce_to,
                          '', types.as_type('float'))
        self.assertRaises(ValueError, types.coerce_to,
                          True, types.as_type('float'))

        self.assertRaises(ValueError, types.coerce_to,
                          list(), types.as_type('Tuple(int32)'))
        self.assertRaises(ValueError, types.coerce_to,
                          ['foo', 2], types.as_type('Tuple(string)'))
        self.assertRaises(ValueError, types.coerce_to,
                          [1], types.as_type('Tuple(string)'))

if __name__ == '__main__':
    unittest.main()
