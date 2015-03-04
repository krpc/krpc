#!/usr/bin/env python2

import unittest
import subprocess
import time
import krpc
import krpc.test.Test as TestSchema
from krpc.stream import Stream, stream, add_stream

class TestStreaming(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.server = subprocess.Popen(['bin/TestServer/TestServer.exe', '50001', '50002'])
        time.sleep(0.25)

    def setUp(self):
        self.conn = krpc.connect(name='TestClient', rpc_port=50001, stream_port=50002)

    @classmethod
    def tearDownClass(cls):
        cls.server.kill()

    def test_add_stream(self):
        s = Stream(self.conn.test_service.float_to_string, 3.14159)
        print s.request
        stream_id = self.conn.krpc.add_stream(s.request)
        print stream_id
        self.conn.krpc.remove_stream(stream_id)

    def test_method(self):
        with stream(self.conn.test_service.float_to_string, 3.14159) as x:
            self.assertEqual('3.14159', x())
            self.assertEqual('3.14159', x())
            self.assertEqual('3.14159', x())
            self.assertEqual('3.14159', x())
            self.assertEqual('3.14159', x())

    def test_property(self):
        self.conn.test_service.string_property = 'foo'
        with stream(getattr, self.conn.test_service, 'string_property') as x:
            self.assertEqual('foo', x())
            self.assertEqual('foo', x())
            self.assertEqual('foo', x())
            self.assertEqual('foo', x())
            self.assertEqual('foo', x())

    def test_class_method(self):
        obj = self.conn.test_service.create_test_object('bob')
        with stream(obj.float_to_string, 3.14159) as x:
            self.assertEqual('bob3.14159', x())
            self.assertEqual('bob3.14159', x())
            self.assertEqual('bob3.14159', x())
            self.assertEqual('bob3.14159', x())
            self.assertEqual('bob3.14159', x())

    def test_class_property(self):
        obj = self.conn.test_service.create_test_object('jeb')
        obj.int_property = 42
        with stream(getattr, obj, 'int_property') as x:
            self.assertEqual(42, x())
            self.assertEqual(42, x())
            self.assertEqual(42, x())
            self.assertEqual(42, x())
            self.assertEqual(42, x())
        self.assertEqual(42, obj.int_property)

    def test_property_setters_are_invalid(self):
        self.assertRaises(ValueError, add_stream, setattr, self.conn.test_service, 'string_property')
        obj = self.conn.test_service.create_test_object('bill')
        self.assertRaises(ValueError, add_stream, setattr, obj.int_property, 42)

if __name__ == '__main__':
    unittest.main()
