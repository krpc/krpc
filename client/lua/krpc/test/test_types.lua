local luaunit = require 'luaunit'
local class = require 'pl.class'
local List = require 'pl.List'
local Set = require 'pl.Set'
local Map = require 'pl.Map'
local Types = require 'krpc.types'
local schema = require 'krpc.schema.KRPC'

local TestTypes = class()

function TestTypes:check_protobuf_type(code, service, name, numtypes, protobuf_type)
  luaunit.assertEquals(protobuf_type.code, code)
  luaunit.assertEquals(protobuf_type.service, service)
  luaunit.assertEquals(protobuf_type.name, name)
  luaunit.assertEquals(#(protobuf_type.types), numtypes)
end

function TestTypes:test_none_type()
  local types = Types()
  local none_type = schema.Type()
  none_type.code = Types.NONE
  luaunit.assertError(types.as_type, none_type)
end

function TestTypes:test_value_types()
  local types = Types()
  cases = List{
    { types:double_type(), Types.DOUBLE, 'number' },
    { types:float_type(), Types.FLOAT, 'number' },
    { types:sint32_type(), Types.SINT32, 'number' },
    { types:sint64_type(), Types.SINT64, 'number' },
    { types:uint32_type(), Types.UINT32, 'number' },
    { types:uint64_type(), Types.UINT64, 'number' },
    { types:bool_type(), Types.BOOL, 'boolean' },
    { types:string_type(), Types.STRING, 'string' },
    { types:bytes_type(), Types.BYTES, 'string' },
  }
  for _, case in ipairs(cases) do
    local typ = case[1]
    local protobuf_code = case[2]
    local lua_type = case[3]
    luaunit.assertTrue(typ:is_a(Types.ValueType))
    self:check_protobuf_type(protobuf_code, '', '', 0, typ.protobuf_type)
    luaunit.assertEquals(typ.lua_type, lua_type)
  end
end

function TestTypes:test_class_types()
  local types = Types()
  local typ = types:class_type('ServiceName', 'ClassName')
  luaunit.assertTrue(typ:is_a(Types.ClassType))
  self:check_protobuf_type(Types.CLASS,
                           'ServiceName', 'ClassName', 0, typ.protobuf_type)
  local instance = typ.lua_type(42)
  luaunit.assertEquals(42, instance._object_id)
  luaunit.assertEquals('ServiceName', instance._service_name)
  luaunit.assertEquals('ClassName', instance._class_name)
  local typ2 = types:as_type(typ.protobuf_type)
  luaunit.assertTrue(typ == typ2)
end

function TestTypes:test_enum_types()
  local types = Types()
  local typ = types:enumeration_type('ServiceName', 'EnumName')
  luaunit.assertTrue(typ:is_a(Types.EnumerationType))
  luaunit.assertEquals(nil, typ.lua_type)
  self:check_protobuf_type(Types.ENUMERATION,
                           'ServiceName', 'EnumName', 0, typ.protobuf_type)
  typ:set_values({a = 0, b = 42, c = 100})
  luaunit.assertEquals(0, typ.lua_type.a.value)
  luaunit.assertEquals(42, typ.lua_type.b.value)
  luaunit.assertEquals(100, typ.lua_type.c.value)
  local typ2 = types:as_type(typ.protobuf_type)
  luaunit.assertTrue(typ == typ2)
end

function TestTypes:test_message_types()
  local types = Types()
  cases = List{
    { types:procedure_call_type(), Types.PROCEDURE_CALL },
    { types:stream_type(), Types.STREAM },
    { types:services_type(), Types.SERVICES },
    { types:status_type(), Types.STATUS }
  }
  for _, case in ipairs(cases) do
    local typ = case[1]
    local protobuf_code = case[2]
    luaunit.assertTrue(typ:is_a(Types.MessageType))
    self:check_protobuf_type(protobuf_code, '', '', 0, typ.protobuf_type)
  end
end

function TestTypes:test_tuple_1_types()
  local types = Types()
  local typ = types:tuple_type({types:bool_type()})
  luaunit.assertTrue(typ:is_a(Types.TupleType))
  luaunit.assertTrue(typ.lua_type == List)
  self:check_protobuf_type(Types.TUPLE, '', '', 1, typ.protobuf_type)
  self:check_protobuf_type(Types.BOOL, '', '', 0, typ.protobuf_type.types[1])
  luaunit.assertEquals(1, typ.value_types:len())
  luaunit.assertTrue(typ.value_types[1]:is_a(Types.ValueType))
  self:check_protobuf_type(Types.BOOL, '', '', 0, typ.value_types[1].protobuf_type)
end

function TestTypes:test_tuple_2_types()
  local types = Types()
  local typ = types:tuple_type({types:uint32_type(), types:string_type()})
  luaunit.assertTrue(typ:is_a(Types.TupleType))
  luaunit.assertTrue(typ.lua_type == List)
  self:check_protobuf_type(Types.TUPLE, '', '', 2, typ.protobuf_type)
  self:check_protobuf_type(Types.UINT32, '', '', 0, typ.protobuf_type.types[1])
  self:check_protobuf_type(Types.STRING, '', '', 0, typ.protobuf_type.types[2])
  luaunit.assertEquals(2, typ.value_types:len())
  luaunit.assertTrue(typ.value_types[1]:is_a(Types.ValueType))
  luaunit.assertTrue(typ.value_types[2]:is_a(Types.ValueType))
  luaunit.assertEquals('number', typ.value_types[1].lua_type)
  luaunit.assertEquals('string', typ.value_types[2].lua_type)
  self:check_protobuf_type(Types.UINT32, '', '', 0, typ.value_types[1].protobuf_type)
  self:check_protobuf_type(Types.STRING, '', '', 0, typ.value_types[2].protobuf_type)
end

function TestTypes:test_tuple_3_types()
  local types = Types()
  local typ = types:tuple_type({types:float_type(), types:uint64_type(), types:string_type()})
  luaunit.assertTrue(typ:is_a(Types.TupleType))
  luaunit.assertTrue(typ.lua_type == List)
  self:check_protobuf_type(Types.TUPLE, '', '', 3, typ.protobuf_type)
  self:check_protobuf_type(Types.FLOAT, '', '', 0, typ.protobuf_type.types[1])
  self:check_protobuf_type(Types.UINT64, '', '', 0, typ.protobuf_type.types[2])
  luaunit.assertEquals(3, typ.value_types:len())
  luaunit.assertTrue(typ.value_types[1]:is_a(Types.ValueType))
  luaunit.assertTrue(typ.value_types[2]:is_a(Types.ValueType))
  luaunit.assertTrue(typ.value_types[3]:is_a(Types.ValueType))
  luaunit.assertEquals('number', typ.value_types[1].lua_type)
  luaunit.assertEquals('number', typ.value_types[2].lua_type)
  luaunit.assertEquals('string', typ.value_types[3].lua_type)
  self:check_protobuf_type(Types.FLOAT, '', '', 0, typ.value_types[1].protobuf_type)
  self:check_protobuf_type(Types.UINT64, '', '', 0, typ.value_types[2].protobuf_type)
  self:check_protobuf_type(Types.STRING, '', '', 0, typ.value_types[3].protobuf_type)
end

function TestTypes:test_list_types()
  local types = Types()
  local typ = types:list_type(types:uint32_type())
  luaunit.assertTrue(typ:is_a(Types.ListType))
  luaunit.assertTrue(typ.lua_type == List)
  self:check_protobuf_type(Types.LIST, '', '', 1, typ.protobuf_type)
  self:check_protobuf_type(Types.UINT32, '', '', 0, typ.protobuf_type.types[1])
  luaunit.assertTrue(typ.value_type:is_a(Types.ValueType))
  luaunit.assertEquals('number', typ.value_type.lua_type)
  self:check_protobuf_type(Types.UINT32, '', '', 0, typ.value_type.protobuf_type)
end

function TestTypes:test_set_types()
  local types = Types()
  local typ = types:set_type(types:string_type())
  luaunit.assertTrue(typ:is_a(Types.SetType))
  luaunit.assertTrue(typ.lua_type == Set)
  self:check_protobuf_type(Types.SET, '', '', 1, typ.protobuf_type)
  self:check_protobuf_type(Types.STRING, '', '', 0, typ.protobuf_type.types[1])
  luaunit.assertTrue(typ.value_type:is_a(Types.ValueType))
  luaunit.assertEquals('string', typ.value_type.lua_type)
  self:check_protobuf_type(Types.STRING, '', '', 0, typ.value_type.protobuf_type)
end

function TestTypes:test_dictionary_types()
  local types = Types()
  local typ = types:dictionary_type(types:string_type(), types:uint32_type())
  luaunit.assertTrue(typ:is_a(Types.DictionaryType))
  luaunit.assertTrue(typ.lua_type == Map)
  self:check_protobuf_type(Types.DICTIONARY, '', '', 2, typ.protobuf_type)
  self:check_protobuf_type(Types.STRING, '', '', 0, typ.protobuf_type.types[1])
  self:check_protobuf_type(Types.UINT32, '', '', 0, typ.protobuf_type.types[2])
  luaunit.assertTrue(typ.key_type:is_a(Types.ValueType))
  luaunit.assertEquals('string', typ.key_type.lua_type)
  self:check_protobuf_type(Types.STRING, '', '', 0, typ.key_type.protobuf_type)
  luaunit.assertTrue(typ.value_type:is_a(Types.ValueType))
  luaunit.assertEquals('number', typ.value_type.lua_type)
  self:check_protobuf_type(Types.UINT32, '', '', 0, typ.value_type.protobuf_type)
end

function TestTypes:test_coerce_to()
  local types = Types()
  local cases = List{
    {42, types:double_type()},
    {42, types:float_type()},
    {42, types:sint32_type()},
    {42, types:sint64_type()},
    {42, types:uint32_type()},
    {42, types:uint64_type()},
    {List{}, types:list_type(types:string_type())},
    {{0, 1, 2}, types:tuple_type({types:uint32_type(), types:uint32_type(), types:uint32_type()})},
    {{0, 1, 2}, types:list_type(types:uint32_type())},
    {{'foo', 'bar'}, types:list_type(types:string_type())}
  }
  for _, x in ipairs(cases) do
    local value = x[1]
    local typ = x[2]
    local coerced_value = types:coerce_to(value, typ)
    luaunit.assertEquals(coerced_value, value)
    luaunit.assertEquals(type(coerced_value), type(value))
  end

  luaunit.assertError(types.coerce_to, types, Types.none, types:float_type())
  luaunit.assertError(types.coerce_to, types, '', types:float_type())
  luaunit.assertError(types.coerce_to, types, true, types:float_type())

  luaunit.assertError(types.coerce_to, types, List{}, types:tuple_type({types:uint32_type()}))
  luaunit.assertError(types.coerce_to, types, List{'foo', 2}, types:tuple_type({types:string_type()}))
  luaunit.assertError(types.coerce_to, types, List{1}, types:tuple_type({types:string_type()}))
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
