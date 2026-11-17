local krpc = require 'krpc'
local conn = krpc.connect()
local vessel = conn.space_center.active_vessel

-- [docs]
-- Print the fields the right-click menu shows for each engine
for _, module in ipairs(vessel.parts:modules_with_name('ModuleEngines')) do
  for _, field in ipairs(module.field_list) do
    if field.visible then
      print(module.part.title .. ' ' .. field.gui_name .. ' ' ..
            field.value)
    end
  end
end

-- Limit the thrust of each engine to half
for _, module in ipairs(vessel.parts:modules_with_name('ModuleEngines')) do
  for _, field in ipairs(module.field_list) do
    if field.name == 'thrustPercentage' then
      field.float_value = 50
    end
  end
end

-- Extend the solar panels, as their Extend button does
local panels = vessel.parts:modules_with_name('ModuleDeployableSolarPanel')
for _, module in ipairs(panels) do
  for _, event in ipairs(module.event_list) do
    if event.name == 'Extend' and event.active then
      event:trigger()
    end
  end
end
-- [/docs]
