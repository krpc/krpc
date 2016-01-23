import collections
from .docparser import DocumentationParser
from krpc.attributes import Attributes
from krpc.types import Types
from krpc.decoder import Decoder

types = Types()

sort_members_failed = []

def sort_members(members, ordering):
    def key_fn((name,member)):
        if member.fullname not in ordering:
            global sort_members_failed
            sort_members_failed.append(member.fullname)
            return 0
        else:
            return ordering.index(member.fullname)
    return collections.OrderedDict(sorted(members.items(), key=key_fn))

class Appendable(object):

    def __init__(self):
        self._appended = []

    def append(self, value):
        self._appended.append(value)

    @property
    def appended(self):
        return '\n\n'.join(self._appended)

class Service(Appendable):
    def __init__(self, name, procedures, classes, enumerations, documentation):
        super(Service, self).__init__()
        self.name = name
        self.fullname = name
        self.documentation = documentation
        self.cref = 'T:%s' % name

        self.members = {}
        for pname,info in procedures.items():
            if Attributes.is_a_procedure(info['attributes']):
                proc = Procedure(name, pname, **info)
                self.members[proc.name] = proc

        properties = {}
        for pname,info in procedures.items():
            if Attributes.is_a_property_accessor(info['attributes']):
                propname = Attributes.get_property_name(info['attributes'])
                if propname not in properties:
                    properties[propname] = {}
                if Attributes.is_a_property_getter(info['attributes']):
                    properties[propname]['getter'] = Procedure(name, pname, **info)
                else:
                    properties[propname]['setter'] = Procedure(name, pname, **info)
        for propname,prop in properties.items():
            prop = Property(name, propname, **prop)
            self.members[prop.name] = prop

        self.classes = {}
        for cname,cinfo in classes.items():
            cprocedures = dict(filter(
                lambda (name,info): Attributes.is_a_class_member(info['attributes']) and \
                Attributes.get_class_name(info['attributes']) == cname, procedures.items()))
            self.classes[cname] = Class(name, cname, cprocedures, **cinfo)

        self.enumerations = dict([(ename,Enumeration(name, ename, **einfo)) for ename,einfo in enumerations.items()])
    def sort(self, ordering):
        self.members = sort_members(self.members, ordering)
        for cls in self.classes.values():
            cls.sort(ordering)
        for enm in self.enumerations.values():
            enm.sort(ordering)

class Class(Appendable):
    def __init__(self, service_name, name, procedures, documentation):
        super(Class, self).__init__()
        self.service_name = service_name
        self.name = name
        self.fullname = service_name+'.'+name
        self.documentation = documentation
        self.cref = 'T:%s.%s' % (service_name, name)

        self.members = {}
        for pname,pinfo in procedures.items():
            if Attributes.is_a_class_member(pinfo['attributes']) and \
               Attributes.get_class_name(pinfo['attributes']) == name:
                if Attributes.is_a_class_method(pinfo['attributes']):
                    member = ClassMethod(service_name, name, pname, **pinfo)
                elif Attributes.is_a_class_static_method(pinfo['attributes']):
                    member = ClassStaticMethod(service_name, name, pname, **pinfo)
                else:
                    continue
                self.members[member.name] = member

        properties = {}
        for pname,pinfo in procedures.items():
            if Attributes.is_a_class_property_accessor(pinfo['attributes']):
                propname = Attributes.get_class_property_name(pinfo['attributes'])
                if propname not in properties:
                    properties[propname] = {}
                if Attributes.is_a_class_property_getter(pinfo['attributes']):
                    properties[propname]['getter'] = Procedure(service_name, pname, **pinfo)
                else:
                    properties[propname]['setter'] = Procedure(service_name, pname, **pinfo)
        for propname,prop in properties.items():
            prop = ClassProperty(service_name, name, propname, **prop)
            self.members[prop.name] = prop

    def sort(self, ordering):
        self.members = sort_members(self.members, ordering)

class Parameter(Appendable):
    def __init__(self, name, position, type, attributes, documentation, default_argument=None):
        self.name = name
        self.type = types.get_parameter_type(position, type, attributes)
        self.has_default_argument = default_argument is not None
        if default_argument is not None:
            default_argument = Decoder.decode(str(bytearray(default_argument)), self.type)
        self.default_argument = default_argument
        self.documentation = documentation

class Procedure(Appendable):
    member_type = 'procedure'

    def __init__(self, service_name, name, parameters, attributes, documentation, return_type=None):
        super(Procedure, self).__init__()
        self.service_name = service_name
        self.name = name
        self.fullname = service_name+'.'+name
        if return_type is not None:
            self.return_type = types.get_return_type(return_type, attributes)
        else:
            self.return_type = None
        self.parameters = [Parameter(position=i, attributes=attributes, documentation=documentation, **info) for i,info in enumerate(parameters)]
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
            self.return_type = types.get_return_type(return_type, attributes)
        else:
            self.return_type = None
        self.parameters = [Parameter(position=i, attributes=attributes, documentation=documentation, **info) for i,info in enumerate(parameters)]
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
            self.return_type = types.get_return_type(return_type, attributes)
        else:
            self.return_type = None
        self.parameters = [Parameter(position=i, attributes=attributes, documentation=documentation, **info) for i,info in enumerate(parameters)]
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
    def __init__(self, service_name, name, values, documentation):
        super(Enumeration, self).__init__()
        self.service_name = service_name
        self.name = name
        self.fullname = service_name+'.'+name
        self.values = {}
        for value in values:
            enm = EnumerationValue(service_name, name, **value)
            self.values[enm.name] = enm
        self.documentation = documentation
        self.cref = 'T:%s.%s' % (service_name, name)

    def sort(self, ordering):
        self.values = sort_members(self.values, ordering)

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
