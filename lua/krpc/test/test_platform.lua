local luaunit = require 'luaunit'
local class = require 'pl.class'
local platform = require 'krpc.platform'

local TestPlatform = class()

function TestPlatform:test_bytelength()
  luaunit.assertEquals(0, (''):len())
  luaunit.assertEquals(3, ('foo'):len())
  luaunit.assertEquals(3, ('\xe2\x84\xa2'):len())
  --luaunit.assertEquals(3, ('\u2122'):len())
end

function TestPlatform:test_hexlify()
  luaunit.assertEquals(platform.hexlify(''), '')
  luaunit.assertEquals(platform.hexlify('\x00\x01\x02'), '000102')
  luaunit.assertEquals(platform.hexlify('\xFF'), 'ff')
end

function TestPlatform:test_unhexlify()
  luaunit.assertEquals(platform.unhexlify(''), '')
  luaunit.assertEquals(platform.unhexlify('000102'), '\x00\x01\x02')
  luaunit.assertEquals(platform.unhexlify('ff'), '\xFF')
end

return TestPlatform
