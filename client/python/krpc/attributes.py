import re

_RE_PROPERTY_NAME = re.compile(r'^Property\.(Get|Set)\((.+)\)$')
_RE_SERVICE_NAME_FROM_CLASS_METHOD = re.compile(r'^Class\.(Static)?Method\(([^,\.]+)\.[^,]+,[^,]+\)$')
_RE_SERVICE_NAME_FROM_CLASS_PROPERTY = re.compile(r'^Class\.Property.(Get|Set)\(([^,\.]+)\.[^,]+,[^,]+\)$')
_RE_CLASS_NAME_FROM_CLASS_METHOD = re.compile(r'^Class\.(Static)?Method\([^,\.]+\.([^,\.]+),[^,]+\)$')
_RE_CLASS_NAME_FROM_CLASS_PROPERTY = re.compile(r'^Class\.Property.(Get|Set)\([^,\.]+\.([^,]+),[^,]+\)$')
_RE_CLASS_METHOD_NAME = re.compile(r'^Class\.(Static)?Method\([^,]+,([^,]+)\)$')
_RE_CLASS_PROPERTY_NAME = re.compile(r'^Class\.Property\.(Get|Set)\([^,]+,([^,]+)\)$')
_RE_RETURN_TYPE = re.compile(r'^ReturnType\.(.+)$')
_RE_PARAMETER_TYPE = re.compile(r'^ParameterType\((\d+)\)\.(.+)$')


class Attributes(object):
    """ Methods for extracting information from procedure attributes """

    @classmethod
    def is_a_procedure(cls, attrs):
        """ Return true if the attributes are for a plain procedure,
            i.e. not a property accessor, class method etc. """
        return not (cls.is_a_property_accessor(attrs) or cls.is_a_class_member(attrs))

    @classmethod
    def is_a_property_accessor(cls, attrs):
        """ Return true if the attributes are for a property getter or setter. """
        return any(attr.startswith('Property.') for attr in attrs)

    @classmethod
    def is_a_property_getter(cls, attrs):
        """ Return true if the attributes are for a property getter. """
        return any(attr.startswith('Property.Get(') for attr in attrs)

    @classmethod
    def is_a_property_setter(cls, attrs):
        """ Return true if the attributes are for a property setter. """
        return any(attr.startswith('Property.Set(') for attr in attrs)

    @classmethod
    def is_a_class_member(cls, attrs):
        """ Return true if the attributes are for a class member. """
        return (
            cls.is_a_class_method(attrs)
            or cls.is_a_class_static_method(attrs)
            or cls.is_a_class_property_accessor(attrs)
        )

    @classmethod
    def is_a_class_method(cls, attrs):
        """ Return true if the attributes are for a class method. """
        return any(attr.startswith('Class.Method(') for attr in attrs)

    @classmethod
    def is_a_class_static_method(cls, attrs):
        """ Return true if the attributes are for a static class method. """
        return any(attr.startswith('Class.StaticMethod(') for attr in attrs)

    @classmethod
    def is_a_class_property_accessor(cls, attrs):
        """ Return true if the attributes are for a class property getter or setter. """
        return any(attr.startswith('Class.Property.') for attr in attrs)

    @classmethod
    def is_a_class_property_getter(cls, attrs):
        """ Return true if the attributes are for a class property getter. """
        return any(attr.startswith('Class.Property.Get(') for attr in attrs)

    @classmethod
    def is_a_class_property_setter(cls, attrs):
        """ Return true if the attributes are for a class property setter. """
        return any(attr.startswith('Class.Property.Set(') for attr in attrs)

    @classmethod
    def get_property_name(cls, attrs):
        """ Return the name of the property handled by a property getter or setter. """
        if cls.is_a_property_accessor(attrs):
            for attr in attrs:
                match = _RE_PROPERTY_NAME.match(attr)
                if match:
                    return match.group(2)
        raise ValueError('Procedure attributes are not a property accessor')

    @classmethod
    def get_service_name(cls, attrs):
        """ Return the name of the service that a class method or property accessor is part of. """
        if cls.is_a_class_method(attrs) or cls.is_a_class_static_method(attrs):
            for attr in attrs:
                match = _RE_SERVICE_NAME_FROM_CLASS_METHOD.match(attr)
                if match:
                    return match.group(2)
        if cls.is_a_class_property_accessor(attrs):
            for attr in attrs:
                match = _RE_SERVICE_NAME_FROM_CLASS_PROPERTY.match(attr)
                if match:
                    return match.group(2)
        raise ValueError('Procedure attributes are not a class method or property accessor')

    @classmethod
    def get_class_name(cls, attrs):
        """ Return the name of the class that a method or property accessor is part of. """
        if cls.is_a_class_method(attrs) or cls.is_a_class_static_method(attrs):
            for attr in attrs:
                match = _RE_CLASS_NAME_FROM_CLASS_METHOD.match(attr)
                if match:
                    return match.group(2)
        if cls.is_a_class_property_accessor(attrs):
            for attr in attrs:
                match = _RE_CLASS_NAME_FROM_CLASS_PROPERTY.match(attr)
                if match:
                    return match.group(2)
        raise ValueError('Procedure attributes are not a class method or property accessor')

    @classmethod
    def get_class_method_name(cls, attrs):
        """ Return the name of a class method. """
        if cls.is_a_class_method(attrs) or cls.is_a_class_static_method(attrs):
            for attr in attrs:
                match = _RE_CLASS_METHOD_NAME.match(attr)
                if match:
                    return match.group(2)
        raise ValueError('Procedure attributes are not a class method')

    @classmethod
    def get_class_property_name(cls, attrs):
        """ Return the name of a class property (for a getter or setter procedure). """
        if cls.is_a_class_property_accessor(attrs):
            for attr in attrs:
                match = _RE_CLASS_PROPERTY_NAME.match(attr)
                if match:
                    return match.group(2)
        raise ValueError('Procedure attributes are not a class property accessor')

    @classmethod
    def get_return_type_attrs(cls, attrs):
        """ Return the attributes for the return type of a procedure. """
        return_type_attrs = []
        for attr in attrs:
            match = _RE_RETURN_TYPE.match(attr)
            if match:
                return_type_attrs.append(match.group(1))
        return return_type_attrs

    @classmethod
    def get_parameter_type_attrs(cls, pos, attrs):
        """ Return the attributes for a specific parameter of a procedure. """
        parameter_type_attrs = []
        for attr in attrs:
            match = _RE_PARAMETER_TYPE.match(attr)
            if match and int(match.group(1)) == pos:
                parameter_type_attrs.append(match.group(2))
        return parameter_type_attrs
