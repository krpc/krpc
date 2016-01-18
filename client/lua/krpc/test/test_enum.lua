local luaunit = require 'luaunit'
local class = require 'pl.class'
local schema = require 'krpc.test.Test'

local TestEnum = class()

function TestEnum:test_enums()
  luaunit.assertEquals(schema.TestEnum.a, 0)
  luaunit.assertEquals(schema.TestEnum.b, 1)
  luaunit.assertEquals(schema.TestEnum.c, 2)
end

return TestEnum
