local luaunit = require 'luaunit'
local class = require 'pl.class'
local none = require 'krpc.types'.none

local TestObjects = class(ServerTest)

function TestObjects:test_equality()
  local obj1 = self.conn.test_service.create_test_object('jeb')
  local obj2 = self.conn.test_service.create_test_object('jeb')
  luaunit.assertTrue(obj1 == obj2)
  luaunit.assertFalse(obj1 ~= obj2)

  local obj3 = self.conn.test_service.create_test_object('bob')
  luaunit.assertFalse(obj1 == obj3)
  luaunit.assertTrue(obj1 ~= obj3)

  self.conn.test_service.object_property = obj1
  local obj1a = self.conn.test_service.object_property
  luaunit.assertEquals(obj1, obj1a)

  luaunit.assertFalse(obj1 == none)
  luaunit.assertTrue(obj1 ~= none)
  luaunit.assertFalse(none == obj1)
  luaunit.assertTrue(none ~= obj1)
end

function TestObjects:test_memory_allocation()
  local obj1 = self.conn.test_service.create_test_object('jeb')
  local obj2 = self.conn.test_service.create_test_object('jeb')
  local obj3 = self.conn.test_service.create_test_object('bob')
  luaunit.assertEquals(obj1._object_id, obj2._object_id)
  luaunit.assertNotEquals(obj1._object_id, obj3._object_id)
end

return TestObjects
