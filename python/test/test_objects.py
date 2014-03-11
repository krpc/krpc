#!/usr/bin/env python2

import unittest
import binascii
import krpc

class TestObjects(unittest.TestCase):

    def setUp(self):
        self.ksp = krpc.connect(name='TestObjects')

    def test_equality(self):
        obj1 = self.ksp.TestService.CreateTestObject('jeb')
        obj2 = self.ksp.TestService.CreateTestObject('jeb')
        self.assertNotEqual(obj1, obj2)

        self.ksp.TestService.ObjectProperty = obj1
        obj1a = self.ksp.TestService.ObjectProperty
        self.assertEqual(obj1, obj1a)

    def test_hash(self):
        obj1 = self.ksp.TestService.CreateTestObject('jeb')
        obj2 = self.ksp.TestService.CreateTestObject('jeb')
        self.assertEqual(obj1._object_id, hash(obj1))
        self.assertEqual(obj2._object_id, hash(obj2))
        self.assertNotEqual(hash(obj1), hash(obj2))

        self.ksp.TestService.ObjectProperty = obj1
        obj1a = self.ksp.TestService.ObjectProperty
        self.assertEqual(hash(obj1), hash(obj1a))


if __name__ == '__main__':
    unittest.main()
