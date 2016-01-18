local luaunit = require 'luaunit'
local class = require 'pl.class'
local platform = require 'krpc.platform'

local TestPlatform = class()

function TestPlatform:test_bytelength()
  luaunit.assertEquals(0, (''):len())
  luaunit.assertEquals(3, ('foo'):len())
  luaunit.assertEquals(3, ('\226\132\162'):len())
  --luaunit.assertEquals(3, ('\u2122'):len())
end

function TestPlatform:test_hexlify()
  luaunit.assertEquals(platform.hexlify(''), '')
  luaunit.assertEquals(platform.hexlify('\0\1\2'), '000102')
  luaunit.assertEquals(platform.hexlify('\255'), 'ff')
end

function TestPlatform:test_unhexlify()
  luaunit.assertEquals(platform.unhexlify(''), '')
  luaunit.assertEquals(platform.unhexlify('000102'), '\0\1\2')
  luaunit.assertEquals(platform.unhexlify('ff'), '\255')
end

return TestPlatform
