local luaunit = require 'luaunit'

TestAttributes = require 'krpc.test.test_attributes'
TestClient = require 'krpc.test.test_client'
TestDecoder = require 'krpc.test.test_decoder'
TestEncodeDecode = require 'krpc.test.test_encodedecode'
TestEncoder = require 'krpc.test.test_encoder'
TestObjects = require 'krpc.test.test_objects'
TestPerformance = require 'krpc.test.test_performance'
TestPlatform = require 'krpc.test.test_platform'
TestSnakeCase = require 'krpc.test.test_snake_case'
TestTypes = require 'krpc.test.test_types'

os.exit(luaunit.LuaUnit:run())
