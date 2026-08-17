local luaunit = require 'luaunit'

TestAttributes = require 'krpc.test.test_attributes'
TestClient = require 'krpc.test.test_client'
TestDecoder = require 'krpc.test.test_decoder'
TestEncodeDecode = require 'krpc.test.test_encodedecode'
TestEncoder = require 'krpc.test.test_encoder'
TestLimits = require 'krpc.test.test_limits'
TestObjects = require 'krpc.test.test_objects'
TestPlatform = require 'krpc.test.test_platform'
TestServiceDefinitions = require 'krpc.test.test_service_definitions'
TestSnakeCase = require 'krpc.test.test_snake_case'
TestTypes = require 'krpc.test.test_types'

os.exit(luaunit.LuaUnit:run())
