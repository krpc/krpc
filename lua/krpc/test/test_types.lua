local luaunit = require 'luaunit'
local class = require 'pl.class'
local List = require 'pl.List'
local Set = require 'pl.Set'
local Map = require 'pl.Map'
local Types = require 'krpc.types'
local schema = require 'krpc.schema.KRPC'

Types.add_search_path('krpc.test')

local TestTypes = class()

local PROTOBUF_VALUE_TYPES = List{'double', 'float', 'int32', 'int64', 'uint32', 'uint64', 'bool', 'string', 'bytes'}
local LUA_VALUE_TYPES = List{numeric, boolean, string}

local PROTOBUF_TO_LUA_VALUE_TYPE = Map{
  double = 'number',
  float = 'number',
  int32 = 'number',
  int64 = 'number',
  uint32 = 'number',
  uint64 = 'number',
  bool = 'boolean',
  string = 'string',
  bytes = 'string'
}

function TestTypes:test_value_types()
  local types = Types()
  for protobuf_type in PROTOBUF_VALUE_TYPES:iter() do
    local lua_type = PROTOBUF_TO_LUA_VALUE_TYPE[protobuf_type]
    local typ = types:as_type(protobuf_type)
    luaunit.assertTrue(typ:is_a(Types.ValueType))
    luaunit.assertEquals(protobuf_type, typ.protobuf_type)
    luaunit.assertEquals(lua_type, typ.lua_type)
  end
  luaunit.assertError(Types.ValueType, 'invalid')
end

function TestTypes:test_message_types()
  local types = Types()
  local typ = types:as_type('KRPC.Request')
  luaunit.assertTrue(typ:is_a(Types.MessageType))
  luaunit.assertEquals('KRPC.Request', typ.protobuf_type)
  luaunit.assertError(types.as_type, types, 'KRPC.DoesntExist')
  luaunit.assertError(Types.MessageType, '')
  luaunit.assertError(Types.MessageType, 'invalid')
  luaunit.assertError(Types.MessageType, '.')
  luaunit.assertError(Types.MessageType, 'foo.bar')
end

function TestTypes:test_protobuf_enum_types()
  local types = Types()
  local typ = types:as_type('Test.TestEnum')
  luaunit.assertTrue(typ:is_a(Types.ProtobufEnumType))
  luaunit.assertEquals('numeric', typ.lua_type)
  luaunit.assertEquals('Test.TestEnum', typ.protobuf_type)
  luaunit.assertError(types.as_type, types, 'Test.DoesntExist')
  luaunit.assertError(Types.ProtobufEnumType, '')
  luaunit.assertError(Types.ProtobufEnumType, 'invalid')
  luaunit.assertError(Types.ProtobufEnumType, '.')
  luaunit.assertError(Types.ProtobufEnumType, 'foo.bar')
end

function TestTypes:test_enum_types()
  local types = Types()
  local typ = types:as_type('Enum(ServiceName.EnumName)')
  luaunit.assertTrue(typ:is_a(Types.EnumType))
  luaunit.assertEquals(nil, typ.lua_type)
  luaunit.assertTrue('Enum(ServiceName.EnumName)', typ.protobuf_type)
  typ:set_values({a = 0, b = 42, c = 100})
  --luaunit.assertTrue(typ.lua_type:is_a(Types.Enum))
  luaunit.assertEquals(0, typ.lua_type.a.value)
  luaunit.assertEquals(42, typ.lua_type.b.value)
  luaunit.assertEquals(100, typ.lua_type.c.value)
end

function TestTypes:test_class_types()
  local types = Types()
  local typ = types:as_type('Class(ServiceName.ClassName)')
  luaunit.assertTrue(typ:is_a(Types.ClassType))
  --luaunit.assertTrue(typ.lua_type:is_a(Types.ClassBase))
  luaunit.assertTrue('Class(ServiceName.ClassName)', typ.protobuf_type)
  instance = typ.lua_type(42)
  luaunit.assertEquals(42, instance._object_id)
  luaunit.assertEquals('ServiceName', instance._service_name)
  luaunit.assertEquals('ClassName', instance._class_name)
  local typ2 = types:as_type('Class(ServiceName.ClassName)')
  luaunit.assertTrue(typ == typ2)
end

function TestTypes:test_list_types()
  local types = Types()
  local typ = types:as_type('List(int32)')
  luaunit.assertTrue(typ:is_a(Types.ListType))
  luaunit.assertTrue(typ.lua_type == List)
  luaunit.assertEquals('List(int32)', typ.protobuf_type)
  luaunit.assertTrue(typ.value_type:is_a(Types.ValueType))
  luaunit.assertEquals('int32', typ.value_type.protobuf_type)
  luaunit.assertEquals('number', typ.value_type.lua_type)
  luaunit.assertError(types.as_type, types, 'List')
  luaunit.assertError(types.as_type, types, 'List(')
  luaunit.assertError(types.as_type, types, 'List()')
  luaunit.assertError(types.as_type, types, 'List(foo')
  luaunit.assertError(types.as_type, types, 'List(int32,string)')
end

function TestTypes:test_dictionary_types()
  local types = Types()
  local typ = types:as_type('Dictionary(string,int32)')
  luaunit.assertTrue(typ:is_a(Types.DictionaryType))
  luaunit.assertTrue(typ.lua_type == Map)
  luaunit.assertEquals('Dictionary(string,int32)', typ.protobuf_type)
  luaunit.assertTrue(typ.key_type:is_a(Types.ValueType))
  luaunit.assertEquals('string', typ.key_type.protobuf_type)
  luaunit.assertEquals('string', typ.key_type.lua_type)
  luaunit.assertTrue(typ.value_type:is_a(Types.ValueType))
  luaunit.assertEquals('int32', typ.value_type.protobuf_type)
  luaunit.assertEquals('number', typ.value_type.lua_type)
  luaunit.assertError(types.as_type, types, 'Dictionary')
  luaunit.assertError(types.as_type, types, 'Dictionary(')
  luaunit.assertError(types.as_type, types, 'Dictionary()')
  luaunit.assertError(types.as_type, types, 'Dictionary(foo')
  luaunit.assertError(types.as_type, types, 'Dictionary(string)')
  luaunit.assertError(types.as_type, types, 'Dictionary(string,)')
  luaunit.assertError(types.as_type, types, 'Dictionary(,)')
  luaunit.assertError(types.as_type, types, 'Dictionary(,string)')
  luaunit.assertError(types.as_type, types, 'Dictionary(int,string))')
end

function TestTypes:test_set_types()
  local types = Types()
  local typ = types:as_type('Set(string)')
  luaunit.assertTrue(typ:is_a(Types.SetType))
  luaunit.assertTrue(typ.lua_type == Set)
  luaunit.assertEquals('Set(string)', typ.protobuf_type)
  luaunit.assertTrue(typ.value_type:is_a(Types.ValueType))
  luaunit.assertEquals('string', typ.value_type.protobuf_type)
  luaunit.assertEquals('string', typ.value_type.lua_type)
  luaunit.assertError(types.as_type, types, 'Set')
  luaunit.assertError(types.as_type, types, 'Set(')
  luaunit.assertError(types.as_type, types, 'Set()')
  luaunit.assertError(types.as_type, types, 'Set(string,)')
  luaunit.assertError(types.as_type, types, 'Set(,)')
  luaunit.assertError(types.as_type, types, 'Set(,string)')
  luaunit.assertError(types.as_type, types, 'Set(int,string))')
end

function TestTypes:test_tuple_types()
  local types = Types()
  local typ = types:as_type('Tuple(bool)')
  luaunit.assertTrue(typ:is_a(Types.TupleType))
  luaunit.assertTrue(typ.lua_type, List)
  luaunit.assertEquals('Tuple(bool)', typ.protobuf_type)
  luaunit.assertEquals(1, typ.value_types:len())
  luaunit.assertTrue(typ.value_types[1]:is_a(Types.ValueType))
  luaunit.assertEquals('bool', typ.value_types[1].protobuf_type)
  luaunit.assertEquals('boolean', typ.value_types[1].lua_type)
  local typ = types:as_type('Tuple(int32,string)')
  luaunit.assertTrue(typ:is_a(Types.TupleType))
  luaunit.assertTrue(typ.lua_type == List)
  luaunit.assertEquals('Tuple(int32,string)', typ.protobuf_type)
  luaunit.assertEquals(2, typ.value_types:len())
  luaunit.assertTrue(typ.value_types[1]:is_a(Types.ValueType))
  luaunit.assertTrue(typ.value_types[2]:is_a(Types.ValueType))
  luaunit.assertEquals('int32', typ.value_types[1].protobuf_type)
  luaunit.assertEquals('string', typ.value_types[2].protobuf_type)
  luaunit.assertEquals('number', typ.value_types[1].lua_type)
  luaunit.assertEquals('string', typ.value_types[2].lua_type)
  local typ = types:as_type('Tuple(float,int64,string)')
  luaunit.assertTrue(typ:is_a(Types.TupleType))
  luaunit.assertTrue(typ.lua_type == List)
  luaunit.assertEquals('Tuple(float,int64,string)', typ.protobuf_type)
  luaunit.assertEquals(3, typ.value_types:len())
  luaunit.assertTrue(typ.value_types[1]:is_a(Types.ValueType))
  luaunit.assertTrue(typ.value_types[2]:is_a(Types.ValueType))
  luaunit.assertTrue(typ.value_types[3]:is_a(Types.ValueType))
  luaunit.assertEquals('float', typ.value_types[1].protobuf_type)
  luaunit.assertEquals('int64', typ.value_types[2].protobuf_type)
  luaunit.assertEquals('string', typ.value_types[3].protobuf_type)
  luaunit.assertEquals('number', typ.value_types[1].lua_type)
  luaunit.assertEquals('number', typ.value_types[2].lua_type)
  luaunit.assertEquals('string', typ.value_types[3].lua_type)
  luaunit.assertError(types.as_type, types, 'Tuple')
  luaunit.assertError(types.as_type, types, 'Tuple(')
  luaunit.assertError(types.as_type, types, 'Tuple()')
  luaunit.assertError(types.as_type, types, 'Tuple(foo')
  luaunit.assertError(types.as_type, types, 'Tuple(string,)')
  luaunit.assertError(types.as_type, types, 'Tuple(,)')
  luaunit.assertError(types.as_type, types, 'Tuple(,string)')
  luaunit.assertError(types.as_type, types, 'Tuple(int,string))')
end

function TestTypes.test_get_parameter_type()
  local types = Types()
  luaunit.assertEquals('number', types:get_parameter_type(0, 'float', List{}).lua_type)
  luaunit.assertEquals('int32', types:get_parameter_type(0, 'int32', List{}).protobuf_type)
  luaunit.assertEquals('KRPC.Response', types:get_parameter_type(1, 'KRPC.Response', List{}).protobuf_type)
  local class_parameter = types:get_parameter_type(0, 'uint64', List{'ParameterType(0).Class(ServiceName.ClassName)'})
  luaunit.assertTrue(types:as_type('Class(ServiceName.ClassName)') == class_parameter)
  luaunit.assertTrue(class_parameter:is_a(Types.ClassType))
  --luaunit.assertTrue(issubclass(class_parameter.python_type, ClassBase))
  luaunit.assertEquals('Class(ServiceName.ClassName)', class_parameter.protobuf_type)
  luaunit.assertEquals('uint64', types:get_parameter_type(0, 'uint64', List{'ParameterType(1).Class(ServiceName.ClassName)'}).protobuf_type)
  luaunit.assertEquals('Test.TestEnum', types:get_parameter_type(0, 'Test.TestEnum', List{}).protobuf_type)
end

function TestTypes.test_get_return_type()
  local types = Types()
  luaunit.assertEquals('float', types:get_return_type('float', List{}).protobuf_type)
  luaunit.assertEquals('int32', types:get_return_type( 'int32', List{}).protobuf_type)
  luaunit.assertEquals('KRPC.Response', types:get_return_type('KRPC.Response', List{}).protobuf_type)
  luaunit.assertEquals('Test.TestEnum', types:get_return_type('Test.TestEnum', List{}).protobuf_type)
  luaunit.assertEquals('Class(ServiceName.ClassName)', types:get_return_type('uint64', List{'ReturnType.Class(ServiceName.ClassName)'}).protobuf_type)
end

--function test_coerce_to()
--  types = Types()
--  cases = [
--      (42.0, 42,   'double'),
--      (42.0, 42,   'float'),
--      (42,   42.0, 'int32'),
--      (42,   42L,  'int32'),
--      (42L,  42.0, 'int64'),
--      (42L,  42,   'int64'),
--      (42,   42.0, 'uint32'),
--      (42,   42L,  'uint32'),
--      (42L,  42.0, 'uint64'),
--      (42L,  42,   'uint64'),
--      (list(), tuple(), 'List(string)'),
--      ((0,1,2), [0,1,2], 'Tuple(int32,int32,int32)'),
--      ([0,1,2], (0,1,2), 'List(int32)'),
--  ]
--  for expected, value, typ in cases:
--      coerced_value = types.coerce_to(value, types.as_type(typ))
--      self.assertEqual(expected, coerced_value)
--      self.assertEqual(type(expected), type(coerced_value))
--
--  self.assertRaises(ValueError, types.coerce_to, None, types.as_type('float'))
--  self.assertRaises(ValueError, types.coerce_to, '', types.as_type('float'))
--  self.assertRaises(ValueError, types.coerce_to, True, types.as_type('float'))
--
--  self.assertRaises(ValueError, types.coerce_to, list(), types.as_type('Tuple(int32)'))
--  self.assertRaises(ValueError, types.coerce_to, ['foo',2], types.as_type('Tuple(string)'))
--  self.assertRaises(ValueError, types.coerce_to, [1], types.as_type('Tuple(string)'))
--  self.assertRaises(ValueError, types.coerce_to, [1,'a','b'], types.as_type('List(string)'))
--end

return TestTypes
