#!/usr/bin/env python2

import unittest
import binascii
import subprocess
import time
import krpc

class TestObjects(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.server = subprocess.Popen(['bin/TestServer/TestServer.exe', '50001'])
        time.sleep(0.25)

    def setUp(self):
        self.ksp = krpc.connect(name='TestObjects', port=50001)

    @classmethod
    def tearDownClass(cls):
        cls.server.kill()

    def test_equality(self):
        obj1 = self.ksp.test_service.create_test_object('jeb')
        obj2 = self.ksp.test_service.create_test_object('jeb')
        self.assertTrue(obj1 == obj2)
        self.assertFalse(obj1 != obj2)

        obj3 = self.ksp.test_service.create_test_object('bob')
        self.assertFalse(obj1 == obj3)
        self.assertTrue(obj1 != obj3)

        self.ksp.test_service.object_property = obj1
        obj1a = self.ksp.test_service.object_property
        self.assertEqual(obj1, obj1a)

    def test_hash(self):
        obj1 = self.ksp.test_service.create_test_object('jeb')
        obj2 = self.ksp.test_service.create_test_object('jeb')
        obj3 = self.ksp.test_service.create_test_object('bob')
        self.assertEqual(obj1._object_id, hash(obj1))
        self.assertEqual(obj2._object_id, hash(obj2))
        self.assertNotEqual(obj1._object_id, hash(obj3))
        self.assertEqual(hash(obj1), hash(obj2))
        self.assertNotEqual(hash(obj1), hash(obj3))

        self.ksp.test_service.object_property = obj1
        obj1a = self.ksp.test_service.object_property
        self.assertEqual(hash(obj1), hash(obj1a))

    def test_memory_allocation(self):
        obj1 = self.ksp.test_service.create_test_object('jeb')
        obj2 = self.ksp.test_service.create_test_object('jeb')
        obj3 = self.ksp.test_service.create_test_object('bob')
        self.assertEquals (obj1._object_id, obj2._object_id)
        self.assertNotEquals (obj1._object_id, obj3._object_id)


if __name__ == '__main__':
    unittest.main()
