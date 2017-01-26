local krpc = require 'krpc'
local math = require 'math'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel

local active_engines = {}
for engine in vessel.parts.engines do
    if engine.active and engine.has_fuel then
       active_engines.add(engine)
    end
end

print('Active engines:')
for engine in active_engines do
    print('   ' .. engine.part.title .. ' in stage ' .. engine.part.stage)
end

thrust = 0
fuel_consumption = 0
for engine in active_engines do
    thrust = thrust + engine.thrust
    fuel_consumption = fuel_consumption + engine.thrust / engine.specific_impulse
end
isp = thrust / fuel_consumption

print('Combined vacuum Isp = ' .. isp .. ' seconds')
