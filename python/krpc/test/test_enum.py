#!/usr/bin/env python2

import unittest
import krpc.schema.Test


class TestEnum(unittest.TestCase):

    def test_enums(self):
        self.assertEqual(1, krpc.schema.Test.a)
        self.assertEqual(2, krpc.schema.Test.b)
        self.assertEqual(3, krpc.schema.Test.c)


if __name__ == '__main__':
    unittest.main()
