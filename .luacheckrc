-- Configuration for luacheck, which lints the Lua client and the Lua documentation
-- examples

-- Only the globals every supported Lua version has: 5.1 to 5.4, and LuaJIT
std = "min"
color = false
codes = true
max_line_length = 150
-- A method need not use its object
self = false

-- A name starting with an underscore is unused on purpose
ignore = {"21[12]/_.*"}

-- The one module that reaches for what only some versions have
files["client/lua/krpc/compat.lua"] = {std = "max"}

-- The conformance testee runs on the Lua the build supplies, 5.4
files["client/lua/conformance"] = {std = "lua54"}

-- LuaUnit runs the test cases a test file defines as globals
files["client/lua/krpc/test"] = {allow_defined_top = true, ignore = {"131"}}
