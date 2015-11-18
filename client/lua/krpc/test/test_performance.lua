local luaunit = require 'luaunit'
local class = require 'pl.class'
local ServerTest = require 'krpc.test.servertest'

local TestPerformance = class(ServerTest)

function TestPerformance:test_performance()
  local n = 100
  local function wrapper()
    self.conn.test_service.float_to_string(3.14159)
  end
  local t1 = os.clock()
  for i=1,n do
    wrapper()
  end
  local t2 = os.clock()
  local t = t2 - t1
  print()
  print(string.format('Total execution time: %.4f seconds', t))
  print(string.format('RPC execution rate: %d per second', (n/t)))
  print(string.format('Latency: %.3f milliseconds', ((t*1000)/n)))
end

return TestPerformance
