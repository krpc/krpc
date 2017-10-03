local krpc = require 'krpc'
local math = require 'math'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel

local active_engines = {}
for _,engine in ipairs(vessel.parts.engines) do
    if engine.active and engine.has_fuel then
       table.insert(active_engines, engine)
    end
end

print('Active engines:')
for _,engine in ipairs(active_engines) do
    print('   ' .. engine.part.title .. ' in stage ' .. engine.part.stage)
end

thrust = 0
fuel_consumption = 0
for _,engine in ipairs(active_engines) do
    thrust = thrust + engine.thrust
    fuel_consumption = fuel_consumption + engine.thrust / engine.specific_impulse
end
isp = thrust / fuel_consumption

print(string.format('Combined vacuum Isp = %.1f seconds', isp))
