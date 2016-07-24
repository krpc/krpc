import unittest
from krpc.test.servertestcase import ServerTestCase


class TestObjects(ServerTestCase, unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        super(TestObjects, cls).setUpClass()

    def test_equality(self):
        obj1 = self.conn.test_service.create_test_object('jeb')
        obj2 = self.conn.test_service.create_test_object('jeb')
        self.assertTrue(obj1 == obj2)
        self.assertFalse(obj1 != obj2)

        obj3 = self.conn.test_service.create_test_object('bob')
        self.assertFalse(obj1 == obj3)
        self.assertTrue(obj1 != obj3)

        self.conn.test_service.object_property = obj1
        obj1a = self.conn.test_service.object_property
        self.assertEqual(obj1, obj1a)

        self.assertFalse(obj1.__eq__(None))
        self.assertTrue(obj1.__ne__(None))

    def test_hash(self):
        obj1 = self.conn.test_service.create_test_object('jeb')
        obj2 = self.conn.test_service.create_test_object('jeb')
        obj3 = self.conn.test_service.create_test_object('bob')
        self.assertEqual(obj1._object_id, hash(obj1))
        self.assertEqual(obj2._object_id, hash(obj2))
        self.assertNotEqual(obj1._object_id, hash(obj3))
        self.assertEqual(hash(obj1), hash(obj2))
        self.assertNotEqual(hash(obj1), hash(obj3))

        self.conn.test_service.object_property = obj1
        obj1a = self.conn.test_service.object_property
        self.assertEqual(hash(obj1), hash(obj1a))

    def test_sorting(self):
        obj1 = self.conn.test_service.create_test_object('object_sorting_1')
        obj2 = self.conn.test_service.create_test_object('object_sorting_2')
        obj3 = self.conn.test_service.create_test_object('object_sorting_3')
        self.assertEqual([obj1, obj2, obj3], sorted([obj2, obj3, obj1]))
        self.assertTrue(obj1 < obj2)
        self.assertTrue(obj2 < obj3)
        self.assertTrue(obj1 <= obj1)
        self.assertTrue(obj1 <= obj2)
        self.assertTrue(obj2 <= obj3)
        self.assertTrue(obj2 > obj1)
        self.assertTrue(obj3 > obj2)
        self.assertTrue(obj2 >= obj1)
        self.assertTrue(obj3 >= obj2)
        self.assertTrue(obj1 >= obj1)

    def test_memory_allocation(self):
        obj1 = self.conn.test_service.create_test_object('jeb')
        obj2 = self.conn.test_service.create_test_object('jeb')
        obj3 = self.conn.test_service.create_test_object('bob')
        self.assertEqual(obj1._object_id, obj2._object_id)
        self.assertNotEqual(obj1._object_id, obj3._object_id)


if __name__ == '__main__':
    unittest.main()
