#!/usr/bin/env python2

import unittest
import binascii
import subprocess
import time
import krpc

class TestClient(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.server = subprocess.Popen(['bin/TestServer/TestServer.exe'], stdout=subprocess.PIPE)
        time.sleep(0.25)

    def setUp(self):
        self.ksp = krpc.connect(name='TestClient')

    @classmethod
    def tearDownClass(cls):
        cls.server.kill()

    def test_value_parameters(self):
        self.assertEqual('3.14159', self.ksp.TestService.FloatToString(float(3.14159)))
        self.assertEqual('3.14159', self.ksp.TestService.DoubleToString(float(3.14159)))
        self.assertEqual('42', self.ksp.TestService.Int32ToString(42))
        self.assertEqual('123456789000', self.ksp.TestService.Int64ToString(123456789000L))
        self.assertEqual('True', self.ksp.TestService.BoolToString(True))
        self.assertEqual('False', self.ksp.TestService.BoolToString(False))
        self.assertEqual(12345, self.ksp.TestService.StringToInt32('12345'))
        self.assertEqual('deadbeef', self.ksp.TestService.BytesToHexString(b'\xde\xad\xbe\xef'))

    def test_multiple_value_parameters(self):
        self.assertEqual('3.14159', self.ksp.TestService.AddMultipleValues(0.14159, 1, 2))

    def test_auto_value_type_conversion(self):
        self.assertEqual('42', self.ksp.TestService.FloatToString(42))
        self.assertEqual('42', self.ksp.TestService.FloatToString(42L))
        self.assertEqual('6', self.ksp.TestService.AddMultipleValues(1L, 2L, 3L))
        self.assertRaises(TypeError, self.ksp.TestService.FloatToString, '42')

    def test_incorrect_parameter_type(self):
        self.assertRaises(TypeError, self.ksp.TestService.FloatToString, 'foo')
        self.assertRaises(TypeError, self.ksp.TestService.AddMultipleValues, 0.14159, 'foo', 2)

    def test_properties(self):
        self.ksp.TestService.StringProperty = 'foo';
        self.assertEqual('foo', self.ksp.TestService.StringProperty)
        self.assertEqual('foo', self.ksp.TestService.StringPropertyPrivateSet)
        self.ksp.TestService.StringPropertyPrivateGet = 'foo'
        obj = self.ksp.TestService.CreateTestObject('bar')
        self.ksp.TestService.ObjectProperty = obj
        self.assertEqual (obj, self.ksp.TestService.ObjectProperty)

    def test_class_as_return_value(self):
        obj = self.ksp.TestService.CreateTestObject('jeb')
        self.assertEqual('TestClass', type(obj).__name__)

    def test_class_none_value(self):
        self.assertIsNone(self.ksp.TestService.EchoTestObject(None))
        obj = self.ksp.TestService.CreateTestObject('bob')
        self.assertEqual('bobnull', obj.ObjectToString(None))
        self.assertIsNone (self.ksp.TestService.ObjectProperty)
        self.ksp.TestService.ObjectProperty = None
        self.assertIsNone (self.ksp.TestService.ObjectProperty)

    def test_class_methods(self):
        obj = self.ksp.TestService.CreateTestObject('bob')
        self.assertEqual('value=bob', obj.GetValue())
        self.assertEqual('bob3.14159', obj.FloatToString(3.14159))
        obj2 = self.ksp.TestService.CreateTestObject('bill')
        self.assertEqual('bobbill', obj.ObjectToString(obj2))

    def test_class_properties(self):
        obj = self.ksp.TestService.CreateTestObject('jeb')
        self.assertEqual(0, obj.IntProperty)
        obj.IntProperty = 42
        self.assertEqual(42, obj.IntProperty)
        obj2 = self.ksp.TestService.CreateTestObject('kermin')
        obj.ObjectProperty = obj2
        self.assertEqual(obj2._object_id, obj.ObjectProperty._object_id)

    def test_setattr_for_properties(self):
        """ Check that properties are added to the dynamically generated service class,
            not the base class krpc.Service """
        self.assertRaises (AttributeError, getattr, krpc.service._Service, 'ObjectProperty')
        self.assertIsNotNone (getattr(self.ksp.TestService, 'ObjectProperty'))

    def test_optional_arguments(self):
        self.assertEqual('jebfoobarbaz', self.ksp.TestService.OptionalArguments('jeb'))
        self.assertEqual('jebbobbillbaz', self.ksp.TestService.OptionalArguments('jeb', 'bob', 'bill'))

    def test_named_parameters(self):
        self.assertEqual('1234', self.ksp.TestService.OptionalArguments(x='1', y='2', z='3', w='4'))
        self.assertEqual('2413', self.ksp.TestService.OptionalArguments(z='1', x='2', w='3', y='4'))
        self.assertEqual('1243', self.ksp.TestService.OptionalArguments('1', '2', w='3', z='4'))
        self.assertEqual('123baz', self.ksp.TestService.OptionalArguments('1', '2', z='3'))
        self.assertEqual('12bar3', self.ksp.TestService.OptionalArguments('1', '2', w='3'))
        self.assertRaises(TypeError, self.ksp.TestService.OptionalArguments, '1', '2', '3', '4', w='5')
        self.assertRaises(TypeError, self.ksp.TestService.OptionalArguments, '1', '2', '3', y='4')
        self.assertRaises(TypeError, self.ksp.TestService.OptionalArguments, '1', foo='4')

        obj = self.ksp.TestService.CreateTestObject('jeb')
        self.assertEqual('1234', obj.OptionalArguments(x='1', y='2', z='3', w='4'))
        self.assertEqual('2413', obj.OptionalArguments(z='1', x='2', w='3', y='4'))
        self.assertEqual('1243', obj.OptionalArguments('1', '2', w='3', z='4'))
        self.assertEqual('123baz', obj.OptionalArguments('1', '2', z='3'))
        self.assertEqual('12bar3', obj.OptionalArguments('1', '2', w='3'))
        self.assertRaises(TypeError, obj.OptionalArguments, '1', '2', '3', '4', w='5')
        self.assertRaises(TypeError, obj.OptionalArguments, '1', '2', '3', y='4')
        self.assertRaises(TypeError, obj.OptionalArguments, '1', foo='4')

    def test_too_many_arguments(self):
        self.assertRaises(TypeError, self.ksp.TestService.OptionalArguments, '1', '2', '3', '4', '5')
        obj = self.ksp.TestService.CreateTestObject('jeb')
        self.assertRaises(TypeError, obj.OptionalArguments, '1', '2', '3', '4', '5')

if __name__ == '__main__':
    unittest.main()
