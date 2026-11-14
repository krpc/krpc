local krpc = require 'krpc'
local socket = require 'socket'
local List = require 'pl.List'

local conn = krpc.connect('Drawing Example')
local vessel = conn.space_center.active_vessel
conn.drawing.add_direction_from_com(List{0, 1, 0}, vessel.reference_frame)
socket.sleep(10)
