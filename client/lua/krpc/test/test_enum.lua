local luaunit = require 'luaunit'
local class = require 'pl.class'
local schema = require 'krpc.test.Test'

local TestEnum = class()

function TestEnum:test_enums()
  luaunit.assertEquals(schema.TestEnum.a, 1)
  luaunit.assertEquals(schema.TestEnum.b, 2)
  luaunit.assertEquals(schema.TestEnum.c, 3)
end

return TestEnum
