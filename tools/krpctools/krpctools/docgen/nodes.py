import base64
from collections import OrderedDict, defaultdict
from krpc.attributes import Attributes
from krpc.types import Types, EnumType
from krpc.decoder import Decoder

class Appendable(object):

    def __init__(self):
        self._appended = []

    types = Types()

    def append(self, value):
        self._appended.append(value)

    @property
    def appended(self):
        return '\n\n'.join(self._appended)

class Service(Appendable):
    def __init__(self, name, procedures, classes, enumerations, documentation, sort):
        super(Service, self).__init__()
        self.name = name
        self.fullname = name
        self.documentation = documentation
        self.cref = 'T:%s' % name

        members = []
        cprocedures = defaultdict(dict)
        properties = defaultdict(dict)

        for pname, info in procedures.iteritems():

            if Attributes.is_a_procedure(info['attributes']):
                members.append(Procedure(name, pname, **info))

            elif Attributes.is_a_property_accessor(info['attributes']):
                propname = Attributes.get_property_name(info['attributes'])
                if Attributes.is_a_property_getter(info['attributes']):
                    properties[propname]['getter'] = Procedure(name, pname, **info)
                else:
                    properties[propname]['setter'] = Procedure(name, pname, **info)

            elif Attributes.is_a_class_member(info['attributes']):
                cname = Attributes.get_class_name(info['attributes'])
                cprocedures[cname][pname] = info

        for propname, prop in properties.iteritems():
            members.append(Property(name, propname, **prop))

        self.classes = {cname: Class(name, cname, cprocedures[cname], sort=sort, **cinfo)
                        for (cname, cinfo) in classes.iteritems()}
        self.enumerations = {ename: Enumeration(name, ename, sort=sort, **einfo)
                             for (ename, einfo) in enumerations.iteritems()}

        self.members = OrderedDict((member.name, member) for member in sorted(members, key=sort))

class Class(Appendable):
    def __init__(self, service_name, name, procedures, documentation, sort):
        super(Class, self).__init__()
        self.service_name = service_name
        self.name = name
        self.fullname = service_name+'.'+name
        self.documentation = documentation
        self.cref = 'T:%s.%s' % (service_name, name)

        members = []
        properties = defaultdict(dict)

        for pname, pinfo in procedures.iteritems():

            #assert(Attributes.is_a_class_member(pinfo['attributes']))
            #assert(Attributes.get_class_name(pinfo['attributes']) == name)

            if Attributes.is_a_class_method(pinfo['attributes']):
                members.append(ClassMethod(service_name, name, pname, **pinfo))

            elif Attributes.is_a_class_static_method(pinfo['attributes']):
                members.append(ClassStaticMethod(service_name, name, pname, **pinfo))

            elif Attributes.is_a_class_property_accessor(pinfo['attributes']):
                propname = Attributes.get_class_property_name(pinfo['attributes'])
                proc = Procedure(service_name, pname, **pinfo)
                if Attributes.is_a_class_property_getter(pinfo['attributes']):
                    properties[propname]['getter'] = proc
                else:
                    properties[propname]['setter'] = proc

        for propname, prop in properties.iteritems():
            members.append(ClassProperty(service_name, name, propname, **prop))

        self.members = OrderedDict((member.name, member) for member in sorted(members, key=sort))

class Parameter(Appendable):
    def __init__(self, name, position, type, attributes, documentation, default_value=None): #pylint: disable=redefined-builtin
        super(Parameter, self).__init__()
        self.name = name
        self.type = self.types.get_parameter_type(position, type, attributes)
        self.has_default_value = default_value is not None
        if default_value is not None:
            # Note: following is a workaround for decoding EnumType, as set_values has not been called
            if not isinstance(self.type, EnumType):
                typ = self.type
            else:
                typ = self.types.as_type('int32')
            default_value = Decoder.decode(str(bytearray(base64.b64decode(default_value))), typ)
        self.default_value = default_value
        self.documentation = documentation

class Procedure(Appendable):
    member_type = 'procedure'

    def __init__(self, service_name, name, parameters, attributes, documentation, return_type=None):
        super(Procedure, self).__init__()
        self.service_name = service_name
        self.name = name
        self.fullname = service_name+'.'+name
        if return_type is not None:
            self.return_type = self.types.get_return_type(return_type, attributes)
        else:
            self.return_type = None
        self.parameters = [Parameter(position=i, attributes=attributes, documentation=documentation, **info)
                           for i, info in enumerate(parameters)]
        self.attributes = attributes
        self.documentation = documentation
        self.cref = 'M:%s.%s' % (service_name, name)

class Property(Appendable):
    member_type = 'property'

    def __init__(self, service_name, name, getter=None, setter=None):
        super(Property, self).__init__()
        self.service_name = service_name
        self.name = name
        self.fullname = service_name+'.'+name
        if getter is not None:
            self.type = getter.return_type
            self.documentation = getter.documentation
        else:
            self.type = setter.parameters[1].type
            self.documentation = setter.documentation
        self.getter = getter
        self.setter = setter
        self.cref = 'M:%s.%s' % (service_name, name)

class ClassMethod(Appendable):
    member_type = 'class_method'

    def __init__(self, service_name, class_name, name, parameters, attributes, documentation, return_type=None):
        super(ClassMethod, self).__init__()
        name = Attributes.get_class_method_name(attributes)
        self.service_name = service_name
        self.class_name = class_name
        self.name = name
        self.fullname = service_name+'.'+class_name+'.'+name
        if return_type is not None:
            self.return_type = self.types.get_return_type(return_type, attributes)
        else:
            self.return_type = None
        self.parameters = [Parameter(position=i, attributes=attributes, documentation=documentation, **info)
                           for i, info in enumerate(parameters)]
        self.attributes = attributes
        self.documentation = documentation
        self.cref = 'M:%s.%s.%s' % (service_name, class_name, name)

class ClassStaticMethod(Appendable):
    member_type = 'class_static_method'

    def __init__(self, service_name, class_name, name, parameters, attributes, documentation, return_type=None):
        super(ClassStaticMethod, self).__init__()
        name = Attributes.get_class_method_name(attributes)
        self.service_name = service_name
        self.class_name = class_name
        self.name = name
        self.fullname = service_name+'.'+class_name+'.'+name
        if return_type is not None:
            self.return_type = self.types.get_return_type(return_type, attributes)
        else:
            self.return_type = None
        self.parameters = [Parameter(position=i, attributes=attributes, documentation=documentation, **info)
                           for i, info in enumerate(parameters)]
        self.attributes = attributes
        self.documentation = documentation
        self.cref = 'M:%s.%s.%s' % (service_name, class_name, name)

class ClassProperty(Appendable):
    member_type = 'class_property'

    def __init__(self, service_name, class_name, name, getter=None, setter=None):
        super(ClassProperty, self).__init__()
        self.service_name = service_name
        self.class_name = class_name
        if getter is not None:
            name = Attributes.get_class_property_name(getter.attributes)
            self.type = getter.return_type
            self.documentation = getter.documentation
        else:
            name = Attributes.get_class_property_name(setter.attributes)
            self.type = setter.parameters[1].type
            self.documentation = setter.documentation
        self.name = name
        self.fullname = service_name+'.'+class_name+'.'+name
        self.getter = getter
        self.setter = setter
        self.cref = 'M:%s.%s.%s' % (service_name, class_name, name)

class Enumeration(Appendable):
    def __init__(self, service_name, name, values, documentation, sort):
        super(Enumeration, self).__init__()
        self.service_name = service_name
        self.name = name
        self.fullname = service_name+'.'+name
        values = (EnumerationValue(service_name, name, **value) for value in values)
        self.values = OrderedDict((v.name, v) for v in sorted(values, key=sort))
        self.documentation = documentation
        self.cref = 'T:%s.%s' % (service_name, name)

class EnumerationValue(Appendable):
    def __init__(self, service_name, enum_name, name, value, documentation):
        super(EnumerationValue, self).__init__()
        self.service_name = service_name
        self.enum_name = enum_name
        self.name = name
        self.fullname = service_name+'.'+enum_name+'.'+name
        self.value = value
        self.documentation = documentation
        self.cref = 'M:%s.%s.%s' % (service_name, enum_name, name)
