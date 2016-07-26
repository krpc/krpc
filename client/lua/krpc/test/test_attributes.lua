local luaunit = require 'luaunit'
local class = require 'pl.class'
local Attributes = require 'krpc.attributes'

local TestAttributes = class()

function TestAttributes:test_is_a_procedure()
  luaunit.assertTrue(Attributes.is_a_procedure('ProcedureName'))
  luaunit.assertFalse(Attributes.is_a_procedure('get_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_procedure('set_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_procedure('ClassName_MethodName'))
  luaunit.assertFalse(Attributes.is_a_procedure('ClassName_static_StaticMethodName'))
  luaunit.assertFalse(Attributes.is_a_procedure('ClassName_get_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_procedure('ClassName_set_PropertyName'))
end

function TestAttributes:test_is_a_property_accessor()
  luaunit.assertFalse(Attributes.is_a_property_accessor('ProcedureName'))
  luaunit.assertTrue(Attributes.is_a_property_accessor('get_PropertyName'))
  luaunit.assertTrue(Attributes.is_a_property_accessor('set_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_property_accessor('ClassName_MethodName'))
  luaunit.assertFalse(Attributes.is_a_property_accessor('ClassName_static_StaticMethodName'))
  luaunit.assertFalse(Attributes.is_a_property_accessor('ClassName_get_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_property_accessor('ClassName_set_PropertyName'))
end

function TestAttributes:test_is_a_property_getter()
  luaunit.assertFalse(Attributes.is_a_property_getter('ProcedureName'))
  luaunit.assertTrue(Attributes.is_a_property_getter('get_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_property_getter('set_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_property_getter('ClassName_MethodName'))
  luaunit.assertFalse(Attributes.is_a_property_getter('ClassName_static_StaticMethodName'))
  luaunit.assertFalse(Attributes.is_a_property_getter('ClassName_get_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_property_getter('ClassName_set_PropertyName'))
end

function TestAttributes:test_is_a_property_setter()
  luaunit.assertFalse(Attributes.is_a_property_setter('ProcedureName'))
  luaunit.assertFalse(Attributes.is_a_property_setter('get_PropertyName'))
  luaunit.assertTrue(Attributes.is_a_property_setter('set_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_property_setter('ClassName_MethodName'))
  luaunit.assertFalse(Attributes.is_a_property_setter('ClassName_static_StaticMethodName'))
  luaunit.assertFalse(Attributes.is_a_property_setter('ClassName_get_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_property_setter('ClassName_set_PropertyName'))
end

function TestAttributes:test_is_a_class_member()
  luaunit.assertFalse(Attributes.is_a_class_member('ProcedureName'))
  luaunit.assertFalse(Attributes.is_a_class_member('get_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_class_member('set_PropertyName'))
  luaunit.assertTrue(Attributes.is_a_class_member('ClassName_MethodName'))
  luaunit.assertTrue(Attributes.is_a_class_member('ClassName_static_StaticMethodName'))
  luaunit.assertTrue(Attributes.is_a_class_member('ClassName_get_PropertyName'))
  luaunit.assertTrue(Attributes.is_a_class_member('ClassName_set_PropertyName'))
end

function TestAttributes:test_is_a_class_method()
  luaunit.assertFalse(Attributes.is_a_class_method('ProcedureName'))
  luaunit.assertFalse(Attributes.is_a_class_method('get_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_class_method('set_PropertyName'))
  luaunit.assertTrue(Attributes.is_a_class_method('ClassName_MethodName'))
  luaunit.assertFalse(Attributes.is_a_class_method('ClassName_static_StaticMethodName'))
  luaunit.assertFalse(Attributes.is_a_class_method('ClassName_get_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_class_method('ClassName_set_PropertyName'))
end

function TestAttributes:test_is_a_class_static_method()
  luaunit.assertFalse(Attributes.is_a_class_static_method('ProcedureName'))
  luaunit.assertFalse(Attributes.is_a_class_static_method('get_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_class_static_method('set_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_class_static_method('ClassName_MethodName'))
  luaunit.assertTrue(Attributes.is_a_class_static_method('ClassName_static_StaticMethodName'))
  luaunit.assertFalse(Attributes.is_a_class_static_method('ClassName_get_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_class_static_method('ClassName_set_PropertyName'))
end

function TestAttributes:test_is_a_class_property_accessor()
  luaunit.assertFalse(Attributes.is_a_class_property_accessor('ProcedureName'))
  luaunit.assertFalse(Attributes.is_a_class_property_accessor('get_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_class_property_accessor('set_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_class_property_accessor('ClassName_MethodName'))
  luaunit.assertFalse(Attributes.is_a_class_property_accessor('ClassName_static_StaticMethodName'))
  luaunit.assertTrue(Attributes.is_a_class_property_accessor('ClassName_get_PropertyName'))
  luaunit.assertTrue(Attributes.is_a_class_property_accessor('ClassName_set_PropertyName'))
end

function TestAttributes:test_is_a_class_property_getter()
  luaunit.assertFalse(Attributes.is_a_class_property_getter('ProcedureName'))
  luaunit.assertFalse(Attributes.is_a_class_property_getter('get_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_class_property_getter('set_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_class_property_getter('ClassName_MethodName'))
  luaunit.assertFalse(Attributes.is_a_class_property_getter('ClassName_static_StaticMethodName'))
  luaunit.assertTrue(Attributes.is_a_class_property_getter('ClassName_get_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_class_property_getter('ClassName_set_PropertyName'))
end

function TestAttributes:test_is_a_class_property_setter()
  luaunit.assertFalse(Attributes.is_a_class_property_setter('ProcedureName'))
  luaunit.assertFalse(Attributes.is_a_class_property_setter('get_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_class_property_setter('set_PropertyName'))
  luaunit.assertFalse(Attributes.is_a_class_property_setter('ClassName_MethodName'))
  luaunit.assertFalse(Attributes.is_a_class_property_setter('ClassName_static_StaticMethodName'))
  luaunit.assertFalse(Attributes.is_a_class_property_setter('ClassName_get_PropertyName'))
  luaunit.assertTrue(Attributes.is_a_class_property_setter('ClassName_set_PropertyName'))
end

function TestAttributes:test_get_property_name()
  luaunit.assertError(Attributes.get_property_name, 'ProcedureName')
  luaunit.assertEquals(Attributes.get_property_name('get_PropertyName'), 'PropertyName')
  luaunit.assertEquals(Attributes.get_property_name('set_PropertyName'), 'PropertyName')
  luaunit.assertError(Attributes.get_property_name, 'ClassName_MethodName')
  luaunit.assertError(Attributes.get_property_name, 'ClassName_static_StaticMethodName')
  luaunit.assertError(Attributes.get_property_name, 'ClassName_get_PropertyName')
  luaunit.assertError(Attributes.get_property_name, 'ClassName_set_PropertyName')
end

function TestAttributes:test_get_class_name()
  luaunit.assertError(Attributes.get_class_name, 'ProcedureName')
  luaunit.assertError(Attributes.get_class_name, 'get_PropertyName')
  luaunit.assertError(Attributes.get_class_name, 'set_PropertyName')
  luaunit.assertEquals(Attributes.get_class_name('ClassName_MethodName'), 'ClassName')
  luaunit.assertEquals(Attributes.get_class_name('ClassName_static_StaticMethodName'), 'ClassName')
  luaunit.assertEquals(Attributes.get_class_name('ClassName_get_PropertyName'), 'ClassName')
  luaunit.assertEquals(Attributes.get_class_name('ClassName_set_PropertyName'), 'ClassName')
end

function TestAttributes:test_get_class_member_name()
  luaunit.assertError(Attributes.get_class_member_name, 'ProcedureName')
  luaunit.assertError(Attributes.get_class_member_name, 'get_PropertyName')
  luaunit.assertError(Attributes.get_class_member_name, 'set_PropertyName')
  luaunit.assertEquals(Attributes.get_class_member_name('ClassName_MethodName'), 'MethodName')
  luaunit.assertEquals(Attributes.get_class_member_name('ClassName_static_StaticMethodName'), 'StaticMethodName')
  luaunit.assertEquals(Attributes.get_class_member_name('ClassName_get_PropertyName'), 'PropertyName')
  luaunit.assertEquals(Attributes.get_class_member_name('ClassName_set_PropertyName'), 'PropertyName')
end

return TestAttributes
