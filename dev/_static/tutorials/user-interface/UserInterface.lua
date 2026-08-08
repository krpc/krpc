local krpc = require 'krpc'
local platform = require 'krpc.platform'
local List = require 'pl.List'
local conn = krpc.connect('User Interface Example')
local canvas = conn.ui.stock_canvas

-- Get the size of the game window in pixels
local screen_size = canvas.rect_transform.size

-- Add a panel to contain the UI elements. The user can drag it around the
-- screen with the mouse.
local panel = canvas:add_panel()
panel.draggable = true

-- Position the panel on the left of the screen
local rect = panel.rect_transform
rect.size = List{200, 120}
rect.position = List{110-(screen_size[1]/2), 0}

-- Lay the contents of the panel out in a column
local layout = panel:add_vertical_layout()
layout.padding = List{10, 10, 10, 10}
layout.spacing = 6

-- Add a button to set the throttle to maximum, with a tooltip shown while the
-- mouse rests on it
local button = panel:add_button("Full Throttle")
button.tooltip = "Set the throttle to maximum"
button.layout_element.preferred_size = List{-1, 30}

-- Add a slider to set the throttle by hand
local slider = panel:add_slider()
slider.layout_element.preferred_size = List{-1, 20}

-- Add some text displaying the total engine thrust
local text = panel:add_text("Thrust: 0 kN")
text.color = List{1, 1, 1, 1}
text.size = 18

local vessel = conn.space_center.active_vessel
while true do
    -- Handle the throttle button being clicked
    if button.clicked then
        vessel.control.throttle = 1
        slider.value = 1
        button.clicked = false
    end

    -- Handle the user moving the throttle slider
    if slider.changed then
        vessel.control.throttle = slider.value
        slider.changed = false
    end

    -- Update the thrust text
    text.content = string.format('Thrust: %.1f kN', vessel.thrust/1000)

    platform.sleep(0.1)
end
