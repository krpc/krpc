local krpc = require 'krpc'

local conn = krpc.connect('Debug Example')
conn.debug.infinite_propellant = true
