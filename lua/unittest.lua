#!/usr/bin/env lua5.2

local luaunit = require "luaunit"

TestClient = require "krpc.test.test_client"
TestDecoder = require "krpc.test.test_decoder"
TestEncoder = require "krpc.test.test_encoder"

os.exit(LuaUnit:run())
