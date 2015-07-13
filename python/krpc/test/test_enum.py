import unittest
import krpc.test.Test as TestSchema

class TestEnum(unittest.TestCase):

    def test_enums(self):
        self.assertEqual(1, TestSchema.a)
        self.assertEqual(2, TestSchema.b)
        self.assertEqual(3, TestSchema.c)

if __name__ == '__main__':
    unittest.main()
