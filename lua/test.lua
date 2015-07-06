#!/usr/bin/env lua5.2

local krpc = require "krpc.init"
local schema = require "krpc.schema.KRPC"

local conn = krpc.connect('localhost', 50000, 50001, "TestClient")

local request = schema.Request()
request.service = "KRPC"
request.procedure = "GetStatus"

conn:_send_request(request)
response = conn:_receive_response()

if response:HasField('error') then
  print(response.error)
else
  status = schema.Status()
  status:ParseFromString(response.return_value)

  print(status.version)
end

------

local request = schema.Request()
request.service = "KRPC"
request.procedure = "GetServices"

conn:_send_request(request)
response = conn:_receive_response()

if response:HasField('error') then
  print(response.error)
else
  services = schema.Services()
  services:ParseFromString(response.return_value)

  for k,v in ipairs(services.services) do
    print(v.name)
    print(v.procedures)
    print(v.classes)
    print(v.enumerations)
  end

end
