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

    def test_incorrect_parameter_type(self):
        self.ksp.TestService.FloatToString(42)

if __name__ == '__main__':
    unittest.main()
