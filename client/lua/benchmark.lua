-- Benchmarks for the lua client, run by //tools/benchmarks:lua.
--
-- Measures what this client costs from inside it: the round trip for a procedure call, and
-- what a call carrying a collection of values costs. The runner starts a TestServer, passes the
-- ports in the environment and reads the JSON printed here; see tools/benchmarks/run_client.py
-- for the contract and for what happens to these numbers.

local krpc = require 'krpc.init'
local socket = require 'socket'

-- How long one timed loop should run for. Long enough that the clock and a stray scheduling
-- delay do not decide the answer, short enough that a whole run stays in seconds.
local TARGET_SECONDS = 0.2

-- How many timed loops to take.
local SAMPLES = 9

-- How long one discarded chunk of calls runs for while a case is being settled, how many of
-- them at a time are asked whether it has stopped getting faster, and how much better the last
-- few have to be than everything before them for it to count as still improving.
local SETTLE_CHUNK_SECONDS = 0.1
local SETTLE_CHUNKS = 3
local SETTLE_TOLERANCE = 0.02

-- How long to keep settling one case before measuring it anyway.
local SETTLE_TIMEOUT_SECONDS = 10.0

-- How many values the collection case sends and gets back. A call carries a value at a time, so
-- what one costs to encode and decode is lost in the round trip it arrives in; a list makes that
-- per-value cost most of what the case measures. The same count for every client, so that the
-- figures can be read against each other.
local LIST_VALUES = 100

-- Wall clock. os.clock measures processor time, which leaves out everything spent waiting for
-- the server to answer - that is most of a round trip, so it is the wrong clock entirely.
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

-- Make discarded calls until they stop getting faster, and return what one costs along with
-- whether it got there.
--
-- A fixed warmup cannot know when it is done. Both ends of a round trip get faster under load
-- for a while - the server's rate control adapts to what it is being asked for - and a case
-- measured before that finishes returns a curve rather than a cost. Every case is settled on
-- its own, since one already warmed by the case before it says so within a few chunks. The
-- cost of a call also falls out of the last chunk, which is what sizes the timed loops.
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
  -- However many chunks it got through, which is fewer than a settle compares where a single
  -- one of them ran longer than the whole timeout.
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

local conn = krpc.connect('lua_client_benchmark', 'localhost',
                          port('RPC_PORT', 50000), port('STREAM_PORT', 50001))

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
