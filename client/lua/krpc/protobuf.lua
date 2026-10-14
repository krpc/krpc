-- The kRPC schema, loaded into a lua-protobuf state of the client's own.
--
-- lua-protobuf keeps its loaded types and its options in one state per lua state, which a
-- script may also load schemas into or set options on. Each call here swaps the client's
-- state in, and the previous one back out, so the two do not affect each other.

local pb = require 'pb'
local slice = require 'pb.slice'
local descriptor = require 'krpc.schema.KRPC'

local protobuf = {}

local _pb_state = pb.state
local _slice_unpack = slice.unpack

local _previous = _pb_state(nil)
assert(pb.load(descriptor))
-- Enumerations decode to their numbers, which the client compares against
pb.option('enum_as_value')
-- Fields encode in the order of their numbers, so a message encodes to the same bytes each time
pb.option('encode_order')
local _state = _pb_state(_previous)

local function _restore(previous, ok, ...)
  _pb_state(previous)
  if not ok then
    error(..., 0)
  end
  return ...
end

--- Call a function with the client's state swapped in
function protobuf.call(func, ...)
  return _restore(_pb_state(_state), pcall(func, ...))
end

local _call = protobuf.call

--- Encode a message, given as a table, of the named type in the krpc.schema package
function protobuf.encode(type_name, message)
  return _call(pb.encode, 'krpc.schema.' .. type_name, message)
end

--- Decode a message of the named type in the krpc.schema package to a table. A scalar field
--- the data leaves out reads as its zero value, and a message field as nil.
function protobuf.decode(type_name, data)
  return _call(pb.decode, 'krpc.schema.' .. type_name, data)
end

--- As decode, for a caller already inside protobuf.call
function protobuf.decode_inside_call(type_name, data)
  return pb.decode('krpc.schema.' .. type_name, data)
end

--- The number of a value of an enumeration in the krpc.schema package
function protobuf.enum(type_name, value_name)
  return _call(pb.enum, 'krpc.schema.' .. type_name, value_name)
end

--- Read values from data, as pb.slice.unpack does. A 64-bit value reads according to the
--- options of the state swapped in, so a decoder calls this inside protobuf.call.
protobuf.unpack = _slice_unpack

return protobuf
