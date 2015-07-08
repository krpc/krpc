local luaunit = require 'luaunit'
local class = require 'pl.class'
local schema = require 'krpc.test.Test'

local TestEnum = class()

function TestEnum:test_performance()
  local n = 100
  local function wrapper()
    --self.conn.test_service.float_to_string(float(3.14159))
  end
  --t = timeit.timeit(stmt=wrapper, number=n)
  --print
  --print 'Total execution time: %.2f seconds' % t
  --print 'RPC execution rate: %d per second' % (n/t)
  --print 'Latency: %.3f milliseconds' % ((t*1000)/n)
end

return TestEnum
