local class = require 'pl.class'
local stringx = require 'pl.stringx'

local Attributes = class()

function Attributes.is_a_procedure(name)
  -- Return true if the name is for a plain procedure,
  -- i.e. not a property accessor, class method etc.
  return name:find('_') == nil
end

function Attributes.is_a_property_accessor(name)
  -- Return true if the name is for a property getter or setter.
  return stringx.startswith(name, 'get_') or stringx.startswith(name, 'set_')
end

function Attributes.is_a_property_getter(name)
  -- Return true if the name is for a property getter.
  return stringx.startswith(name, 'get_')
end

function Attributes.is_a_property_setter(name)
  -- Return true if the name is for a property setter.
  return stringx.startswith(name, 'set_')
end

function Attributes.is_a_class_member(name)
  -- Return true if the name is for a class member.
  return name:find('_') and not (stringx.startswith(name, 'get_') or stringx.startswith(name, 'set_'))
end

function Attributes.is_a_class_method(name)
  -- Return true if the name is for a class method.
  return
    Attributes.is_a_class_member(name) and
    not name:find('_static_') and
    not name:find('_get_') and
    not name:find('_set_')
end

function Attributes.is_a_class_static_method(name)
  -- Return true if the name is for a static class method.
  return name:find('_static_') ~= nil
end

function Attributes.is_a_class_property_accessor(name)
  -- Return true if the name is for a class property getter or setter.
  return name:find('_get_') or name:find('_set_')
end

function Attributes.is_a_class_property_getter(name)
  -- Return true if the name is for a class property getter.
  return name:find('_get_') ~= nil
end

function Attributes.is_a_class_property_setter(name)
  -- Return true if the name is for a class property setter.
  return name:find('_set_') ~= nil
end

function Attributes.get_property_name(name)
  -- Return the name of the property handled by a property getter or setter.
  if Attributes.is_a_property_accessor(name) then
    return name:sub(5)
  end
  error('Procedure attributes are not a property accessor')
end

function Attributes.get_class_name(name)
  -- Return the name of the class that a method or property accessor is part of.
  if Attributes.is_a_class_member(name) then
    i, _ = name:find('_')
    return name:sub(1, i-1)
  end
  error('Procedure attributes are not a class method or property accessor')
end

function Attributes.get_class_member_name(name)
  -- Return the name of a class member.
  if Attributes.is_a_class_member(name) then
    name = name:reverse()
    i, _ = name:find('_')
    return name:sub(1, i-1):reverse()
  end
  error('Procedure attributes are not a class method')
end

return Attributes
