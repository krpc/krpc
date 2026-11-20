local krpc = require 'krpc'

local conn = krpc.connect('User Interface Example')
conn.ui.message('Hello, World!', 10)
