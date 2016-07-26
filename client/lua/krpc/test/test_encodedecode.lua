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
    local data = encoder.encode(decoded, typ)
    luaunit.assertEquals(platform.hexlify(data), encoded)
  end
end

local function _run_test_decode_value(typ, cases)
  for _, case in ipairs(cases) do
    decoded = case[1]
    encoded = case[2]
    local value = decoder.decode(platform.unhexlify(encoded), typ)
    if typ.protobuf_type.code == Types.FLOAT or typ.protobuf_type.code == Types.DOUBLE then
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
  _run_test_encode_value(types:float_type(), cases)
  _run_test_decode_value(types:float_type(), cases)
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
  _run_test_encode_value(types:double_type(), cases)
  _run_test_decode_value(types:double_type(), cases)
end

function TestEncodeDecode:test_sint32()
  local cases = {
    {0, '00'},
    {1, '02'},
    {42, '54'},
    {300, 'd804'},
    {-33, '41'},
    {2147483647, 'feffffff0f'},
    {-2147483648, 'ffffffff0f'}
  }
  _run_test_encode_value(types:sint32_type(), cases)
  _run_test_decode_value(types:sint32_type(), cases)
end

function TestEncodeDecode:test_sint64()
  local cases = {
    {0, '00'},
    {1, '02'},
    {42, '54'},
    {300, 'd804'},
    {1234567890000, 'a091d89fee47'},
    {-33, '41'}
  }
  _run_test_encode_value(types:sint64_type(), cases)
  _run_test_decode_value(types:sint64_type(), cases)
end

function TestEncodeDecode:test_uint32()
  local cases = {
    {0, '00'},
    {1, '01'},
    {42, '2a'},
    {300, 'ac02'},
    {math.huge, 'ffffffffffffffff7f'}
  }
  _run_test_encode_value(types:uint32_type(), cases)
  _run_test_decode_value(types:uint32_type(), cases)

  luaunit.assertError(encoder.encode, -1, types:uint32_type())
  luaunit.assertError(encoder.encode, -849, types:uint32_type())
end

function TestEncodeDecode:test_uint64()
  local cases = {
    {0, '00'},
    {1, '01'},
    {42, '2a'},
    {300, 'ac02'},
    {1234567890000, 'd088ec8ff723'}
  }
  _run_test_encode_value(types:uint64_type(), cases)
  _run_test_decode_value(types:uint64_type(), cases)

  luaunit.assertError(encoder.encode, -1, types:uint64_type())
  luaunit.assertError(encoder.encode, -849, types:uint64_type())
end

function TestEncodeDecode:test_bool()
  local cases = {
    {true, '01'},
    {false, '00'}
  }
  _run_test_encode_value(types:bool_type(), cases)
  _run_test_decode_value(types:bool_type(), cases)
end

function TestEncodeDecode:test_string()
  local cases = {
    {'', '00'},
    {'testing', '0774657374696e67'},
    {'One small step for Kerbal-kind!', '1f4f6e6520736d616c6c207374657020666f72204b657262616c2d6b696e6421'},
    {'\226\132\162', '03e284a2'},
    {'Mystery Goo\226\132\162 Containment Unit', '1f4d79737465727920476f6fe284a220436f6e7461696e6d656e7420556e6974'}
  }
  _run_test_encode_value(types:string_type(), cases)
  _run_test_decode_value(types:string_type(), cases)
end

function TestEncodeDecode:test_bytes()
  local cases = {
    {'', '00'},
    {'\186\218\85', '03bada55'},
    {'\222\173\190\239', '04deadbeef'}
  }
  _run_test_encode_value(types:bytes_type(), cases)
  _run_test_decode_value(types:bytes_type(), cases)
end

function TestEncodeDecode:test_tuple()
  local cases = {{List{1}, '0a0101'}}
  typ = types:tuple_type({types:uint32_type()})
  _run_test_encode_value(typ, cases)
  _run_test_decode_value(typ, cases)
  local cases = {{List{1,'jeb',false}, '0a01010a04036a65620a0100'}}
  typ = types:tuple_type({types:uint32_type(), types:string_type(), types:bool_type()})
  _run_test_encode_value(typ, cases)
  _run_test_decode_value(typ, cases)
end

function TestEncodeDecode:test_list()
  local cases = {
    {List{}, ''},
    {List{1}, '0a0101'},
    {List{1,2,3,4}, '0a01010a01020a01030a0104'}
  }
  typ = types:list_type(types:uint32_type())
  _run_test_encode_value(typ, cases)
  _run_test_decode_value(typ, cases)
end

function TestEncodeDecode:test_set()
  local cases = {
    {Set{}, ''},
    {Set{1}, '0a0101'},
    {Set{1,2,3,4}, '0a01010a01020a01030a0104'}
  }
  typ = types:set_type(types:uint32_type())
  _run_test_encode_value(typ, cases)
  _run_test_decode_value(typ, cases)
end

function TestEncodeDecode:test_dictionary()
  local x = Map{}
  x[''] = 0
  local cases = {
    {Map{}, ''},
    {x, '0a060a0100120100'},
    {Map{foo = 42, bar = 365, baz = 3}, '0a0a0a04036261721202ed020a090a040362617a1201030a090a0403666f6f12012a'}
  }
  typ = types:dictionary_type(types:string_type(), types:uint32_type())
  _run_test_encode_value(typ, cases)
  _run_test_decode_value(typ, cases)
end

return TestEncodeDecode
