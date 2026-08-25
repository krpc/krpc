-- Benchmarks for the lua client, run by //tools/benchmarks:lua.
--
-- Measures this client from inside it: the round trip for a procedure call, and the cost of
-- a call carrying a collection of values. The runner starts a TestServer, names in the
-- environment where it is listening and over which transport, and reads the JSON printed
-- here. See tools/benchmarks/run_client.py for the contract.

local krpc = require 'krpc.init'
local socket = require 'socket'

-- Duration of one timed loop. Long enough that the clock and a stray scheduling delay do not
-- decide the answer, and short enough that a whole run stays in seconds.
local TARGET_SECONDS = 0.2

-- The number of timed loops to take.
local SAMPLES = 9

-- The duration of one discarded chunk of calls while a case is being settled, the number of
-- them compared at a time, and the margin the last few have to beat for the case to count as
-- still improving.
local SETTLE_CHUNK_SECONDS = 0.1
local SETTLE_CHUNKS = 3
local SETTLE_TOLERANCE = 0.02

-- The time to keep settling one case before measuring it anyway.
local SETTLE_TIMEOUT_SECONDS = 10.0

-- The number of values the collection case sends and gets back. A call carries one value at a
-- time, so the cost of encoding and decoding it is lost in the round trip. A list makes that
-- per-value cost most of what the case measures. The same count for every client, so that the
-- figures can be read against each other.
local LIST_VALUES = 100

-- Wall clock. os.clock measures processor time, which leaves out the wait for the server to
-- answer, and that is most of a round trip.
local function now()
  return socket.gettime()
end

local function port(name, fallback)
  local value = os.getenv(name)
  if value == nil then
    return fallback
  end
  return tonumber(value)
end

-- Call for a short while and return the milliseconds one call took.
local function chunk(call)
  local start = now()
  local calls = 0
  while now() - start < SETTLE_CHUNK_SECONDS do
    call()
    calls = calls + 1
  end
  return (now() - start) * 1e3 / math.max(calls, 1)
end

-- The smallest of the values between the two indices, both inclusive.
local function smallest(values, first, last)
  local best = values[first]
  for index = first, last do
    best = math.min(best, values[index])
  end
  return best
end

-- Make discarded calls until they stop getting faster, and return the cost of one along with
-- whether it got there. A fixed warmup cannot know when it is done: both ends of a round trip
-- get faster under load for a while, as the server's rate control adapts, and a case measured
-- before that finishes returns a curve. Every case is settled on its own, as one already
-- warmed by the case before it settles within a few chunks. The cost of a call falls out of
-- the last chunk, which sizes the timed loops.
local function settle(call)
  local chunks = {chunk(call)}
  local start = now()
  while now() - start < SETTLE_TIMEOUT_SECONDS do
    chunks[#chunks + 1] = chunk(call)
    if #chunks > SETTLE_CHUNKS then
      local split = #chunks - SETTLE_CHUNKS
      local recent = smallest(chunks, split + 1, #chunks)
      local earlier = smallest(chunks, 1, split)
      if recent >= earlier * (1 - SETTLE_TOLERANCE) then
        return recent, true
      end
    end
  end
  -- The chunks it got through, which is fewer than a settle compares when a single chunk ran
  -- longer than the whole timeout.
  return smallest(chunks, math.max(#chunks - SETTLE_CHUNKS + 1, 1), #chunks), false
end

-- Time the call in a loop, several times over, and return milliseconds per call along with
-- whether the case had settled before any of it was measured.
local function timed_loop(call)
  local per_call, settled = settle(call)
  local iterations = math.max(math.floor(TARGET_SECONDS * 1e3 / per_call), 1)

  local samples = {}
  for _ = 1, SAMPLES do
    local loop = now()
    for _ = 1, iterations do
      call()
    end
    samples[#samples + 1] = (now() - loop) * 1e3 / iterations
  end
  return samples, settled
end

local function round_trip(name, call)
  local samples, settled = timed_loop(call)
  return {
    name = name, unit = 'ms', samples = samples, rate = 'calls/s', note = '',
    settled = settled
  }
end

local function emit(cases)
  local parts = {}
  for _, item in ipairs(cases) do
    local samples = {}
    for _, sample in ipairs(item.samples) do
      samples[#samples + 1] = string.format('%.17g', sample)
    end
    parts[#parts + 1] = string.format(
      '{"case": "%s", "unit": "%s", "rate": "%s", "note": "%s", "samples": [%s]%s}',
      item.name, item.unit, item.rate, item.note, table.concat(samples, ', '),
      item.settled and '' or ', "settled": false')
  end
  print(string.format('{"results": [%s]}', table.concat(parts, ', ')))
end

-- Connect over whichever transport the runner started the server with, named by socket path or
-- by port. Both are measured, as the transport is part of what a call costs.
local function connect()
  local rpc_path = os.getenv('RPC_PATH')
  if rpc_path ~= nil then
    return krpc.connect_local('lua_client_benchmark', rpc_path)
  end
  return krpc.connect('lua_client_benchmark', 'localhost',
                      port('RPC_PORT', 50000), port('STREAM_PORT', 50001))
end

local conn = connect()

local values = {}
for i = 1, LIST_VALUES do
  values[i] = i
end

emit({
  round_trip('round trip', function()
    conn.test_service.float_to_string(3.14159)
  end),
  round_trip('round trip, 3 arguments', function()
    conn.test_service.add_multiple_values(3.14159, 1, 2)
  end),
  round_trip(string.format('round trip, list of %d values', LIST_VALUES), function()
    conn.test_service.increment_list(values)
  end),
})

conn:close()
