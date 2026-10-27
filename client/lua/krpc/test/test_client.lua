-- The client end to end against a server, which the harness running this starts and tells it
-- how to reach.
local luaunit = require 'luaunit'
local class = require 'pl.class'
local List = require 'pl.List'
local krpc = require 'krpc.init'
local Types = require 'krpc.types'

TestClient = class()

function TestClient:setUp()
  local port = os.getenv('KRPC_TEST_TCPIP_PORT') or 50000
  self.conn = krpc.connect('LuaClientTest', 'localhost', port)
end

function TestClient:tearDown()
  self.conn:close()
end

function TestClient:test_plain_table_for_a_collection_or_structure()
  -- A plain table passes for a list, a tuple and a structure, its items taken in order, and
  -- holds a null at a position that takes one
  local service = self.conn.test_service
  luaunit.assertEquals(List{1, 2, 3}, service.increment_list({0, 1, 2}))
  luaunit.assertEquals(List{1, Types.none, 3},
                       service.echo_list_of_nullable_ints({1, Types.none, 3}))
  luaunit.assertEquals(List{2, 3}, service.increment_tuple({1, 2}))
  luaunit.assertEquals(List{1, Types.none},
                       service.echo_tuple_with_a_nullable_object({1, Types.none}))
  local obj = service.create_test_object('alpha')
  luaunit.assertEquals(List{List{obj, Types.none}},
                       service.echo_nested_list_of_nullable_objects({{obj, Types.none}}))
  local result = service.nullable_struct_echo({42, Types.none, Types.none, 'beta', obj})
  luaunit.assertEquals(42, result.int_field)
  luaunit.assertEquals(Types.none, result.nullable_int_field)
  luaunit.assertEquals('beta', result.nullable_string_field)
  luaunit.assertEquals(obj, result.nullable_object_field)
end

os.exit(luaunit.LuaUnit:run())
