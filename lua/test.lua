#!/usr/bin/env lua5.2

local krpc = require "krpc.init"
local schema = require "krpc.schema.KRPC"

local conn = krpc.connect('localhost', 50000, 50001, "TestClient")

local request = schema.Request()
request.service = "KRPC"
request.procedure = "GetStatus"

conn:send_request(request)
response = conn:receive_response()

if response:HasField('error') then
  print(response.error)
else
  status = schema.Status()
  status:ParseFromString(response.return_value)

  print(status.version)
end
