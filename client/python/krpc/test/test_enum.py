import unittest
import krpc.test.schema.Test as TestSchema

class TestEnum(unittest.TestCase):

    def test_enums(self):
        self.assertEqual(0, TestSchema.a)
        self.assertEqual(1, TestSchema.b)
        self.assertEqual(2, TestSchema.c)

if __name__ == '__main__':
    unittest.main()
