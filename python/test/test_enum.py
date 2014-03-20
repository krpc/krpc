#!/usr/bin/env python2

import unittest
import krpc
import schema.Test


class TestEnum(unittest.TestCase):

    def test_enums(self):
        self.assertEqual(1, schema.Test.a)
        self.assertEqual(2, schema.Test.b)
        self.assertEqual(3, schema.Test.c)


if __name__ == '__main__':
    unittest.main()
