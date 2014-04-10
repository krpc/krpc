#!/usr/bin/env python2

import unittest
import binascii
import subprocess
import time
import krpc

class TestObjects(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.server = subprocess.Popen(['bin/TestServer/TestServer.exe'])
        time.sleep(0.25)

    def setUp(self):
        self.ksp = krpc.connect(name='TestObjects')

    @classmethod
    def tearDownClass(cls):
        cls.server.kill()

    def test_equality(self):
        obj1 = self.ksp.test_service.create_test_object('jeb')
        obj2 = self.ksp.test_service.create_test_object('jeb')
        self.assertNotEqual(obj1, obj2)

        self.ksp.test_service.object_property = obj1
        obj1a = self.ksp.test_service.object_property
        self.assertEqual(obj1, obj1a)

    def test_hash(self):
        obj1 = self.ksp.test_service.create_test_object('jeb')
        obj2 = self.ksp.test_service.create_test_object('jeb')
        self.assertEqual(obj1._object_id, hash(obj1))
        self.assertEqual(obj2._object_id, hash(obj2))
        self.assertNotEqual(hash(obj1), hash(obj2))

        self.ksp.test_service.object_property = obj1
        obj1a = self.ksp.test_service.object_property
        self.assertEqual(hash(obj1), hash(obj1a))


if __name__ == '__main__':
    unittest.main()
