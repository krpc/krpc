local luaunit = require 'luaunit'
local class = require 'pl.class'
local Types = require 'krpc.types'

local TestTypes = class()

function TestTypes:test_nullable_type_shares_its_lua_type()
  local types = Types()
  -- Nullability belongs to the position a value sits in, so a class has one metatable
  -- however the position that holds it is declared
  local cls = types:class_type('ServiceName', 'ClassName')
  luaunit.assertIs(cls.lua_type, types:nullable(cls).lua_type)
  local enumeration = types:enumeration_type('ServiceName', 'EnumName')
  local nullable_enumeration = types:nullable(enumeration)
  enumeration:set_values({a = 0})
  luaunit.assertIs(enumeration.lua_type, nullable_enumeration.lua_type)
  local struct = types:struct_type('ServiceName', 'StructName')
  local nullable_struct = types:nullable(struct)
  struct:set_fields({{'count', types:uint32_type()}})
  luaunit.assertIs(struct.lua_type, nullable_struct.lua_type)
  luaunit.assertEquals(struct.field_types, nullable_struct.field_types)
end

function TestTypes:test_struct_and_class_compare_unequal_to_other_values()
  local types = Types()
  local struct_type = types:struct_type('ServiceName', 'EqualityStruct')
  struct_type:set_fields({{'count', types:uint32_type()}})
  local class_type = types:class_type('ServiceName', 'EqualityClass')
  for _, value in ipairs({struct_type.lua_type(1), class_type.lua_type(1)}) do
    luaunit.assertFalse(Types.none == value)
    luaunit.assertFalse(value == Types.none)
    luaunit.assertFalse({} == value)
    luaunit.assertFalse(value == {})
  end
end

function TestTypes:test_struct_ordering_of_a_field_that_orders_neither_way()
  local types = Types()
  local typ = types:struct_type('ServiceName', 'NanStruct')
  typ:set_fields({{'value', types:double_type()}, {'count', types:uint32_type()}})
  -- A NaN is unequal to itself and orders neither way, so ordering moves on to the next
  -- field rather than calling the two values ordered
  local nan = 0/0
  luaunit.assertTrue(typ.lua_type(nan, 1) < typ.lua_type(nan, 2))
  luaunit.assertFalse(typ.lua_type(nan, 2) < typ.lua_type(nan, 1))
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
