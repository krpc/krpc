import unittest
from krpc.attributes import Attributes


class TestTypes(unittest.TestCase):
    def test_is_a_procedure(self):
        self.assertTrue(Attributes.is_a_procedure([]))
        self.assertFalse(Attributes.is_a_procedure(
            ['Property.Get(PropertyName)']))
        self.assertFalse(Attributes.is_a_procedure(
            ['Property.Set(PropertyName)']))
        self.assertFalse(Attributes.is_a_procedure(
            ['Class.Method(ServiceName.ClassName,MethodName)']))
        self.assertFalse(Attributes.is_a_procedure(
            ['Class.StaticMethod(ServiceName.ClassName,MethodName)']))
        self.assertFalse(Attributes.is_a_procedure(
            ['Class.Property.Get(ServiceName.ClassName,PropertyName)']))
        self.assertFalse(Attributes.is_a_procedure(
            ['Class.Property.Set(ServiceName.ClassName,PropertyName)']))

    def test_is_a_property_accessor(self):
        self.assertFalse(Attributes.is_a_property_accessor([]))
        self.assertTrue(Attributes.is_a_property_accessor(
            ['Property.Get(PropertyName)']))
        self.assertTrue(Attributes.is_a_property_accessor(
            ['Property.Set(PropertyName)']))
        self.assertFalse(Attributes.is_a_property_accessor(
            ['Class.Method(ServiceName.ClassName,MethodName)']))
        self.assertFalse(Attributes.is_a_property_accessor(
            ['Class.StaticMethod(ServiceName.ClassName,MethodName)']))
        self.assertFalse(Attributes.is_a_property_accessor(
            ['Class.Property.Get(ServiceName.ClassName,PropertyName)']))
        self.assertFalse(Attributes.is_a_property_accessor(
            ['Class.Property.Set(ServiceName.ClassName,PropertyName)']))

    def test_is_a_property_getter(self):
        self.assertFalse(Attributes.is_a_property_getter([]))
        self.assertTrue(Attributes.is_a_property_getter(
            ['Property.Get(PropertyName)']))
        self.assertFalse(Attributes.is_a_property_getter(
            ['Property.Set(PropertyName)']))
        self.assertFalse(Attributes.is_a_property_getter(
            ['Class.Method(ServiceName.ClassName,MethodName)']))
        self.assertFalse(Attributes.is_a_property_getter(
            ['Class.StaticMethod(ServiceName.ClassName,MethodName)']))
        self.assertFalse(Attributes.is_a_property_getter(
            ['Class.Property.Get(ServiceName.ClassName,PropertyName)']))
        self.assertFalse(Attributes.is_a_property_getter(
            ['Class.Property.Set(ServiceName.ClassName,PropertyName)']))

    def test_is_a_property_setter(self):
        self.assertFalse(Attributes.is_a_property_setter([]))
        self.assertFalse(Attributes.is_a_property_setter(
            ['Property.Get(PropertyName)']))
        self.assertTrue(Attributes.is_a_property_setter(
            ['Property.Set(PropertyName)']))
        self.assertFalse(Attributes.is_a_property_setter(
            ['Class.Method(ServiceName.ClassName,MethodName)']))
        self.assertFalse(Attributes.is_a_property_setter(
            ['Class.StaticMethod(ServiceName.ClassName,MethodName)']))
        self.assertFalse(Attributes.is_a_property_setter(
            ['Class.Property.Get(ServiceName.ClassName,PropertyName)']))
        self.assertFalse(Attributes.is_a_property_setter(
            ['Class.Property.Set(ServiceName.ClassName,PropertyName)']))

    def test_is_a_class_method(self):
        self.assertFalse(Attributes.is_a_class_method([]))
        self.assertFalse(Attributes.is_a_class_method(
            ['Property.Get(PropertyName)']))
        self.assertFalse(Attributes.is_a_class_method(
            ['Property.Set(PropertyName)']))
        self.assertTrue(Attributes.is_a_class_method(
            ['Class.Method(ServiceName.ClassName,MethodName)']))
        self.assertFalse(Attributes.is_a_class_method(
            ['Class.StaticMethod(ServiceName.ClassName,MethodName)']))
        self.assertFalse(Attributes.is_a_class_method(
            ['Class.Property.Get(ServiceName.ClassName,PropertyName)']))
        self.assertFalse(Attributes.is_a_class_method(
            ['Class.Property.Set(ServiceName.ClassName,PropertyName)']))

    def test_is_a_class_property_accessor(self):
        self.assertFalse(Attributes.is_a_class_property_accessor([]))
        self.assertFalse(Attributes.is_a_class_property_accessor(
            ['Property.Get(PropertyName)']))
        self.assertFalse(Attributes.is_a_class_property_accessor(
            ['Property.Set(PropertyName)']))
        self.assertFalse(Attributes.is_a_class_property_accessor(
            ['Class.Method(ServiceName.ClassName,MethodName)']))
        self.assertFalse(Attributes.is_a_class_property_accessor(
            ['Class.StaticMethod(ServiceName.ClassName,MethodName)']))
        self.assertTrue(Attributes.is_a_class_property_accessor(
            ['Class.Property.Get(ServiceName.ClassName,PropertyName)']))
        self.assertTrue(Attributes.is_a_class_property_accessor(
            ['Class.Property.Set(ServiceName.ClassName,PropertyName)']))

    def test_is_a_class_property_getter(self):
        self.assertFalse(Attributes.is_a_class_property_getter([]))
        self.assertFalse(Attributes.is_a_class_property_getter(
            ['Property.Get(PropertyName)']))
        self.assertFalse(Attributes.is_a_class_property_getter(
            ['Property.Set(PropertyName)']))
        self.assertFalse(Attributes.is_a_class_property_getter(
            ['Class.Method(ServiceName.ClassName,MethodName)']))
        self.assertFalse(Attributes.is_a_class_property_getter(
            ['Class.StaticMethod(ServiceName.ClassName,MethodName)']))
        self.assertTrue(Attributes.is_a_class_property_getter(
            ['Class.Property.Get(ServiceName.ClassName,PropertyName)']))
        self.assertFalse(Attributes.is_a_class_property_getter(
            ['Class.Property.Set(ServiceName.ClassName,PropertyName)']))

    def test_is_a_class_property_setter(self):
        self.assertFalse(Attributes.is_a_class_property_setter([]))
        self.assertFalse(Attributes.is_a_class_property_setter(
            ['Property.Get(PropertyName)']))
        self.assertFalse(Attributes.is_a_class_property_setter(
            ['Property.Set(PropertyName)']))
        self.assertFalse(Attributes.is_a_class_property_setter(
            ['Class.Method(ServiceName.ClassName,MethodName)']))
        self.assertFalse(Attributes.is_a_class_property_setter(
            ['Class.StaticMethod(ServiceName.ClassName,MethodName)']))
        self.assertFalse(Attributes.is_a_class_property_setter(
            ['Class.Property.Get(ServiceName.ClassName,PropertyName)']))
        self.assertTrue(Attributes.is_a_class_property_setter(
            ['Class.Property.Set(ServiceName.ClassName,PropertyName)']))

    def test_get_property_name(self):
        self.assertRaises(ValueError, Attributes.get_property_name, [])
        self.assertEqual('PropertyName', Attributes.get_property_name(
            ['Property.Get(PropertyName)']))
        self.assertEqual('PropertyName', Attributes.get_property_name(
            ['Property.Set(PropertyName)']))
        self.assertRaises(
            ValueError, Attributes.get_property_name,
            ['Class.Method(ServiceName.ClassName,MethodName)'])
        self.assertRaises(
            ValueError, Attributes.get_property_name,
            ['Class.StaticMethod(ServiceName.ClassName,MethodName)'])
        self.assertRaises(
            ValueError, Attributes.get_property_name,
            ['Class.Property.Get(ServiceName.ClassName,PropertyName)'])
        self.assertRaises(
            ValueError, Attributes.get_property_name,
            ['Class.Property.Set(ServiceName.ClassName,PropertyName)'])

    def test_get_service_name(self):
        self.assertRaises(
            ValueError, Attributes.get_service_name, [])
        self.assertRaises(
            ValueError, Attributes.get_service_name,
            ['Property.Get(PropertyName)'])
        self.assertRaises(
            ValueError, Attributes.get_service_name,
            ['Property.Set(PropertyName)'])
        self.assertEqual('ServiceName', Attributes.get_service_name(
            ['Class.Method(ServiceName.ClassName,MethodName)']))
        self.assertEqual('ServiceName', Attributes.get_service_name(
            ['Class.StaticMethod(ServiceName.ClassName,MethodName)']))
        self.assertEqual('ServiceName', Attributes.get_service_name(
            ['Class.Property.Get(ServiceName.ClassName,PropertyName)']))
        self.assertEqual('ServiceName', Attributes.get_service_name(
            ['Class.Property.Set(ServiceName.ClassName,PropertyName)']))

    def test_get_class_name(self):
        self.assertRaises(ValueError, Attributes.get_class_name, [])
        self.assertRaises(
            ValueError, Attributes.get_class_name,
            ['Property.Get(PropertyName)'])
        self.assertRaises(
            ValueError, Attributes.get_class_name,
            ['Property.Set(PropertyName)'])
        self.assertEqual('ClassName', Attributes.get_class_name(
            ['Class.Method(ServiceName.ClassName,MethodName)']))
        self.assertEqual('ClassName', Attributes.get_class_name(
            ['Class.StaticMethod(ServiceName.ClassName,MethodName)']))
        self.assertEqual('ClassName', Attributes.get_class_name(
            ['Class.Property.Get(ServiceName.ClassName,PropertyName)']))
        self.assertEqual('ClassName', Attributes.get_class_name(
            ['Class.Property.Set(ServiceName.ClassName,PropertyName)']))

    def test_get_class_method_name(self):
        self.assertRaises(ValueError, Attributes.get_class_method_name, [])
        self.assertRaises(
            ValueError, Attributes.get_class_method_name,
            ['Property.Get(PropertyName)'])
        self.assertRaises(
            ValueError, Attributes.get_class_method_name,
            ['Property.Set(PropertyName)'])
        self.assertEqual('MethodName', Attributes.get_class_method_name(
            ['Class.Method(ServiceName.ClassName,MethodName)']))
        self.assertEqual('MethodName', Attributes.get_class_method_name(
            ['Class.StaticMethod(ServiceName.ClassName,MethodName)']))
        self.assertRaises(
            ValueError, Attributes.get_class_method_name,
            ['Class.Property.Get(ServiceName.ClassName,PropertyName)'])
        self.assertRaises(
            ValueError, Attributes.get_class_method_name,
            ['Class.Property.Set(ServiceName.ClassName,PropertyName)'])

    def test_get_class_property_name(self):
        self.assertRaises(ValueError, Attributes.get_class_property_name, [])
        self.assertRaises(
            ValueError, Attributes.get_class_property_name,
            ['Property.Get(PropertyName)'])
        self.assertRaises(
            ValueError, Attributes.get_class_property_name,
            ['Property.Set(PropertyName)'])
        self.assertRaises(
            ValueError, Attributes.get_class_property_name,
            ['Class.Method(ServiceName.ClassName,MethodName)'])
        self.assertRaises(
            ValueError, Attributes.get_class_property_name,
            ['Class.StaticMethod(ServiceName.ClassName,MethodName)'])
        self.assertEqual('PropertyName', Attributes.get_class_property_name(
            ['Class.Property.Get(ServiceName.ClassName,PropertyName)']))
        self.assertEqual('PropertyName', Attributes.get_class_property_name(
            ['Class.Property.Set(ServiceName.ClassName,PropertyName)']))

    def test_get_return_type_attributes(self):
        self.assertEqual([], Attributes.get_return_type_attrs([]))
        self.assertEqual([], Attributes.get_return_type_attrs(
            ['Class.Method(ServiceName.ClassName,MethodName)']))
        self.assertEqual(['Class(ServiceName.ClassName)'], Attributes.get_return_type_attrs(
            ['ReturnType.Class(ServiceName.ClassName)']))
        self.assertEqual(['Class(ServiceName.ClassName)'], Attributes.get_return_type_attrs(
            ['Class.Method(ServiceName.ClassName,MethodName)',
             'ReturnType.Class(ServiceName.ClassName)']))
        self.assertEqual(['List(string)'], Attributes.get_return_type_attrs(
            ['ReturnType.List(string)']))
        self.assertEqual(['Dictionary(int32,string)'], Attributes.get_return_type_attrs(
            ['ReturnType.Dictionary(int32,string)']))
        self.assertEqual(['Set(string)'], Attributes.get_return_type_attrs(
            ['ReturnType.Set(string)']))
        self.assertEqual(['List(Dictionary(int32,string))'], Attributes.get_return_type_attrs(
            ['ReturnType.List(Dictionary(int32,string))']))
        self.assertEqual(['Dictionary(int32,List(ServiceName.ClassName))'], Attributes.get_return_type_attrs(
            ['ReturnType.Dictionary(int32,List(ServiceName.ClassName))']))

    def test_get_parameter_type_attributes(self):
        self.assertEqual([], Attributes.get_parameter_type_attrs(0, []))
        self.assertEqual([], Attributes.get_parameter_type_attrs(
            0, ['Class.Method(ServiceName.ClassName,MethodName)']))
        self.assertEqual([], Attributes.get_parameter_type_attrs(
            0, ['ReturnType.Class(ServiceName.ClassName)']))
        self.assertEqual([], Attributes.get_parameter_type_attrs(
            0, ['Class.Method(ServiceName.ClassName,MethodName)',
                'ReturnType.Class(ServiceName.ClassName)']))
        self.assertEqual([], Attributes.get_parameter_type_attrs(
            1, ['ParameterType(2).Class(ServiceName.ClassName)']))
        self.assertEqual(['Class(ServiceName.ClassName)'], Attributes.get_parameter_type_attrs(
            2, ['ParameterType(2).Class(ServiceName.ClassName)']))
        self.assertEqual(['Class(ServiceName.ClassName2)'], Attributes.get_parameter_type_attrs(
            2, ['ParameterType(0).Class(ServiceName.ClassName1)',
                'ParameterType(2).Class(ServiceName.ClassName2)']))
        self.assertEqual(['Class(ServiceName.ClassName)'], Attributes.get_parameter_type_attrs(
            1, ['Class.Method(ServiceName.ClassName,MethodName)',
                'ParameterType(1).Class(ServiceName.ClassName)']))
        self.assertEqual(['List(string)'], Attributes.get_parameter_type_attrs(
            1, ['ParameterType(1).List(string)']))
        self.assertEqual(['Dictionary(int32,string)'], Attributes.get_parameter_type_attrs(
            1, ['ParameterType(1).Dictionary(int32,string)']))
        self.assertEqual(['Set(string)'], Attributes.get_parameter_type_attrs(
            1, ['ParameterType(1).Set(string)']))
        self.assertEqual(['List(Dictionary(int32,string))'], Attributes.get_parameter_type_attrs(
            1, ['ParameterType(1).List(Dictionary(int32,string))']))
        self.assertEqual(['Dictionary(int32,List(ServiceName.ClassName))'], Attributes.get_parameter_type_attrs(
            1, ['ParameterType(1).Dictionary(int32,List(ServiceName.ClassName))']))

if __name__ == '__main__':
    unittest.main()
