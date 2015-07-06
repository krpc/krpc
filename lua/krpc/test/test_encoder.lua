local TestEncoder = {}
TestEncoder.__index = TestEncoder

local encoder = require "krpc.encoder"
local platform = require "krpc.platform"

function TestEncoder:test_rpc_hello_message()
  message = encoder.RPC_HELLO_MESSAGE
  assertEquals(message:len(), 12)
  assertEquals(platform.hexlify(message), '48454c4c4f2d525043000000')
end

function TestEncoder:test_stream_hello_message()
  message = encoder.STREAM_HELLO_MESSAGE
  assertEquals(message:len(), 12)
  assertEquals(platform.hexlify(message), '48454c4c4f2d53545245414d')
end

function TestEncoder:test_client_name()
  message = encoder.client_name('foo')
  assertEquals(message:len(), 32)
  assertEquals(platform.hexlify(message), "666f6f" .. string.rep("00", 29))
end

function TestEncoder:test_empty_client_name()
  message = encoder.client_name()
  assertEquals(message:len(), 32)
  assertEquals(platform.hexlify(message), string.rep("00", 32))
end

function TestEncoder:test_long_client_name()
  message = encoder.client_name(string.rep("a", 33))
  assertEquals(message:len(), 32)
  assertEquals(platform.hexlify(message), string.rep("61", 32))
end

return TestEncoder
