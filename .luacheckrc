-- Configuration for luacheck, which lints the Lua client and the Lua documentation
-- examples
std = "lua51"
color = false
codes = true
max_line_length = 150
-- A method need not use its object
self = false

-- A name starting with an underscore is unused on purpose
ignore = {"21[12]/_.*"}

-- LuaUnit runs the test cases a test file defines as globals
files["client/lua/krpc/test"] = {allow_defined_top = true, ignore = {"131"}}
