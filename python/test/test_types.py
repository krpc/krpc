#!/usr/bin/env python2

import unittest
from krpc import _Types as Types
import proto.KRPC

class TestTypes(unittest.TestCase):

    def test_is_value_type(self):
        self.assertTrue(Types.is_value_type(float))
        self.assertTrue(Types.is_value_type(int))
        self.assertTrue(Types.is_value_type(long))
        self.assertTrue(Types.is_value_type(bool))
        self.assertTrue(Types.is_value_type(str))
        self.assertFalse(Types.is_value_type(type))
        self.assertFalse(Types.is_value_type(type(TestTypes)))
        self.assertFalse(Types.is_value_type('int'))
        self.assertFalse(Types.is_value_type('int32'))
        self.assertFalse(Types.is_value_type(proto.KRPC.Request))
        self.assertFalse(Types.is_value_type(proto.KRPC.Response))
        self.assertFalse(Types.is_value_type(None))

    def test_is_message_type(self):
        self.assertTrue(Types.is_message_type(proto.KRPC.Request))
        self.assertTrue(Types.is_message_type(proto.KRPC.Response))
        self.assertFalse(Types.is_message_type(float))
        self.assertFalse(Types.is_message_type(int))
        self.assertFalse(Types.is_message_type(long))
        self.assertFalse(Types.is_message_type(bool))
        self.assertFalse(Types.is_message_type(str))
        self.assertFalse(Types.is_message_type(type))
        self.assertFalse(Types.is_message_type(type(TestTypes)))
        self.assertFalse(Types.is_message_type('int'))
        self.assertFalse(Types.is_message_type('int32'))
        self.assertFalse(Types.is_message_type(None))

    def test_is_valid_type(self):
        self.assertTrue(Types.is_valid_type(float))
        self.assertTrue(Types.is_valid_type(int))
        self.assertFalse(Types.is_valid_type(type))
        self.assertFalse(Types.is_valid_type(type(TestTypes)))
        self.assertFalse(Types.is_valid_type('int'))
        self.assertFalse(Types.is_valid_type('int32'))
        self.assertFalse(Types.is_valid_type(None))

    def test_as_python_type(self):
        self.assertEqual(float, Types.as_python_type('float'))
        self.assertEqual(int, Types.as_python_type('int32'))
        self.assertEqual(long, Types.as_python_type('int64'))
        self.assertEqual(bool, Types.as_python_type('bool'))
        self.assertEqual(str, Types.as_python_type('string'))
        self.assertEqual(proto.KRPC.Response, Types.as_python_type('KRPC.Response'))
        self.assertEqual(proto.KRPC.Request, Types.as_python_type('KRPC.Request'))
        self.assertRaises(TypeError, Types.as_python_type, None)
        self.assertRaises(TypeError, Types.as_python_type, '')
        self.assertRaises(TypeError, Types.as_python_type, 'TestTypes')
        self.assertRaises(TypeError, Types.as_python_type, float)
        self.assertRaises(TypeError, Types.as_python_type, proto.KRPC.Response)

    def test_as_protobuf_type(self):
        self.assertEqual('float', Types.as_protobuf_type(float))
        self.assertEqual('int32', Types.as_protobuf_type(int))
        self.assertEqual('int64', Types.as_protobuf_type(long))
        self.assertEqual('bool', Types.as_protobuf_type(bool))
        self.assertEqual('string', Types.as_protobuf_type(str))
        self.assertEqual('KRPC.Response', Types.as_protobuf_type(proto.KRPC.Response))
        self.assertEqual('KRPC.Request', Types.as_protobuf_type(proto.KRPC.Request))
        self.assertRaises(TypeError, Types.as_protobuf_type, None)
        self.assertRaises(TypeError, Types.as_protobuf_type, '')
        self.assertRaises(TypeError, Types.as_protobuf_type, type)
        self.assertRaises(TypeError, Types.as_protobuf_type, TestTypes)
        self.assertRaises(TypeError, Types.as_protobuf_type, 'KRPC.Response')

if __name__ == '__main__':
    unittest.main()
