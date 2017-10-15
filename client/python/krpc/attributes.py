class Attributes(object):
    """ Methods for extracting information from procedure attributes """

    @classmethod
    def is_a_procedure(cls, name):
        """ Return true if the name is for a plain procedure,
            i.e. not a property accessor, class method etc. """
        return '_' not in name

    @classmethod
    def is_a_property_accessor(cls, name):
        """ Return true if the name is for a property getter or setter. """
        return name.startswith('get_') or name.startswith('set_')

    @classmethod
    def is_a_property_getter(cls, name):
        """ Return true if the name is for a property getter. """
        return name.startswith('get_')

    @classmethod
    def is_a_property_setter(cls, name):
        """ Return true if the name is for a property setter. """
        return name.startswith('set_')

    @classmethod
    def is_a_class_member(cls, name):
        """ Return true if the name is for a class member. """
        return '_' in name and not (name.startswith('get_') or
                                    name.startswith('set_'))

    @classmethod
    def is_a_class_method(cls, name):
        """ Return true if the name is for a class method. """
        return \
            cls.is_a_class_member(name) and \
            ('_static_' not in name) and \
            ('_get_' not in name) and \
            ('_set_' not in name)

    @classmethod
    def is_a_class_static_method(cls, name):
        """ Return true if the name is for a static class method. """
        return '_static_' in name

    @classmethod
    def is_a_class_property_accessor(cls, name):
        """ Return true if the name is for a
            class property getter or setter. """
        return '_get_' in name or '_set_' in name

    @classmethod
    def is_a_class_property_getter(cls, name):
        """ Return true if the name is for a class property getter. """
        return '_get_' in name

    @classmethod
    def is_a_class_property_setter(cls, name):
        """ Return true if the name is for a class property setter. """
        return '_set_' in name

    @classmethod
    def get_property_name(cls, name):
        """ Return the name of the property handled by
            a property getter or setter. """
        if not cls.is_a_property_accessor(name):
            raise ValueError('Procedure is not a property')
        # Strip 'get_' or 'set_' off of the start of the name
        return name[4:]

    @classmethod
    def get_class_name(cls, name):
        """ Return the name of the class that a
            method or property accessor is part of. """
        if not cls.is_a_class_member(name):
            raise ValueError('Procedure is not a class method or property')
        return name.partition('_')[0]

    @classmethod
    def get_class_member_name(cls, name):
        """ Return the name of a class method. """
        if not cls.is_a_class_member(name):
            raise ValueError('Procedure is not a class method or property')
        return name.rpartition('_')[2]
