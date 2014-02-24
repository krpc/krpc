#!/usr/bin/env python2

import unittest
import binascii
import krpc

class TestClient(unittest.TestCase):

    def setUp(self):
        self.ksp = krpc.connect(name='TestClient')

    def test_value_parameters(self):
        self.assertEqual('3.14159', self.ksp.TestService.FloatToString(float(3.14159)))
        self.assertEqual('42', self.ksp.TestService.Int32ToString(42))
        self.assertEqual('123456789000', self.ksp.TestService.Int64ToString(123456789000L))
        self.assertEqual('True', self.ksp.TestService.BoolToString(True))
        self.assertEqual('False', self.ksp.TestService.BoolToString(False))
        self.assertEqual(12345, self.ksp.TestService.StringToInt32('12345'))

    def test_multiple_value_parameters(self):
        self.assertEqual('3.14159', self.ksp.TestService.AddMultipleValues(0.14159, 1, 2))

    def test_auto_value_type_conversion(self):
        self.assertEqual('42', self.ksp.TestService.FloatToString(42))
        self.assertEqual('42', self.ksp.TestService.FloatToString(42L))
        self.assertEqual('6', self.ksp.TestService.AddMultipleValues(1L, 2L, 3L))
        self.assertRaises(ValueError, self.ksp.TestService.FloatToString, '42')

    def test_incorrect_parameter_type(self):
        self.assertRaises(ValueError, self.ksp.TestService.FloatToString, 'foo')
        self.assertRaises(ValueError, self.ksp.TestService.AddMultipleValues, 0.14159, 'foo', 2)

    def test_properties(self):
        self.ksp.TestService.StringProperty = 'foo';
        self.assertEqual('foo', self.ksp.TestService.StringProperty)
        self.assertEqual('foo', self.ksp.TestService.StringPropertyPrivateSet)
        self.ksp.TestService.StringPropertyPrivateGet = 'foo'

if __name__ == '__main__':
    unittest.main()
