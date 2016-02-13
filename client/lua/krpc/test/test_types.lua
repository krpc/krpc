local luaunit = require 'luaunit'
local class = require 'pl.class'
local List = require 'pl.List'
local Set = require 'pl.Set'
local Map = require 'pl.Map'
local Types = require 'krpc.types'

local TestTypes = class()

local PROTOBUF_VALUE_TYPES = List{'double', 'float', 'int32', 'int64', 'uint32', 'uint64', 'bool', 'string', 'bytes'}
local LUA_VALUE_TYPES = List{'number', 'boolean', 'string'}

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

function TestTypes:test_enum_types()
  local types = Types()
  local typ = types:as_type('Enum(ServiceName.EnumName)')
  luaunit.assertTrue(typ:is_a(Types.EnumType))
  luaunit.assertEquals(nil, typ.lua_type)
  luaunit.assertTrue('Enum(ServiceName.EnumName)', typ.protobuf_type)
  typ:set_values({a = 0, b = 42, c = 100})
  luaunit.assertEquals(0, typ.lua_type.a.value)
  luaunit.assertEquals(42, typ.lua_type.b.value)
  luaunit.assertEquals(100, typ.lua_type.c.value)
end

function TestTypes:test_class_types()
  local types = Types()
  local typ = types:as_type('Class(ServiceName.ClassName)')
  luaunit.assertTrue(typ:is_a(Types.ClassType))
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
  luaunit.assertEquals('number', types:get_parameter_type(1, 'float', List{}).lua_type)
  luaunit.assertEquals('int32', types:get_parameter_type(1, 'int32', List{}).protobuf_type)
  luaunit.assertEquals('KRPC.Response', types:get_parameter_type(1, 'KRPC.Response', List{}).protobuf_type)
  local class_parameter = types:get_parameter_type(1, 'uint64', List{'ParameterType(0).Class(ServiceName.ClassName)'})
  luaunit.assertTrue(types:as_type('Class(ServiceName.ClassName)') == class_parameter)
  luaunit.assertTrue(class_parameter:is_a(Types.ClassType))
  luaunit.assertEquals('Class(ServiceName.ClassName)', class_parameter.protobuf_type)
  luaunit.assertEquals('uint64', types:get_parameter_type(1, 'uint64', List{'ParameterType(1).Class(ServiceName.ClassName)'}).protobuf_type)
end

function TestTypes.test_get_return_type()
  local types = Types()
  luaunit.assertEquals('float', types:get_return_type('float', List{}).protobuf_type)
  luaunit.assertEquals('int32', types:get_return_type( 'int32', List{}).protobuf_type)
  luaunit.assertEquals('KRPC.Response', types:get_return_type('KRPC.Response', List{}).protobuf_type)
  luaunit.assertEquals('Class(ServiceName.ClassName)', types:get_return_type('uint64', List{'ReturnType.Class(ServiceName.ClassName)'}).protobuf_type)
end

function TestTypes:test_coerce_to()
  local types = Types()
  luaunit.assertEquals(List{'foo','bar'}, types:coerce_to(List{'foo','bar'}, types:as_type('Tuple(string)')))
  luaunit.assertError(types.coerce_to, types, Types.none, types:as_type('float'))
  luaunit.assertError(types.coerce_to, types, '', types:as_type('float'))
  luaunit.assertError(types.coerce_to, types, true, types:as_type('float'))
end

function TestTypes:test_none()
  luaunit.assertEquals(tostring(Types.none), 'none')
  luaunit.assertTrue(Types.none == Types.none)
  luaunit.assertFalse(Types.none ~= Types.none)
  luaunit.assertTrue(Types.none ~= nil)
  luaunit.assertTrue(Types.none ~= true)
  luaunit.assertTrue(Types.none ~= false)
  luaunit.assertTrue(Types.none ~= 'foo')
  luaunit.assertTrue(Types.none ~= {'foo'})
  luaunit.assertTrue(Types.none ~= class()())
end

return TestTypes
