local class = require 'pl.class'
local List = require 'pl.List'
local stringx = require 'pl.stringx'
local seq = require 'pl.seq'

local Attributes = class()

local function _or(x, y)
  return x or y
end

local function _startswith(s)
  return function (x) return stringx.startswith(x, s) end
end

local function startswith(s, xs)
  return seq.map(_startswith(s), xs)
end

local function any(xs)
  xs = seq.copy(xs)
  if xs:len() == 0 then
    return false
  end
  if xs:len() == 1 then
    return xs[1]
  end
  return seq.reduce(_or, xs)
end

function Attributes.is_a_procedure(attrs)
  return
    not Attributes.is_a_property_accessor(attrs) and
    not Attributes.is_a_class_method(attrs) and
    not Attributes.is_a_class_static_method(attrs) and
    not Attributes.is_a_class_property_accessor(attrs)
end

function Attributes.is_a_property_accessor(attrs)
  -- Return true if the attributes are for a property getter or setter.
  return any(startswith('Property.', attrs))
end

function Attributes.is_a_property_getter(attrs)
  -- Return true if the attributes are for a property getter.
  return any(startswith('Property.Get(', attrs))
end

function Attributes.is_a_property_setter(attrs)
  -- Return true if the attributes are for a property setter.
  return any(startswith('Property.Set(', attrs))
end

function Attributes.is_a_class_method(attrs)
  -- Return true if the attributes are for a class method.
  return any(startswith('Class.Method(', attrs))
end

function Attributes.is_a_class_static_method(attrs)
  -- Return true if the attributes are for a static class method.
  return any(startswith('Class.StaticMethod(', attrs))
end

function Attributes.is_a_class_property_accessor(attrs)
  -- Return true if the attributes are for a class property getter or setter.
  return any(startswith('Class.Property.', attrs))
end

function Attributes.is_a_class_property_getter(attrs)
  -- Return true if the attributes are for a class property getter.
  return any(startswith('Class.Property.Get(', attrs))
end

function Attributes.is_a_class_property_setter(attrs)
  -- Return true if the attributes are for a class property setter.
  return any(startswith('Class.Property.Set(', attrs))
end

function Attributes.get_property_name(attrs)
  -- Return the name of the property handled by a property getter or setter.
  if Attributes.is_a_property_accessor(attrs) then
    for attr in attrs:iter() do
      local typ =
        attr:match('^Property%.Get%((.+)%)$') or
        attr:match('^Property%.Set%((.+)%)$')
      if typ then
        return typ
      end
    end
  end
  error('Procedure attributes are not a property accessor')
end

function Attributes.get_service_name(attrs)
  -- Return the name of the service that a class method or property accessor is part of.
  if Attributes.is_a_class_method(attrs) or Attributes.is_a_class_static_method(attrs) then
    for attr in attrs:iter() do
      local typ =
        attr:match('^Class%.Method%(([^,%.]+)%.[^,]+,[^,]+%)$') or
        attr:match('^Class%.StaticMethod%(([^,%.]+)%.[^,]+,[^,]+%)$')
      if typ then
        return typ
      end
    end
  end
  if Attributes.is_a_class_property_accessor(attrs) then
    for attr in attrs:iter() do
      local typ =
        attr:match('^Class%.Property.Get%(([^,%.]+)%.[^,]+,[^,]+%)$') or
        attr:match('^Class%.Property.Set%(([^,%.]+)%.[^,]+,[^,]+%)$')
      if typ then
        return typ
      end
    end
  end
  error('Procedure attributes are not a class method or property accessor')
end

function Attributes.get_class_name(attrs)
  -- Return the name of the class that a method or property accessor is part of.
  if Attributes.is_a_class_method(attrs) or Attributes.is_a_class_static_method(attrs) then
    for attr in attrs:iter() do
      local typ =
        attr:match('^Class%.Method%([^,%.]+%.([^,%.]+),[^,]+%)$') or
        attr:match('^Class%.StaticMethod%([^,%.]+%.([^,%.]+),[^,]+%)$')
      if typ then
        return typ
      end
    end
  end
  if Attributes.is_a_class_property_accessor(attrs) then
    for attr in attrs:iter() do
      local typ =
        attr:match('^Class%.Property.Get%([^,%.]+%.([^,]+),[^,]+%)$') or
        attr:match('^Class%.Property.Set%([^,%.]+%.([^,]+),[^,]+%)$')
      if typ then
        return typ
      end
    end
  end
  error('Procedure attributes are not a class method or property accessor')
end

function Attributes.get_class_method_name(attrs)
  -- Return the name of a class method.
  if Attributes.is_a_class_method(attrs) or Attributes.is_a_class_static_method(attrs) then
    for attr in attrs:iter() do
      typ =
        attr:match('^Class%.Method%([^,]+,([^,]+)%)$') or
        attr:match('^Class%.StaticMethod%([^,]+,([^,]+)%)$')
      if typ then
        return typ
      end
    end
  end
  error('Procedure attributes are not a class method')
end

function Attributes.get_class_property_name(attrs)
  -- Return the name of a class property (for a getter or setter procedure).
  if Attributes.is_a_class_property_accessor(attrs) then
    for attr in attrs:iter() do
      typ =
        attr:match('^Class%.Property%.Get%([^,]+,([^,]+)%)$') or
        attr:match('^Class%.Property%.Set%([^,]+,([^,]+)%)$')
      if typ then
        return typ
      end
    end
  end
  error('Procedure attributes are not a class property accessor')
end

function Attributes.get_return_type_attrs(attrs)
  -- Return the attributes for the return type of a procedure.
  local return_type_attrs = List{}
  for attr in attrs:iter() do
    local typ = attr:match('^ReturnType%.(.+)$')
    if typ then
      return_type_attrs:append(typ)
    end
  end
  return return_type_attrs
end

function Attributes.get_parameter_type_attrs(pos, attrs)
  -- Return the attributes for a specific parameter of a procedure.
  local parameter_type_attrs = List{}
  for attr in attrs:iter() do
    local typ = attr:match('^ParameterType%(' .. (pos-1) .. '%)%.(.+)$')
    if typ then
      parameter_type_attrs:append(typ)
    end
  end
  return parameter_type_attrs
end

return Attributes
