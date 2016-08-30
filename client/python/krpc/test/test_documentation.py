import unittest
from krpc.test.servertestcase import ServerTestCase


class TestDocumentation(ServerTestCase, unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        super(TestDocumentation, cls).setUpClass()

    def test_basic(self):
        # TODO: fix tests
        self.assertEqual('Service documentation string.', self.conn.test_service.__doc__)
        self.assertEqual('Procedure documentation string.', self.conn.test_service.float_to_string.__doc__)
        # self.assertEqual('Property documentation string.', self.conn.test_service.string_property.__doc__)
        self.assertEqual('Class documentation string.', self.conn.test_service.TestClass.__doc__)
        obj = self.conn.test_service.create_test_object('Jeb')
        self.assertEqual('Method documentation string.', obj.get_value.__doc__)
        # self.assertEqual('Property documentation string.', obj.int_property.__doc__)
        self.assertEqual('Enum documentation string.', self.conn.test_service.TestEnum.__doc__)
        self.assertEqual('Enum ValueA documentation string.', self.conn.test_service.TestEnum.value_a.__doc__)
        self.assertEqual('Enum ValueB documentation string.', self.conn.test_service.TestEnum.value_b.__doc__)
        self.assertEqual('Enum ValueC documentation string.', self.conn.test_service.TestEnum.value_c.__doc__)


if __name__ == '__main__':
    unittest.main()
