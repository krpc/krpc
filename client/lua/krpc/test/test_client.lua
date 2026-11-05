-- The client end to end against a server, which the harness running this starts and tells it
-- how to reach.
local luaunit = require 'luaunit'
local class = require 'pl.class'
local List = require 'pl.List'
local krpc = require 'krpc.init'
local Service = require 'krpc.service'
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

function TestClient:test_nil_for_a_null()
  -- A null result is nil. A nil argument is a null at a nullable parameter with no default.
  local service = self.conn.test_service
  luaunit.assertNil(service.echo_nullable_int(nil))
  luaunit.assertNil(service.echo_test_object(Types.none))
  local obj = service.create_test_object('alpha')
  service.object_property = obj
  luaunit.assertEquals(obj, service.object_property)
  service.object_property = nil
  luaunit.assertNil(service.object_property)
  luaunit.assertNil(obj:echo_nullable_object(nil))
end

function TestClient:test_argument_error_names_the_types()
  local service = self.conn.test_service
  luaunit.assertErrorMsgContains('TestService.CreateTestObject() argument 1 must be a string, got nil',
                                 service.create_test_object, nil)
  luaunit.assertErrorMsgContains('TestService.CreateTestObject() argument 1 must be a string, got null',
                                 service.create_test_object, Types.none)
  luaunit.assertErrorMsgContains('TestService.CreateTestObject() argument 1 must be a string, got a table',
                                 service.create_test_object, {})
  luaunit.assertErrorMsgContains(
    'TestService.EchoTestObject() argument 1 must be a Class(TestService.TestClass), got a number',
    service.echo_test_object, 42)
end

function TestClient:test_compile_returns_the_function()
  local func = self.conn.krpc.Expression.constant_int(1)
  luaunit.assertEquals(func, self.conn:compile(func))
end

function TestClient:test_run_with_nil_arguments()
  local Expression = self.conn.krpc.Expression
  local Type = self.conn.krpc.Type
  local nullable = Type.nullable_type(Type.string())
  local x = Expression.parameter('x', nullable)
  local y = Expression.parameter('y', nullable)
  local func = Expression.lambda({x, y}, y)
  luaunit.assertEquals('b', self.conn:run(func, nil, 'b'))
  -- A trailing nil counts as an argument
  luaunit.assertNil(self.conn:run(func, 'a', nil))
  luaunit.assertErrorMsgContains('The function takes 2 arguments, got 1', self.conn.run, self.conn, func, 'a')
  local z = Expression.parameter('z', Type.string())
  luaunit.assertErrorMsgContains('Client.run() argument 1 must be a string, got nil',
                                 self.conn.run, self.conn, Expression.lambda({z}, z), nil)
end

function TestClient:test_run_with_a_function_of_a_closed_connection()
  local other = krpc.connect('LuaClientTestOther', 'localhost', os.getenv('KRPC_TEST_TCPIP_PORT') or 50000)
  local func = other.krpc.Expression.constant_int(1)
  other:close()
  luaunit.assertEquals(1, self.conn:run(func))
end

function TestClient:test_get_call_expects_the_service()
  local service = self.conn.test_service
  local message = 'get_call expects the service TestService'
  -- Taking the value for the service would leave a nullable setter with a null
  local obj = service.create_test_object('alpha')
  luaunit.assertErrorMsgContains(message, self.conn.get_call, self.conn, service.set_object_property, obj)
  luaunit.assertErrorMsgContains(message, self.conn.get_call, self.conn, service.get_object_property)
  luaunit.assertErrorMsgContains(message, self.conn.get_call, self.conn, service.get_object_property,
                                 self.conn.krpc)
end

function TestClient:test_get_call_expects_a_procedure()
  -- A property's value is what a program most often passes by mistake
  self.conn.test_service.string_property = 'foo'
  luaunit.assertErrorMsgContains('get_call expects a procedure', self.conn.get_call, self.conn,
                                 self.conn.test_service.string_property)
  luaunit.assertErrorMsgContains('get_call expects a procedure', self.conn.get_call, self.conn, 42)
end

function TestClient:test_get_call_with_a_procedure_of_another_connection()
  local other = krpc.connect('LuaClientTestOther', 'localhost', os.getenv('KRPC_TEST_TCPIP_PORT') or 50000)
  local call = self.conn:get_call(other.test_service.int32_to_string, 42)
  other:close()
  local func = self.conn.krpc.Expression.call(call)
  luaunit.assertEquals('42', self.conn:run(func))
end

function TestClient:test_function_must_be_an_expression()
  local obj = self.conn.test_service.create_test_object('alpha')
  for _, value in ipairs({42, 'foo', obj}) do
    luaunit.assertErrorMsgContains('Expected a KRPC.Expression object', self.conn.compile, self.conn, value)
    luaunit.assertErrorMsgContains('Expected a KRPC.Expression object', self.conn.run, self.conn, value)
  end
end

function TestClient:test_procedure_registry_does_not_hold_a_client()
  -- A value that reached a class table would keep its client alive on Lua 5.1 and LuaJIT, as their
  -- weak tables have no ephemerons, and CI runs 5.4 alone
  for _, info in pairs(Service.procedures) do
    for _, value in pairs(info) do
      luaunit.assertNotEquals('table', type(value))
    end
  end
end

os.exit(luaunit.LuaUnit:run())
