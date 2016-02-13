local luaunit = require 'luaunit'
local class = require 'pl.class'
local List = require 'pl.List'
local Set = require 'pl.Set'
local Map = require 'pl.Map'
local encoder = require 'krpc.encoder'
local decoder = require 'krpc.decoder'
local platform = require 'krpc.platform'
local Types = require 'krpc.types'

local TestEncodeDecode = class()

local types = Types()

local function _run_test_encode_value(typ, cases)
  for _, case in ipairs(cases) do
    decoded = case[1]
    encoded = case[2]
    local data = encoder.encode(decoded, types:as_type(typ))
    luaunit.assertEquals(platform.hexlify(data), encoded)
  end
end

local function _run_test_decode_value(typ, cases)
  for _, case in ipairs(cases) do
    decoded = case[1]
    encoded = case[2]
    local value = decoder.decode(platform.unhexlify(encoded), types:as_type(typ))
    if typ == 'float' or typ == 'double' then
      luaunit.assertEquals(tostring(decoded):sub(1,8), tostring(value):sub(1,8))
    else
      luaunit.assertEquals(value, decoded)
    end
  end
end

function TestEncodeDecode:test_float()
  local cases = {
    {-1.0, '000080bf'},
    {0.0, '00000000'},
    {3.14159265359, 'db0f4940'},
    {math.huge, '0000807f'},
    {-math.huge, '000080ff'},
    {0/0, '0000c0ff'} -- should be 0000c07f ??
  }
  _run_test_encode_value('float', cases)
  _run_test_decode_value('float', cases)
end

function TestEncodeDecode:test_double()
  local cases = {
    {0.0, '0000000000000000'},
    {-1.0, '000000000000f0bf'},
    {3.14159265359, 'ea2e4454fb210940'},
    {math.huge, '000000000000f07f'},
    {-math.huge, '000000000000f0ff'},
    {0/0, '000000000000f8ff'} -- should be 000000000000f87f ??
  }
  _run_test_encode_value('double', cases)
  _run_test_decode_value('double', cases)
end

function TestEncodeDecode:test_int32()
  local cases = {
    {0, '00'},
    {1, '01'},
    {42, '2a'},
    {300, 'ac02'},
    {-33, 'dfffffffffffffffff01'},
    {math.huge, 'ffffffffffffffff7f'},
    {-math.huge, '80808080808080808001'}
  }
  _run_test_encode_value('int32', cases)
  _run_test_decode_value('int32', cases)
end

function TestEncodeDecode:test_int64()
  local cases = {
    {0, '00'},
    {1, '01'},
    {42, '2a'},
    {300, 'ac02'},
    {1234567890000, 'd088ec8ff723'},
    {-33, 'dfffffffffffffffff01'}
  }
  _run_test_encode_value('int64', cases)
  _run_test_decode_value('int64', cases)
end

function TestEncodeDecode:test_uint32()
  local cases = {
    {0, '00'},
    {1, '01'},
    {42, '2a'},
    {300, 'ac02'},
    {math.huge, 'ffffffffffffffff7f'}
  }
  _run_test_encode_value('uint32', cases)
  _run_test_decode_value('uint32', cases)

  luaunit.assertError(encoder.encode, -1, types:as_type('uint32'))
  luaunit.assertError(encoder.encode, -849, types:as_type('uint32'))
end

function TestEncodeDecode:test_uint64()
  local cases = {
    {0, '00'},
    {1, '01'},
    {42, '2a'},
    {300, 'ac02'},
    {1234567890000, 'd088ec8ff723'}
  }
  _run_test_encode_value('uint64', cases)
  _run_test_decode_value('uint64', cases)

  luaunit.assertError(encoder.encode, -1, types:as_type('uint64'))
  luaunit.assertError(encoder.encode, -849, types:as_type('uint64'))
end

function TestEncodeDecode:test_bool()
  local cases = {
    {true, '01'},
    {false, '00'}
  }
  _run_test_encode_value('bool', cases)
  _run_test_decode_value('bool', cases)
end

function TestEncodeDecode:test_string()
  local cases = {
    {'', '00'},
    {'testing', '0774657374696e67'},
    {'One small step for Kerbal-kind!', '1f4f6e6520736d616c6c207374657020666f72204b657262616c2d6b696e6421'},
    {'\226\132\162', '03e284a2'},
    {'Mystery Goo\226\132\162 Containment Unit', '1f4d79737465727920476f6fe284a220436f6e7461696e6d656e7420556e6974'}
  }
  _run_test_encode_value('string', cases)
  _run_test_decode_value('string', cases)
end

function TestEncodeDecode:test_bytes()
  local cases = {
    {'', '00'},
    {'\186\218\85', '03bada55'},
    {'\222\173\190\239', '04deadbeef'}
  }
  _run_test_encode_value('bytes', cases)
  _run_test_decode_value('bytes', cases)
end

function TestEncodeDecode:test_list()
  local cases = {
    {List{}, ''},
    {List{1}, '0a0101'},
    {List{1,2,3,4}, '0a01010a01020a01030a0104'}
  }
  _run_test_encode_value('List(int32)', cases)
  _run_test_decode_value('List(int32)', cases)
end

function TestEncodeDecode:test_dictionary()
  local x = Map{}
  x[''] = 0
  local cases = {
    {Map{}, ''},
    {x, '0a060a0100120100'},
    {Map{foo = 42, bar = 365, baz = 3}, '0a0a0a04036261721202ed020a090a040362617a1201030a090a0403666f6f12012a'}
  }
  _run_test_encode_value('Dictionary(string,int32)', cases)
  _run_test_decode_value('Dictionary(string,int32)', cases)
end

function TestEncodeDecode:test_set()
  local cases = {
    {Set{}, ''},
    {Set{1}, '0a0101'},
    {Set{1,2,3,4}, '0a01010a01020a01030a0104'}
  }
  _run_test_encode_value('Set(int32)', cases)
  _run_test_decode_value('Set(int32)', cases)
end

function TestEncodeDecode:test_tuple()
  local cases = {{List{1}, '0a0101'}}
  _run_test_encode_value('Tuple(int32)', cases)
  _run_test_decode_value('Tuple(int32)', cases)
  local cases = {{List{1,'jeb',false}, '0a01010a04036a65620a0100'}}
  _run_test_encode_value('Tuple(int32,string,bool)', cases)
  _run_test_decode_value('Tuple(int32,string,bool)', cases)
end

return TestEncodeDecode
