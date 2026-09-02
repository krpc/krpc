local luaunit = require 'luaunit'
local ServerTest = require 'krpc.test.servertest'
local class = require 'pl.class'
local seq = require 'pl.seq'
local stringx = require 'pl.stringx'
local tablex = require 'pl.tablex'
local List = require 'pl.List'
local Map = require 'pl.Map'
local Set = require 'pl.Set'
local krpc = require 'krpc.init'
local Types = require 'krpc.types'

local TestClient = class(ServerTest)

function TestClient:test_version()
  status = self.conn.krpc:get_status()
  luaunit.assertStrMatches(status.version, '%d+.%d+.%d+')
end

function TestClient:test_wrong_rpc_port()
  luaunit.assertError(
    krpc.connect, 'LuaClientTestWrongRpcPort',
    'localhost', self.get_rpc_port() ^ self.get_stream_port(), self.get_stream_port())
end

function TestClient:test_wrong_rpc_server()
  luaunit.assertErrorMsgContains(
    'Connection request was for the rpc server, but this is the stream server. ' ..
    'Did you connect to the wrong port number or socket path?',
    self.connect_to_stream_server, 'LuaClientTestWrongRpcServer')
end

function TestClient:test_value_parameters()
  luaunit.assertEquals('3.14159', self.conn.test_service.float_to_string(3.14159))
  luaunit.assertEquals('3.14159', self.conn.test_service.double_to_string(3.14159))
  luaunit.assertEquals('42', self.conn.test_service.int32_to_string(42))
  luaunit.assertEquals('123456789000', self.conn.test_service.int64_to_string(123456789000))
  luaunit.assertEquals('True', self.conn.test_service.bool_to_string(true))
  luaunit.assertEquals('False', self.conn.test_service.bool_to_string(false))
  luaunit.assertEquals(12345, self.conn.test_service.string_to_int32('12345'))
  luaunit.assertEquals('deadbeef', self.conn.test_service.bytes_to_hex_string('\222\173\190\239'))
end

function TestClient:test_multiple_value_parameters()
  luaunit.assertEquals('3.14159', self.conn.test_service.add_multiple_values(0.14159, 1, 2))
end

function TestClient:test_auto_value_type_conversion()
  luaunit.assertEquals('42', self.conn.test_service.float_to_string(42))
  luaunit.assertEquals('6', self.conn.test_service.add_multiple_values(1, 2, 3))
  luaunit.assertErrorMsgContains(
    'TestService.FloatToString() argument 1 must be a number, got a string',
    self.conn.test_service.float_to_string, '42')
end

function TestClient:test_incorrect_parameter_type()
  luaunit.assertErrorMsgContains(
    'TestService.FloatToString() argument 1 must be a number, got a string',
    self.conn.test_service.float_to_string, 'foo')
  luaunit.assertErrorMsgContains(
    'TestService.AddMultipleValues() argument 2 must be a number, got a string',
    self.conn.test_service.add_multiple_values, 0.14159, 'foo', 2)
end

function TestClient:test_properties()
  self.conn.test_service:set_string_property('bar')
  luaunit.assertEquals('bar', self.conn.test_service:get_string_property())

  self.conn.test_service.string_property = 'foo'
  luaunit.assertEquals('foo', self.conn.test_service.string_property)

  luaunit.assertEquals('foo', self.conn.test_service.string_property_private_set)
  self.conn.test_service.string_property_private_get = 'foo'
  obj = self.conn.test_service.create_test_object('bar')
  self.conn.test_service.object_property = obj
  luaunit.assertEquals(obj, self.conn.test_service.object_property)
end

function TestClient:test_extension_members()
  local obj = self.conn.test_service.create_test_object('jeb')
  luaunit.assertEquals('value=jeb42', obj:extension_method(42))
  luaunit.assertEquals('value=jeb', obj.extension_property)
  obj.extension_read_write_property = 42
  luaunit.assertEquals(42, obj.extension_read_write_property)
  -- The extension property writes through to the class's own int_property
  luaunit.assertEquals(42, obj.int_property)
end

function TestClient:test_extension_member_returning_class_from_other_service()
  local obj = self.conn.test_service.create_test_object('jeb')
  local obj2 = obj:extension_method_returning_class_from_other_service()
  luaunit.assertTrue(obj2:is_a(self.conn.test_service2.TestClass2))
  luaunit.assertEquals('value=jeb', obj2.value)
end

function TestClient:test_class_as_return_value()
  local obj = self.conn.test_service.create_test_object('jeb')
  luaunit.assertTrue(obj:is_a(self.conn.test_service.TestClass))
end

function TestClient:test_class_none_value()
  self.conn.test_service.echo_test_object(Types.none)
  luaunit.assertEquals(self.conn.test_service.echo_test_object(Types.none), Types.none)
  obj = self.conn.test_service.create_test_object('bob')
  luaunit.assertEquals('bobnull', obj:object_to_string(Types.none))
  self.conn.test_service.object_property = Types.none
  luaunit.assertEquals(self.conn.test_service.object_property, Types.none)
end

function TestClient:test_nullable_non_class_values()
  -- Nullable value-type, string and collection parameters and return values
  luaunit.assertEquals(42, self.conn.test_service.echo_nullable_int(42))
  luaunit.assertEquals(Types.none, self.conn.test_service.echo_nullable_int(Types.none))
  luaunit.assertEquals('foo', self.conn.test_service.echo_nullable_string('foo'))
  luaunit.assertEquals(Types.none, self.conn.test_service.echo_nullable_string(Types.none))
  luaunit.assertEquals(List{1,2,3}, self.conn.test_service.echo_nullable_list(List{1,2,3}))
  luaunit.assertEquals(Types.none, self.conn.test_service.echo_nullable_list(Types.none))
end

function TestClient:test_non_nullable_parameter_rejects_null()
  -- A null argument to a parameter that is not nullable is rejected by the server
  luaunit.assertError(self.conn.test_service.not_nullable_object, Types.none)
end

function TestClient:test_nullable_class_method()
  local obj = self.conn.test_service.create_test_object('jeb')
  local obj2 = self.conn.test_service.create_test_object('bob')
  luaunit.assertEquals(obj2, obj:echo_nullable_object(obj2))
  luaunit.assertEquals(Types.none, obj:echo_nullable_object(Types.none))
end

function TestClient:test_nullable_class_type_shares_one_class()
  -- A nullable class-typed value is the class of the non-nullable one, and not a second
  -- class built for the nullable declaration
  local obj = self.conn.test_service.create_test_object('jeb')
  luaunit.assertIs(getmetatable(obj:echo_nullable_object(obj)), getmetatable(obj))
end

function TestClient:test_nullable_class_static_method()
  local obj = self.conn.test_service.create_test_object('jeb')
  luaunit.assertEquals(obj, self.conn.test_service.TestClass.static_nullable_object(obj))
  luaunit.assertEquals(Types.none, self.conn.test_service.TestClass.static_nullable_object(Types.none))
end

function TestClient:test_nullable_property()
  local obj = self.conn.test_service.create_test_object('jeb')
  -- object_property is nullable and its setter accepts null
  self.conn.test_service.object_property = Types.none
  luaunit.assertEquals(Types.none, self.conn.test_service.object_property)
  -- nullable_object is nullable for reads, but its setter guards against null
  self.conn.test_service.nullable_object = obj
  luaunit.assertEquals(obj, self.conn.test_service.nullable_object)
  luaunit.assertError(function() self.conn.test_service.nullable_object = Types.none end)
end

function TestClient:test_empty_collection_default()
  -- An empty-collection default is distinguishable from no default: the argument can be
  -- omitted and the empty list is used.
  luaunit.assertEquals(List{}, self.conn.test_service.empty_list_default())
  luaunit.assertEquals(List{'foo', 'bar'}, self.conn.test_service.empty_list_default(List{'foo', 'bar'}))
end

function TestClient:test_class_methods()
  local obj = self.conn.test_service.create_test_object('bob')
  luaunit.assertEquals('value=bob', obj:get_value())
  luaunit.assertEquals('bob3.14159', obj:float_to_string(3.14159))
  local obj2 = self.conn.test_service.create_test_object('bill')
  luaunit.assertEquals('bobbill', obj:object_to_string(obj2))
end

function TestClient:test_class_static_methods()
  luaunit.assertEquals('jeb', self.conn.test_service.TestClass.static_method())
  luaunit.assertEquals('jebbobbill', self.conn.test_service.TestClass.static_method('bob', 'bill'))
end

function TestClient:test_class_properties()
  local obj = self.conn.test_service.create_test_object('jeb')
  obj:set_int_property(0)
  luaunit.assertEquals(0, obj:get_int_property())
  obj.int_property = 0
  luaunit.assertEquals(0, obj.int_property)
  obj.int_property = 42
  luaunit.assertEquals(42, obj.int_property)
  local obj2 = self.conn.test_service.create_test_object('kermin')
  obj.object_property = obj2
  luaunit.assertEquals(obj2._object_id, obj.object_property._object_id)
  obj.string_property_private_get = 'bob'
  luaunit.assertEquals('bob', obj.string_property_private_set)
end

function TestClient:test_optional_arguments()
  luaunit.assertEquals('jebfoobarnull', self.conn.test_service.optional_arguments('jeb'))
  luaunit.assertEquals('jebbobbillnull', self.conn.test_service.optional_arguments('jeb', 'bob', 'bill'))
  local obj = self.conn.test_service.create_test_object('kermin')
  luaunit.assertEquals('jebbobbillkermin', self.conn.test_service.optional_arguments('jeb', 'bob', 'bill', obj))
end

function TestClient:test_blocking_procedure()
  luaunit.assertEquals(0, self.conn.test_service.blocking_procedure(0,0))
  luaunit.assertEquals(1, self.conn.test_service.blocking_procedure(1,0))
  luaunit.assertEquals(1+2, self.conn.test_service.blocking_procedure(2))
  local total = 0
  for i=1,42 do
    total = total + i
  end
  luaunit.assertEquals(total, self.conn.test_service.blocking_procedure(42))
end

function TestClient:test_too_many_arguments()
  -- Calling a procedure with more arguments than it has parameters is not an
  -- error in Lua: excess arguments are discarded by the language before the
  -- client sees them, so there is no failure mode to test.
end

local function filter_private(xs)
  return seq.copy(seq.filter(
    xs:iter(),
    function (x) return not stringx.startswith(x, '_') end)
  )
end

function TestClient:test_client_members()
  -- benchmark is the server-side benchmarks the test server exposes
  -- test_service2 owns the class that an extension member of test_service returns
  luaunit.assertEquals(
    Set{'krpc', 'test_service', 'test_service2', 'benchmark'},
    Set(filter_private(tablex.keys(self.conn)))
  )
end

function TestClient:test_enumerations()
  local enum = self.conn.test_service.TestEnum

  luaunit.assertEquals(enum.value_b, self.conn.test_service.enum_return())
  luaunit.assertEquals(enum.value_a, self.conn.test_service.enum_echo(enum.value_a))
  luaunit.assertEquals(enum.value_b, self.conn.test_service.enum_echo(enum.value_b))
  luaunit.assertEquals(enum.value_c, self.conn.test_service.enum_echo(enum.value_c))

  luaunit.assertEquals(enum.value_a, self.conn.test_service.enum_default_arg(enum.value_a))
  luaunit.assertEquals(enum.value_c, self.conn.test_service.enum_default_arg())
  luaunit.assertEquals(enum.value_b, self.conn.test_service.enum_default_arg(enum.value_b))

  luaunit.assertEquals(List{enum.value_b, enum.value_c}, self.conn.test_service.enum_list_default())
  luaunit.assertEquals(List{enum.value_a, enum.value_b},
                       self.conn.test_service.enum_list_default(List{enum.value_a, enum.value_b}))
end

function TestClient:test_invalid_enum()
  luaunit.assertError(ValueError, self.conn.test_service.TestEnum, 9999)
end

function TestClient:test_collections()
  luaunit.assertEquals(List{}, self.conn.test_service.increment_list(List{}))
  luaunit.assertEquals(List{1,2,3}, self.conn.test_service.increment_list(List{0,1,2}))
  luaunit.assertEquals(Map{}, self.conn.test_service.increment_dictionary(Map{}))
  luaunit.assertEquals(Map{a=1, b=2, c=3}, self.conn.test_service.increment_dictionary(Map{a=0, b=1, c=2}))
  luaunit.assertEquals(Set{}, self.conn.test_service.increment_set(Set{}))
  luaunit.assertEquals(Set{1,2,3}, self.conn.test_service.increment_set(Set{0,1,2}))
  luaunit.assertEquals(List{2,3}, self.conn.test_service.increment_tuple(List{1,2}))
  luaunit.assertError(self.conn.test_service.increment_list, Types.none)
  luaunit.assertError(self.conn.test_service.increment_set, Types.none)
  luaunit.assertError(self.conn.test_service.increment_dictionary, Types.none)
end

function TestClient:test_coercion_does_not_leak_a_global()
  -- Coercing an argument, as passing a plain table where a list is expected does, must
  -- not leave the status of its pcall behind in the global namespace
  rawset(_G, 'ok', nil)
  luaunit.assertEquals(List{1,2,3}, self.conn.test_service.increment_list({0,1,2}))
  luaunit.assertNil(rawget(_G, 'ok'))
end

function TestClient:test_nested_collections()
  luaunit.assertEquals(Map{}, self.conn.test_service.increment_nested_collection(Map{}))
  luaunit.assertEquals(Map{a=List{1, 2}, b=List{}, c=List{3}},
                       self.conn.test_service.increment_nested_collection(Map{a=List{0, 1}, b=List{}, c=List{2}}))
end

function TestClient:test_collections_of_objects()
  local l = self.conn.test_service.add_to_object_list(List{}, "jeb")
  luaunit.assertEquals(1, l:len())
  luaunit.assertEquals("value=jeb", l[1]:get_value())
  local l = self.conn.test_service.add_to_object_list(l, "bob")
  luaunit.assertEquals(2, l:len())
  luaunit.assertEquals("value=jeb", l[1]:get_value())
  luaunit.assertEquals("value=bob", l[2]:get_value())
end

function TestClient:test_structs()
  local service = self.conn.test_service
  local value = service.TestStruct(42, 'jeb', service.TestEnum.value_b, List{1, 2, 3})
  local result = service.struct_echo(value)
  luaunit.assertEquals(value, result)
  luaunit.assertEquals(42, result.int_field)
  luaunit.assertEquals('jeb', result.string_field)
  luaunit.assertEquals(service.TestEnum.value_b, result.enum_field)
  luaunit.assertEquals(List{1, 2, 3}, result.list_field)
end

function TestClient:test_nested_structs()
  local service = self.conn.test_service
  local obj = service.create_test_object('bob')
  local value = service.TestNestedStruct(
    service.TestStruct(1, 'jeb', service.TestEnum.value_a, List{}), obj, 'bill')
  local result = service.nested_struct_echo(value)
  luaunit.assertEquals(value, result)
  luaunit.assertEquals(1, result.struct_field.int_field)
  luaunit.assertEquals(obj, result.object_field)
  luaunit.assertEquals('bill', result.string_field)
end

function TestClient:test_collections_of_structs()
  local service = self.conn.test_service
  local values = List{
    service.TestStruct(0, 'jeb', service.TestEnum.value_c, List{}),
    service.TestStruct(1, 'bob', service.TestEnum.value_c, List{})}
  local result = service.increment_list_of_structs(values)
  luaunit.assertEquals(2, #result)
  luaunit.assertEquals(1, result[1].int_field)
  luaunit.assertEquals(2, result[2].int_field)
end

function TestClient:test_nullable_structs()
  local service = self.conn.test_service
  luaunit.assertEquals(Types.none, service.struct_echo_nullable(Types.none))
  local value = service.TestStruct(1, 'jeb', service.TestEnum.value_a, List{})
  luaunit.assertEquals(value, service.struct_echo_nullable(value))
end

function TestClient:test_nullable_list_elements()
  local service = self.conn.test_service
  luaunit.assertEquals(List{1, Types.none, 3},
                       service.echo_list_of_nullable_ints(List{1, Types.none, 3}))
  local obj = service.create_test_object('jeb')
  luaunit.assertEquals(List{obj, Types.none},
                       service.echo_list_of_nullable_objects(List{obj, Types.none}))
end

function TestClient:test_nullable_positions_coerced_from_a_plain_table()
  -- A plain table is coerced to the collection the position declares, and a null in it is
  -- carried through to the nullable position that holds it
  local service = self.conn.test_service
  luaunit.assertEquals(List{1, Types.none, 3},
                       service.echo_list_of_nullable_ints({1, Types.none, 3}))
  local obj = service.create_test_object('jeb')
  luaunit.assertEquals(List{1, Types.none},
                       service.echo_tuple_with_a_nullable_object({1, Types.none}))
  luaunit.assertEquals(List{List{obj, Types.none}},
                       service.echo_nested_list_of_nullable_objects({{obj, Types.none}}))
end

function TestClient:test_nullable_dictionary_values()
  local service = self.conn.test_service
  local obj = service.create_test_object('jeb')
  local value = Map{a = obj, b = Types.none}
  luaunit.assertEquals(value, service.echo_dictionary_of_nullable_objects(value))
end

function TestClient:test_nullable_tuple_item()
  local service = self.conn.test_service
  local obj = service.create_test_object('jeb')
  luaunit.assertEquals(List{1, obj},
                       service.echo_tuple_with_a_nullable_object(List{1, obj}))
  luaunit.assertEquals(List{1, Types.none},
                       service.echo_tuple_with_a_nullable_object(List{1, Types.none}))
end

function TestClient:test_nullable_nested_list_elements()
  local service = self.conn.test_service
  local obj = service.create_test_object('jeb')
  local value = List{List{obj, Types.none}, List{}}
  luaunit.assertEquals(value, service.echo_nested_list_of_nullable_objects(value))
end

function TestClient:test_nullable_struct_fields()
  local service = self.conn.test_service
  local obj = service.create_test_object('jeb')
  local value = service.TestNullableStruct(1, 2, service.TestEnum.value_b, 'jeb', obj)
  luaunit.assertEquals(value, service.nullable_struct_echo(value))
end

function TestClient:test_null_struct_fields()
  local service = self.conn.test_service
  local value = service.TestNullableStruct(
    1, Types.none, Types.none, Types.none, Types.none)
  local result = service.nullable_struct_echo(value)
  luaunit.assertEquals(1, result.int_field)
  luaunit.assertEquals(Types.none, result.nullable_int_field)
  luaunit.assertEquals(Types.none, result.nullable_enum_field)
  luaunit.assertEquals(Types.none, result.nullable_string_field)
  luaunit.assertEquals(Types.none, result.nullable_object_field)
end

function TestClient:test_nullable_elements_of_a_struct_field()
  local service = self.conn.test_service
  local obj = service.create_test_object('jeb')
  local value = service.TestNestedNullableStruct(List{obj, Types.none}, 'jeb')
  luaunit.assertEquals(value, service.echo_nested_nullable_struct(value))
end

function TestClient:test_struct_default_value()
  local service = self.conn.test_service
  luaunit.assertEquals(
    service.TestStruct(42, 'jeb', service.TestEnum.value_b, List{1, 2, 3}),
    service.struct_default())
end

function TestClient:test_struct_comparison()
  local service = self.conn.test_service
  local a = service.TestStruct(1, 'jeb', service.TestEnum.value_a, List{})
  local b = service.TestStruct(1, 'jeb', service.TestEnum.value_a, List{})
  local c = service.TestStruct(2, 'jeb', service.TestEnum.value_a, List{})

  luaunit.assertEquals(a, b)
  -- Ordered by the fields in turn
  luaunit.assertTrue(a < c)
  luaunit.assertTrue(c > a)
  luaunit.assertTrue(a <= b)
  luaunit.assertTrue(a >= b)
  luaunit.assertFalse(c < a)

  local values = {c, a}
  table.sort(values)
  luaunit.assertEquals(a, values[1])
end

function TestClient:test_collections_default_values()
  luaunit.assertEquals(List{1, false}, self.conn.test_service.tuple_default())
  luaunit.assertEquals(List{1, 2, 3}, self.conn.test_service.list_default())
  luaunit.assertEquals(Set{1, 2, 3}, self.conn.test_service.set_default())
  local m = Map{}
  m:set(1, false)
  m:set(2, true)
  luaunit.assertEquals(m, self.conn.test_service.dictionary_default())
end

function TestClient:test_invalid_operation_exception()
  luaunit.assertErrorMsgContains(
    'KRPC.InvalidOperationException: Invalid operation',
    self.conn.test_service.throw_invalid_operation_exception)
end

function TestClient:test_argument_exception()
  luaunit.assertErrorMsgContains(
    'KRPC.ArgumentException: Invalid argument',
    self.conn.test_service.throw_argument_exception)
end

function TestClient:test_argument_null_exception()
  luaunit.assertErrorMsgContains(
    'KRPC.ArgumentNullException: Value cannot be null',
    self.conn.test_service.throw_argument_null_exception, "")
end

function TestClient:test_argument_out_of_range_exception()
  luaunit.assertErrorMsgContains(
    'KRPC.ArgumentOutOfRangeException: Specified argument was out of the range of valid values.',
    self.conn.test_service.throw_argument_out_of_range_exception, 0)
end

function TestClient:test_custom_exception()
  luaunit.assertErrorMsgContains(
    'TestService.CustomException: A custom kRPC exception',
    self.conn.test_service.throw_custom_exception)
end

-- Collect the public member names of an object: its own string keys, plus those
-- of class tables reachable through the metatable (penlight class instances have
-- their class table as metatable, with parent classes linked through _base).
-- Service members live on per-service class tables, so the instance's own keys
-- are not enough. Penlight's class machinery is excluded, along with
-- underscore-prefixed private members.
local CLASS_LIB_MEMBERS = Set{'is_a', 'class_of', 'cast', 'catch', 'lineinfo'}
local function public_members(obj)
  local members = Set{}
  local function add(t)
    for k,_ in pairs(t) do
      if type(k) == 'string' and k:sub(1,1) ~= '_' and not CLASS_LIB_MEMBERS[k] then
        members = members + Set{k}
      end
    end
  end
  add(obj)
  local cls = getmetatable(obj)
  if rawget(obj, '_init') ~= nil then
    cls = obj  -- obj is itself a class table; walk its parents
  end
  while type(cls) == 'table' do
    add(cls)
    cls = rawget(cls, '_base')
  end
  return members
end

function TestClient:test_krpc_service_members()
  local members = Set.values(public_members(self.conn.krpc))
  table.sort(members)
  luaunit.assertEquals(
    members,
    {'Expression',
     'GameScene',
     'Type',
     'add_event',
     'add_stream',
     'get_client_id',
     'get_client_name',
     'get_clients',
     'get_current_game_scene',
     'get_game_scene',
     'get_paused',
     'get_services',
     'get_status',
     'hold_tick',
     'next_tick',
     'release_tick',
     'remove_stream',
     'set_game_scene',
     'set_paused',
     'set_stream_rate',
     'start_stream'})
end

function TestClient:test_test_service_service_members()
  local members = Set.values(public_members(self.conn.test_service))
  table.sort(members)
  luaunit.assertEquals(
    members,
    {'DeprecatedClass',
     'DeprecatedEnum',
     'DeprecatedStruct',
     'TestClass',
     'TestEnum',
     'TestNestedNullableStruct',
     'TestNestedStruct',
     'TestNullableStruct',
     'TestStruct',
     'add_multiple_values',
     'add_to_object_list',
     'blocking_procedure',
     'bool_to_string',
     'bytes_to_hex_string',
     'counter',
     'counter_struct',
     'create_test_object',
     'deprecated_procedure',
     'deprecated_procedure_no_message',
     'dictionary_default',
     'double_special_defaults',
     'double_to_string',
     'echo_dictionary_of_nullable_objects',
     'echo_list_of_nullable_ints',
     'echo_list_of_nullable_objects',
     'echo_nested_list_of_nullable_objects',
     'echo_nested_nullable_struct',
     'echo_nullable_int',
     'echo_nullable_list',
     'echo_nullable_string',
     'echo_test_object',
     'echo_tuple_with_a_nullable_object',
     'empty_list_default',
     'enum_default_arg',
     'enum_echo',
     'enum_list_default',
     'enum_return',
     'float_special_defaults',
     'float_to_string',
     'get_deprecated_property',
     'get_nullable_object',
     'get_object_property',
     'get_string_property',
     'get_string_property_private_set',
     'increment_dictionary',
     'increment_list',
     'increment_list_of_structs',
     'increment_nested_collection',
     'increment_set',
     'increment_tuple',
     'int32_special_defaults',
     'int32_to_string',
     'int64_special_defaults',
     'int64_to_string',
     'list_default',
     'nested_struct_echo',
     'not_nullable_object',
     'nullable_struct_echo',
     'on_timer',
     'on_timer_using_lambda',
     'optional_arguments',
     'reset_custom_exception_later',
     'reset_invalid_operation_exception_later',
     'return_null_when_not_allowed',
     'set_default',
     'set_deprecated_property',
     'set_nullable_object',
     'set_object_property',
     'set_string_property',
     'set_string_property_private_get',
     'string_to_int32',
     'struct_default',
     'struct_echo',
     'struct_echo_nullable',
     'throw_argument_exception',
     'throw_argument_null_exception',
     'throw_argument_out_of_range_exception',
     'throw_custom_exception',
     'throw_custom_exception_later',
     'throw_invalid_operation_exception',
     'throw_invalid_operation_exception_later',
     'tuple_default',
     'uint32_special_defaults',
     'uint64_special_defaults'})
end

function TestClient:test_test_service_test_class_members()
  local members = Set.values(public_members(self.conn.test_service.TestClass))
  table.sort(members)
  luaunit.assertEquals(
    members,
    {'echo_nullable_object',
     'extension_method',
     'extension_method_returning_class_from_other_service',
     'float_to_string',
     'get_extension_property',
     'get_extension_read_write_property',
     'get_int_property',
     'get_object_property',
     'get_string_property_private_set',
     'get_value',
     'object_to_string',
     'optional_arguments',
     'set_extension_read_write_property',
     'set_int_property',
     'set_object_property',
     'set_string_property_private_get',
     'static_method',
     'static_nullable_object'})
end

function TestClient:test_test_service_enum_members()
  local members = Set.values(public_members(self.conn.test_service.TestEnum))
  table.sort(members)
  luaunit.assertEquals(
    members,
    {'value_a',
     'value_b',
     'value_c'})
end

function TestClient:test_test_service_enum_values()
  luaunit.assertEquals(0, self.conn.test_service.TestEnum.value_a.value)
  luaunit.assertEquals(1, self.conn.test_service.TestEnum.value_b.value)
  luaunit.assertEquals(2, self.conn.test_service.TestEnum.value_c.value)
end

function TestClient:test_line_endings()
  local strings = {
    'foo\nbar',
    'foo\rbar',
    'foo\n\rbar',
    'foo\r\nbar',
    'foo\16bar',
    'foo\19bar',
    'foo\16\19bar',
    'foo\19\16bar'
  }
  for _,s in ipairs(strings) do
    self.conn.test_service.string_property = s
    luaunit.assertEquals(s, self.conn.test_service.string_property)
  end
end

function TestClient:test_types_from_different_connections()
  local conn1 = self:connect()
  local conn2 = self:connect()
  luaunit.assertFalse(conn1.test_service.TestClass == conn2.test_service.TestClass)
  local obj2 = conn2.test_service.TestClass(0)
  local obj1 = conn1._types:coerce_to(obj2, conn1._types:class_type('TestService', 'TestClass'))
  luaunit.assertEquals(obj1, obj2)
  luaunit.assertTrue(conn1.test_service.TestClass:class_of(obj1))
  luaunit.assertTrue(conn2.test_service.TestClass:class_of(obj2))
end

return TestClient
