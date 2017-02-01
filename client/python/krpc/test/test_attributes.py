import unittest
from krpc.attributes import Attributes


class TestAttributes(unittest.TestCase):

    cases = [
        'ProcedureName',
        'get_PropertyName',
        'set_PropertyName',
        'ClassName_MethodName',
        'ClassName_static_StaticMethodName',
        'ClassName_get_PropertyName',
        'ClassName_set_PropertyName'
    ]

    def check(self, method, *returnsTrue):
        for case in self.cases:
            self.assertEquals(case in returnsTrue, method(case))

    def test_is_a_procedure(self):
        self.check(Attributes.is_a_procedure, 'ProcedureName')

    def test_is_a_property_accessor(self):
        self.check(Attributes.is_a_property_accessor,
                   'get_PropertyName', 'set_PropertyName')

    def test_is_a_property_getter(self):
        self.check(Attributes.is_a_property_getter, 'get_PropertyName')

    def test_is_a_property_setter(self):
        self.check(Attributes.is_a_property_setter, 'set_PropertyName')

    def test_is_a_class_member(self):
        self.check(Attributes.is_a_class_member,
                   'ClassName_MethodName', 'ClassName_static_StaticMethodName',
                   'ClassName_get_PropertyName', 'ClassName_set_PropertyName')

    def test_is_a_class_method(self):
        self.check(Attributes.is_a_class_method, 'ClassName_MethodName')

    def test_is_a_class_static_method(self):
        self.check(Attributes.is_a_class_static_method,
                   'ClassName_static_StaticMethodName')

    def test_is_a_class_property_accessor(self):
        self.check(Attributes.is_a_class_property_accessor,
                   'ClassName_get_PropertyName', 'ClassName_set_PropertyName')

    def test_is_a_class_property_getter(self):
        self.check(Attributes.is_a_class_property_getter,
                   'ClassName_get_PropertyName')

    def test_is_a_class_property_setter(self):
        self.check(Attributes.is_a_class_property_setter,
                   'ClassName_set_PropertyName')

    def test_get_property_name(self):
        self.assertRaises(ValueError,
                          Attributes.get_property_name, 'ProcedureName')
        self.assertEqual('PropertyName',
                         Attributes.get_property_name('get_PropertyName'))
        self.assertEqual('PropertyName',
                         Attributes.get_property_name('set_PropertyName'))
        self.assertRaises(ValueError,
                          Attributes.get_property_name, 'ClassName_MethodName')
        self.assertRaises(ValueError,
                          Attributes.get_property_name,
                          'ClassName_StaticMethodName')
        self.assertRaises(ValueError,
                          Attributes.get_property_name,
                          'ClassName_get_PropertyName)')
        self.assertRaises(ValueError,
                          Attributes.get_property_name,
                          'ClassName_set_PropertyName)')

    def test_get_class_name(self):
        self.assertRaises(ValueError,
                          Attributes.get_class_name, 'ProcedureName')
        self.assertRaises(ValueError,
                          Attributes.get_class_name, 'get_PropertyName')
        self.assertRaises(ValueError,
                          Attributes.get_class_name, 'set_PropertyName')
        self.assertEqual('ClassName',
                         Attributes.get_class_name('ClassName_MethodName'))
        self.assertEqual('ClassName',
                         Attributes.get_class_name(
                             'ClassName_StaticMethodName'))
        self.assertEqual('ClassName',
                         Attributes.get_class_name(
                             'ClassName_get_PropertyName)'))
        self.assertEqual('ClassName',
                         Attributes.get_class_name(
                             'ClassName_set_PropertyName)'))

    def test_get_class_member_name(self):
        self.assertRaises(ValueError,
                          Attributes.get_class_member_name, 'ProcedureName')
        self.assertRaises(ValueError,
                          Attributes.get_class_member_name, 'get_PropertyName')
        self.assertRaises(ValueError,
                          Attributes.get_class_member_name, 'set_PropertyName')
        self.assertEqual('MethodName',
                         Attributes.get_class_member_name(
                             'ClassName_MethodName'))
        self.assertEqual('StaticMethodName',
                         Attributes.get_class_member_name(
                             'ClassName_StaticMethodName'))
        self.assertEqual('PropertyName',
                         Attributes.get_class_member_name(
                             'ClassName_get_PropertyName'))
        self.assertEqual('PropertyName',
                         Attributes.get_class_member_name(
                             'ClassName_set_PropertyName'))

if __name__ == '__main__':
    unittest.main()
