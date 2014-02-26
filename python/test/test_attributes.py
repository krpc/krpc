#!/usr/bin/env python2

import unittest
from krpc import _Attributes as Attributes

class TestTypes(unittest.TestCase):

    def test_is_a_procedure(self):
        self.assertTrue(Attributes.is_a_procedure([]))
        self.assertFalse(Attributes.is_a_procedure(['Property.Get(PropertyName)']))
        self.assertFalse(Attributes.is_a_procedure(['Property.Set(PropertyName)']))
        self.assertFalse(Attributes.is_a_procedure(['Class.Method(ClassName,MethodName)']))
        self.assertFalse(Attributes.is_a_procedure(['Class.Property.Get(ClassName,PropertyName)']))
        self.assertFalse(Attributes.is_a_procedure(['Class.Property.Set(ClassName,PropertyName)']))

    def test_is_a_property_accessor(self):
        self.assertFalse(Attributes.is_a_property_accessor([]))
        self.assertTrue(Attributes.is_a_property_accessor(['Property.Get(PropertyName)']))
        self.assertTrue(Attributes.is_a_property_accessor(['Property.Set(PropertyName)']))
        self.assertFalse(Attributes.is_a_property_accessor(['Class.Method(ClassName,MethodName)']))
        self.assertFalse(Attributes.is_a_property_accessor(['Class.Property.Get(ClassName,PropertyName)']))
        self.assertFalse(Attributes.is_a_property_accessor(['Class.Property.Set(ClassName,PropertyName)']))

    def test_is_a_property_getter(self):
        self.assertFalse(Attributes.is_a_property_getter([]))
        self.assertTrue(Attributes.is_a_property_getter(['Property.Get(PropertyName)']))
        self.assertFalse(Attributes.is_a_property_getter(['Property.Set(PropertyName)']))
        self.assertFalse(Attributes.is_a_property_getter(['Class.Method(ClassName,MethodName)']))
        self.assertFalse(Attributes.is_a_property_getter(['Class.Property.Get(ClassName,PropertyName)']))
        self.assertFalse(Attributes.is_a_property_getter(['Class.Property.Set(ClassName,PropertyName)']))

    def test_is_a_property_setter(self):
        self.assertFalse(Attributes.is_a_property_setter([]))
        self.assertFalse(Attributes.is_a_property_setter(['Property.Get(PropertyName)']))
        self.assertTrue(Attributes.is_a_property_setter(['Property.Set(PropertyName)']))
        self.assertFalse(Attributes.is_a_property_setter(['Class.Method(ClassName,MethodName)']))
        self.assertFalse(Attributes.is_a_property_setter(['Class.Property.Get(ClassName,PropertyName)']))
        self.assertFalse(Attributes.is_a_property_setter(['Class.Property.Set(ClassName,PropertyName)']))

    def test_is_a_class_method(self):
        self.assertFalse(Attributes.is_a_class_method([]))
        self.assertFalse(Attributes.is_a_class_method(['Property.Get(PropertyName)']))
        self.assertFalse(Attributes.is_a_class_method(['Property.Set(PropertyName)']))
        self.assertTrue(Attributes.is_a_class_method(['Class.Method(ClassName,MethodName)']))
        self.assertFalse(Attributes.is_a_class_method(['Class.Property.Get(ClassName,PropertyName)']))
        self.assertFalse(Attributes.is_a_class_method(['Class.Property.Set(ClassName,PropertyName)']))

    def test_is_a_class_property_accessor(self):
        self.assertFalse(Attributes.is_a_class_property_accessor([]))
        self.assertFalse(Attributes.is_a_class_property_accessor(['Property.Get(PropertyName)']))
        self.assertFalse(Attributes.is_a_class_property_accessor(['Property.Set(PropertyName)']))
        self.assertFalse(Attributes.is_a_class_property_accessor(['Class.Method(ClassName,MethodName)']))
        self.assertTrue(Attributes.is_a_class_property_accessor(['Class.Property.Get(ClassName,PropertyName)']))
        self.assertTrue(Attributes.is_a_class_property_accessor(['Class.Property.Set(ClassName,PropertyName)']))

    def test_is_a_class_property_getter(self):
        self.assertFalse(Attributes.is_a_class_property_getter([]))
        self.assertFalse(Attributes.is_a_class_property_getter(['Property.Get(PropertyName)']))
        self.assertFalse(Attributes.is_a_class_property_getter(['Property.Set(PropertyName)']))
        self.assertFalse(Attributes.is_a_class_property_getter(['Class.Method(ClassName,MethodName)']))
        self.assertTrue(Attributes.is_a_class_property_getter(['Class.Property.Get(ClassName,PropertyName)']))
        self.assertFalse(Attributes.is_a_class_property_getter(['Class.Property.Set(ClassName,PropertyName)']))

    def test_is_a_class_property_setter(self):
        self.assertFalse(Attributes.is_a_class_property_setter([]))
        self.assertFalse(Attributes.is_a_class_property_setter(['Property.Get(PropertyName)']))
        self.assertFalse(Attributes.is_a_class_property_setter(['Property.Set(PropertyName)']))
        self.assertFalse(Attributes.is_a_class_property_setter(['Class.Method(ClassName,MethodName)']))
        self.assertFalse(Attributes.is_a_class_property_setter(['Class.Property.Get(ClassName,PropertyName)']))
        self.assertTrue(Attributes.is_a_class_property_setter(['Class.Property.Set(ClassName,PropertyName)']))

    def test_get_property_name(self):
        self.assertRaises(ValueError, Attributes.get_property_name, [])
        self.assertEqual('PropertyName', Attributes.get_property_name(['Property.Get(PropertyName)']))
        self.assertEqual('PropertyName', Attributes.get_property_name(['Property.Set(PropertyName)']))
        self.assertRaises(ValueError, Attributes.get_property_name, ['Class.Method(ClassName,MethodName)'])
        self.assertRaises(ValueError, Attributes.get_property_name, ['Class.Property.Get(ClassName,PropertyName)'])
        self.assertRaises(ValueError, Attributes.get_property_name, ['Class.Property.Set(ClassName,PropertyName)'])

    def test_get_class_name(self):
        self.assertRaises(ValueError, Attributes.get_class_name, [])
        self.assertRaises(ValueError, Attributes.get_class_name, ['Property.Get(PropertyName)'])
        self.assertRaises(ValueError, Attributes.get_class_name, ['Property.Set(PropertyName)'])
        self.assertEqual('ClassName', Attributes.get_class_name(['Class.Method(ClassName,MethodName)']))
        self.assertEqual('ClassName', Attributes.get_class_name(['Class.Property.Get(ClassName,PropertyName)']))
        self.assertEqual('ClassName', Attributes.get_class_name(['Class.Property.Set(ClassName,PropertyName)']))

    def test_get_class_method_name(self):
        self.assertRaises(ValueError, Attributes.get_class_method_name, [])
        self.assertRaises(ValueError, Attributes.get_class_method_name, ['Property.Get(PropertyName)'])
        self.assertRaises(ValueError, Attributes.get_class_method_name, ['Property.Set(PropertyName)'])
        self.assertEqual('MethodName', Attributes.get_class_method_name(['Class.Method(ClassName,MethodName)']))
        self.assertRaises(ValueError, Attributes.get_class_method_name, ['Class.Property.Get(ClassName,PropertyName)'])
        self.assertRaises(ValueError, Attributes.get_class_method_name, ['Class.Property.Set(ClassName,PropertyName)'])

    def test_get_class_property_name(self):
        self.assertRaises(ValueError, Attributes.get_class_property_name, [])
        self.assertRaises(ValueError, Attributes.get_class_property_name, ['Property.Get(PropertyName)'])
        self.assertRaises(ValueError, Attributes.get_class_property_name, ['Property.Set(PropertyName)'])
        self.assertRaises(ValueError, Attributes.get_class_property_name, ['Class.Method(ClassName,MethodName)'])
        self.assertEqual('PropertyName', Attributes.get_class_property_name(['Class.Property.Get(ClassName,PropertyName)']))
        self.assertEqual('PropertyName', Attributes.get_class_property_name(['Class.Property.Set(ClassName,PropertyName)']))

    def test_get_return_type_attributes(self):
        self.assertEqual([], Attributes.get_return_type_attrs([]))
        self.assertEqual([], Attributes.get_return_type_attrs(['Class.Method(ClassName,MethodName)']))
        self.assertEqual(['Class(ClassName)'], Attributes.get_return_type_attrs(['ReturnType.Class(ClassName)']))
        self.assertEqual(['Class(ClassName)'], Attributes.get_return_type_attrs(['Class.Method(ClassName,MethodName)', 'ReturnType.Class(ClassName)']))

    def test_get_parameter_type_attributes(self):
        self.assertEqual([], Attributes.get_parameter_type_attrs(0, []))
        self.assertEqual([], Attributes.get_parameter_type_attrs(0, ['Class.Method(ClassName,MethodName)']))
        self.assertEqual([], Attributes.get_parameter_type_attrs(0, ['ReturnType.Class(ClassName)']))
        self.assertEqual([], Attributes.get_parameter_type_attrs(0, ['Class.Method(ClassName,MethodName)', 'ReturnType.Class(ClassName)']))
        self.assertEqual([], Attributes.get_parameter_type_attrs(1, ['ParameterType(2).Class(ClassName)']))
        self.assertEqual(['Class(ClassName)'], Attributes.get_parameter_type_attrs(2, ['ParameterType(2).Class(ClassName)']))
        self.assertEqual(['Class(ClassName2)'], Attributes.get_parameter_type_attrs(2, ['ParameterType(0).Class(ClassName1)', 'ParameterType(2).Class(ClassName2)']))
        self.assertEqual(['Class(ClassName)'], Attributes.get_parameter_type_attrs(1, ['Class.Method(ClassName,MethodName)', 'ParameterType(1).Class(ClassName)']))

if __name__ == '__main__':
    unittest.main()
