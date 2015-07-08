local luaunit = require 'luaunit'
local class = require 'pl.class'
local List = require 'pl.List'
local Attributes = require 'krpc.attributes'

local TestAttributes = class()

function TestAttributes:test_is_a_procedure()
  luaunit.assertTrue(Attributes.is_a_procedure(List{}))
  luaunit.assertFalse(Attributes.is_a_procedure(List{'Property.Get(PropertyName)'}))
  luaunit.assertFalse(Attributes.is_a_procedure(List{'Property.Set(PropertyName)'}))
  luaunit.assertFalse(Attributes.is_a_procedure(List{'Class.Method(ServiceName.ClassName,MethodName)'}))
  luaunit.assertFalse(Attributes.is_a_procedure(List{'Class.StaticMethod(ServiceName.ClassName,MethodName)'}))
  luaunit.assertFalse(Attributes.is_a_procedure(List{'Class.Property.Get(ServiceName.ClassName,PropertyName)'}))
  luaunit.assertFalse(Attributes.is_a_procedure(List{'Class.Property.Set(ServiceName.ClassName,PropertyName)'}))
end

function TestAttributes:test_is_a_property_accessor()
  luaunit.assertFalse(Attributes.is_a_property_accessor(List{}))
  luaunit.assertTrue(Attributes.is_a_property_accessor(List{'Property.Get(PropertyName)'}))
  luaunit.assertTrue(Attributes.is_a_property_accessor(List{'Property.Set(PropertyName)'}))
  luaunit.assertFalse(Attributes.is_a_property_accessor(List{'Class.Method(ServiceName.ClassName,MethodName)'}))
  luaunit.assertFalse(Attributes.is_a_property_accessor(List{'Class.StaticMethod(ServiceName.ClassName,MethodName)'}))
  luaunit.assertFalse(Attributes.is_a_property_accessor(List{'Class.Property.Get(ServiceName.ClassName,PropertyName)'}))
  luaunit.assertFalse(Attributes.is_a_property_accessor(List{'Class.Property.Set(ServiceName.ClassName,PropertyName)'}))
end

function TestAttributes:test_is_a_property_getter()
  luaunit.assertFalse(Attributes.is_a_property_getter(List{}))
  luaunit.assertTrue(Attributes.is_a_property_getter(List{'Property.Get(PropertyName)'}))
  luaunit.assertFalse(Attributes.is_a_property_getter(List{'Property.Set(PropertyName)'}))
  luaunit.assertFalse(Attributes.is_a_property_getter(List{'Class.Method(ServiceName.ClassName,MethodName)'}))
  luaunit.assertFalse(Attributes.is_a_property_getter(List{'Class.StaticMethod(ServiceName.ClassName,MethodName)'}))
  luaunit.assertFalse(Attributes.is_a_property_getter(List{'Class.Property.Get(ServiceName.ClassName,PropertyName)'}))
  luaunit.assertFalse(Attributes.is_a_property_getter(List{'Class.Property.Set(ServiceName.ClassName,PropertyName)'}))
end

function TestAttributes:test_is_a_property_setter()
  luaunit.assertFalse(Attributes.is_a_property_setter(List{}))
  luaunit.assertFalse(Attributes.is_a_property_setter(List{'Property.Get(PropertyName)'}))
  luaunit.assertTrue(Attributes.is_a_property_setter(List{'Property.Set(PropertyName)'}))
  luaunit.assertFalse(Attributes.is_a_property_setter(List{'Class.Method(ServiceName.ClassName,MethodName)'}))
  luaunit.assertFalse(Attributes.is_a_property_setter(List{'Class.StaticMethod(ServiceName.ClassName,MethodName)'}))
  luaunit.assertFalse(Attributes.is_a_property_setter(List{'Class.Property.Get(ServiceName.ClassName,PropertyName)'}))
  luaunit.assertFalse(Attributes.is_a_property_setter(List{'Class.Property.Set(ServiceName.ClassName,PropertyName)'}))
end

function TestAttributes:test_is_a_class_method()
  luaunit.assertFalse(Attributes.is_a_class_method(List{}))
  luaunit.assertFalse(Attributes.is_a_class_method(List{'Property.Get(PropertyName)'}))
  luaunit.assertFalse(Attributes.is_a_class_method(List{'Property.Set(PropertyName)'}))
  luaunit.assertTrue(Attributes.is_a_class_method(List{'Class.Method(ServiceName.ClassName,MethodName)'}))
  luaunit.assertFalse(Attributes.is_a_class_method(List{'Class.StaticMethod(ServiceName.ClassName,MethodName)'}))
  luaunit.assertFalse(Attributes.is_a_class_method(List{'Class.Property.Get(ServiceName.ClassName,PropertyName)'}))
  luaunit.assertFalse(Attributes.is_a_class_method(List{'Class.Property.Set(ServiceName.ClassName,PropertyName)'}))
end

function TestAttributes:test_is_a_class_property_accessor()
  luaunit.assertFalse(Attributes.is_a_class_property_accessor(List{}))
  luaunit.assertFalse(Attributes.is_a_class_property_accessor(List{'Property.Get(PropertyName)'}))
  luaunit.assertFalse(Attributes.is_a_class_property_accessor(List{'Property.Set(PropertyName)'}))
  luaunit.assertFalse(Attributes.is_a_class_property_accessor(List{'Class.Method(ServiceName.ClassName,MethodName)'}))
  luaunit.assertFalse(Attributes.is_a_class_property_accessor(List{'Class.StaticMethod(ServiceName.ClassName,MethodName)'}))
  luaunit.assertTrue(Attributes.is_a_class_property_accessor(List{'Class.Property.Get(ServiceName.ClassName,PropertyName)'}))
  luaunit.assertTrue(Attributes.is_a_class_property_accessor(List{'Class.Property.Set(ServiceName.ClassName,PropertyName)'}))
end

function TestAttributes:test_is_a_class_property_getter()
  luaunit.assertFalse(Attributes.is_a_class_property_getter(List{}))
  luaunit.assertFalse(Attributes.is_a_class_property_getter(List{'Property.Get(PropertyName)'}))
  luaunit.assertFalse(Attributes.is_a_class_property_getter(List{'Property.Set(PropertyName)'}))
  luaunit.assertFalse(Attributes.is_a_class_property_getter(List{'Class.Method(ServiceName.ClassName,MethodName)'}))
  luaunit.assertFalse(Attributes.is_a_class_property_getter(List{'Class.StaticMethod(ServiceName.ClassName,MethodName)'}))
  luaunit.assertTrue(Attributes.is_a_class_property_getter(List{'Class.Property.Get(ServiceName.ClassName,PropertyName)'}))
  luaunit.assertFalse(Attributes.is_a_class_property_getter(List{'Class.Property.Set(ServiceName.ClassName,PropertyName)'}))
end

function TestAttributes:test_is_a_class_property_setter()
  luaunit.assertFalse(Attributes.is_a_class_property_setter(List{}))
  luaunit.assertFalse(Attributes.is_a_class_property_setter(List{'Property.Get(PropertyName)'}))
  luaunit.assertFalse(Attributes.is_a_class_property_setter(List{'Property.Set(PropertyName)'}))
  luaunit.assertFalse(Attributes.is_a_class_property_setter(List{'Class.Method(ServiceName.ClassName,MethodName)'}))
  luaunit.assertFalse(Attributes.is_a_class_property_setter(List{'Class.StaticMethod(ServiceName.ClassName,MethodName)'}))
  luaunit.assertFalse(Attributes.is_a_class_property_setter(List{'Class.Property.Get(ServiceName.ClassName,PropertyName)'}))
  luaunit.assertTrue(Attributes.is_a_class_property_setter(List{'Class.Property.Set(ServiceName.ClassName,PropertyName)'}))
end

function TestAttributes:test_get_property_name()
  luaunit.assertError(Attributes.get_property_name, List{})
  luaunit.assertEquals('PropertyName', Attributes.get_property_name(List{'Property.Get(PropertyName)'}))
  luaunit.assertEquals('PropertyName', Attributes.get_property_name(List{'Property.Set(PropertyName)'}))
  luaunit.assertError(ValueError, Attributes.get_property_name, List{'Class.Method(ServiceName.ClassName,MethodName)'})
  luaunit.assertError(Attributes.get_property_name, List{'Class.StaticMethod(ServiceName.ClassName,MethodName)'})
  luaunit.assertError(Attributes.get_property_name, List{'Class.Property.Get(ServiceName.ClassName,PropertyName)'})
  luaunit.assertError(Attributes.get_property_name, List{'Class.Property.Set(ServiceName.ClassName,PropertyName)'})
end

function TestAttributes:test_get_service_name()
  luaunit.assertError(Attributes.get_service_name, List{})
  luaunit.assertError(Attributes.get_service_name, List{'Property.Get(PropertyName)'})
  luaunit.assertError(Attributes.get_service_name, List{'Property.Set(PropertyName)'})
  luaunit.assertEquals('ServiceName', Attributes.get_service_name(List{'Class.Method(ServiceName.ClassName,MethodName)'}))
  luaunit.assertEquals('ServiceName', Attributes.get_service_name(List{'Class.StaticMethod(ServiceName.ClassName,MethodName)'}))
  luaunit.assertEquals('ServiceName', Attributes.get_service_name(List{'Class.Property.Get(ServiceName.ClassName,PropertyName)'}))
  luaunit.assertEquals('ServiceName', Attributes.get_service_name(List{'Class.Property.Set(ServiceName.ClassName,PropertyName)'}))
end

function TestAttributes:test_get_class_name()
  luaunit.assertError(Attributes.get_class_name, List{})
  luaunit.assertError(Attributes.get_class_name, List{'Property.Get(PropertyName)'})
  luaunit.assertError(Attributes.get_class_name, List{'Property.Set(PropertyName)'})
  luaunit.assertEquals('ClassName', Attributes.get_class_name(List{'Class.Method(ServiceName.ClassName,MethodName)'}))
  luaunit.assertEquals('ClassName', Attributes.get_class_name(List{'Class.StaticMethod(ServiceName.ClassName,MethodName)'}))
  luaunit.assertEquals('ClassName', Attributes.get_class_name(List{'Class.Property.Get(ServiceName.ClassName,PropertyName)'}))
  luaunit.assertEquals('ClassName', Attributes.get_class_name(List{'Class.Property.Set(ServiceName.ClassName,PropertyName)'}))
end

function TestAttributes:test_get_class_method_name()
  luaunit.assertError(Attributes.get_class_method_name, List{})
  luaunit.assertError(Attributes.get_class_method_name, List{'Property.Get(PropertyName)'})
  luaunit.assertError(Attributes.get_class_method_name, List{'Property.Set(PropertyName)'})
  luaunit.assertEquals('MethodName', Attributes.get_class_method_name(List{'Class.Method(ServiceName.ClassName,MethodName)'}))
  luaunit.assertEquals('MethodName', Attributes.get_class_method_name(List{'Class.StaticMethod(ServiceName.ClassName,MethodName)'}))
  luaunit.assertError(Attributes.get_class_method_name, List{'Class.Property.Get(ServiceName.ClassName,PropertyName)'})
  luaunit.assertError(Attributes.get_class_method_name, List{'Class.Property.Set(ServiceName.ClassName,PropertyName)'})
end

function TestAttributes:test_get_class_property_name()
  luaunit.assertError(Attributes.get_class_property_name, List{})
  luaunit.assertError(Attributes.get_class_property_name, List{'Property.Get(PropertyName)'})
  luaunit.assertError(Attributes.get_class_property_name, List{'Property.Set(PropertyName)'})
  luaunit.assertError(Attributes.get_class_property_name, List{'Class.Method(ServiceName.ClassName,MethodName)'})
  luaunit.assertError(Attributes.get_class_property_name, List{'Class.StaticMethod(ServiceName.ClassName,MethodName)'})
  luaunit.assertEquals('PropertyName', Attributes.get_class_property_name(List{'Class.Property.Get(ServiceName.ClassName,PropertyName)'}))
  luaunit.assertEquals('PropertyName', Attributes.get_class_property_name(List{'Class.Property.Set(ServiceName.ClassName,PropertyName)'}))
end

function TestAttributes:test_get_return_type_attributes()
  luaunit.assertEquals(List{}, Attributes.get_return_type_attrs(List{}))
  luaunit.assertEquals(List{}, Attributes.get_return_type_attrs(List{'Class.Method(ServiceName.ClassName,MethodName)'}))
  luaunit.assertEquals(List{'Class(ServiceName.ClassName)'}, Attributes.get_return_type_attrs(List{'ReturnType.Class(ServiceName.ClassName)'}))
  luaunit.assertEquals(List{'Class(ServiceName.ClassName)'}, Attributes.get_return_type_attrs(List{'Class.Method(ServiceName.ClassName,MethodName)', 'ReturnType.Class(ServiceName.ClassName)'}))
  luaunit.assertEquals(List{'List(string)'}, Attributes.get_return_type_attrs(List{'ReturnType.List(string)'}))
  luaunit.assertEquals(List{'Dictionary(int32,string)'}, Attributes.get_return_type_attrs(List{'ReturnType.Dictionary(int32,string)'}))
  luaunit.assertEquals(List{'Set(string)'}, Attributes.get_return_type_attrs(List{'ReturnType.Set(string)'}))
  luaunit.assertEquals(List{'List(Dictionary(int32,string))'}, Attributes.get_return_type_attrs(List{'ReturnType.List(Dictionary(int32,string))'}))
  luaunit.assertEquals(List{'Dictionary(int32,List(ServiceName.ClassName))'}, Attributes.get_return_type_attrs(List{'ReturnType.Dictionary(int32,List(ServiceName.ClassName))'}))
end

function TestAttributes:test_get_parameter_type_attributes()
  luaunit.assertEquals(List{}, Attributes.get_parameter_type_attrs(1, List{}))
  luaunit.assertEquals(List{}, Attributes.get_parameter_type_attrs(1, List{'Class.Method(ServiceName.ClassName,MethodName)'}))
  luaunit.assertEquals(List{}, Attributes.get_parameter_type_attrs(1, List{'ReturnType.Class(ServiceName.ClassName)'}))
  luaunit.assertEquals(List{}, Attributes.get_parameter_type_attrs(1, List{'Class.Method(ServiceName.ClassName,MethodName)', 'ReturnType.Class(ServiceName.ClassName)'}))
  luaunit.assertEquals(List{}, Attributes.get_parameter_type_attrs(2, List{'ParameterType(2).Class(ServiceName.ClassName)'}))
  luaunit.assertEquals(List{'Class(ServiceName.ClassName)'}, Attributes.get_parameter_type_attrs(3, List{'ParameterType(2).Class(ServiceName.ClassName)'}))
  luaunit.assertEquals(List{'Class(ServiceName.ClassName2)'}, Attributes.get_parameter_type_attrs(3, List{'ParameterType(0).Class(ServiceName.ClassName1)', 'ParameterType(2).Class(ServiceName.ClassName2)'}))
  luaunit.assertEquals(List{'Class(ServiceName.ClassName)'}, Attributes.get_parameter_type_attrs(2, List{'Class.Method(ServiceName.ClassName,MethodName)', 'ParameterType(1).Class(ServiceName.ClassName)'}))
  luaunit.assertEquals(List{'List(string)'}, Attributes.get_parameter_type_attrs(2, List{'ParameterType(1).List(string)'}))
  luaunit.assertEquals(List{'Dictionary(int32,string)'}, Attributes.get_parameter_type_attrs(2, List{'ParameterType(1).Dictionary(int32,string)'}))
  luaunit.assertEquals(List{'Set(string)'}, Attributes.get_parameter_type_attrs(2, List{'ParameterType(1).Set(string)'}))
  luaunit.assertEquals(List{'List(Dictionary(int32,string))'}, Attributes.get_parameter_type_attrs(2, List{'ParameterType(1).List(Dictionary(int32,string))'}))
  luaunit.assertEquals(List{'Dictionary(int32,List(ServiceName.ClassName))'}, Attributes.get_parameter_type_attrs(2, List{'ParameterType(1).Dictionary(int32,List(ServiceName.ClassName))'}))
end

return TestAttributes
