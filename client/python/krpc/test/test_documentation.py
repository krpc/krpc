import unittest
from typing import cast
from krpc.test.servertestcase import ServerTestCase


class TestDocumentation(ServerTestCase, unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        super(TestDocumentation, cls).setUpClass()

    def check_doc(self, expected: str, obj: object) -> None:
        doc = cast(str, obj.__doc__)
        self.assertEqual(expected, doc.strip())

    def test_basic(self) -> None:
        self.check_doc('Service documentation string.', self.conn.test_service)
        self.check_doc('Procedure documentation string.', self.conn.test_service.float_to_string)
        self.check_doc('Property documentation string.',
                       type(self.conn.test_service).string_property)
        self.check_doc('Class documentation string.', self.conn.test_service.TestClass)
        obj = self.conn.test_service.create_test_object('Jeb')
        self.check_doc('Method documentation string.', obj.get_value)
        self.check_doc('Property documentation string.',
                       self.conn.test_service.TestClass.int_property)
        self.check_doc('Enum documentation string.',
                       self.conn.test_service.TestEnum)
        self.check_doc('Enum ValueA documentation string.',
                       self.conn.test_service.TestEnum.value_a)
        self.check_doc('Enum ValueB documentation string.',
                       self.conn.test_service.TestEnum.value_b)
        self.check_doc('Enum ValueC documentation string.',
                       self.conn.test_service.TestEnum.value_c)


if __name__ == '__main__':
    unittest.main()
