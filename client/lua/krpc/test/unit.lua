-- What the client does regardless of the transport it talks over, and without a server
-- to talk to: encoding and decoding values, the types they are carried as, and reading a
-- service definition.
local luaunit = require 'luaunit'

TestAttributes = require 'krpc.test.test_attributes'
TestDecoder = require 'krpc.test.test_decoder'
TestEncodeDecode = require 'krpc.test.test_encodedecode'
TestEncoder = require 'krpc.test.test_encoder'
TestLimits = require 'krpc.test.test_limits'
TestPlatform = require 'krpc.test.test_platform'
TestServiceDefinitions = require 'krpc.test.test_service_definitions'
TestSnakeCase = require 'krpc.test.test_snake_case'
TestTypes = require 'krpc.test.test_types'

os.exit(luaunit.LuaUnit:run())
