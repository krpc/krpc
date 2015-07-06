local TestDecoder = {}
TestDecoder.__index = TestDecoder

local decoder = require "krpc.decoder"

function TestDecoder:test_guid()
  assertEquals('6f271b39-00dd-4de4-9732-f0d3a68838df', decoder.guid('\x39\x1b\x27\x6f\xdd\x00\xe4\x4d\x97\x32\xf0\xd3\xa6\x88\x38\xdf'))
end

return TestDecoder
