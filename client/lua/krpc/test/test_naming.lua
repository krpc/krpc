local luaunit = require 'luaunit'
local class = require 'pl.class'
local Connection = require 'krpc.connection'

-- The names a service takes on a connection
local TestNaming = class()

function TestNaming:test_members_are_the_public_members_of_a_connection()
  local public = {}
  for key, value in pairs(Connection) do
    if type(key) == 'string' and key:sub(1, 1) ~= '_' and key:upper() ~= key
      and type(value) == 'function' then
      public[#public + 1] = key
    end
  end
  table.sort(public)
  luaunit.assertEquals(public, Connection.MEMBERS)
end

function TestNaming:test_a_service_named_as_a_member_is_escaped()
  luaunit.assertEquals(Connection._service_field('Close'), 'close_')
  luaunit.assertEquals(Connection._service_field('SpaceCenter'), 'space_center')
end

return TestNaming
