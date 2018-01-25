from collections import OrderedDict, defaultdict
from krpc.attributes import Attributes
from krpc.types import Types
from ..utils import as_type, decode_default_value


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
    def __init__(self, name, procedures, classes, enumerations,
                 exceptions, documentation, sort):
        super(Service, self).__init__()
        self.name = name
        self.fullname = name
        self.documentation = documentation
        self.cref = 'T:%s' % name

        members = []
        cprocedures = defaultdict(dict)
        properties = defaultdict(dict)

        for pname, info in procedures.iteritems():
            del info['id']

            if Attributes.is_a_procedure(pname):
                members.append(Procedure(name, pname, **info))

            elif Attributes.is_a_property_accessor(pname):
                propname = Attributes.get_property_name(pname)
                if Attributes.is_a_property_getter(pname):
                    properties[propname]['getter'] = Procedure(
                        name, pname, **info)
                else:
                    properties[propname]['setter'] = Procedure(
                        name, pname, **info)

            elif Attributes.is_a_class_member(pname):
                cname = Attributes.get_class_name(pname)
                cprocedures[cname][pname] = info

        for propname, prop in properties.iteritems():
            members.append(Property(name, propname, **prop))

        self.classes = {
            cname: Class(name, cname, cprocedures[cname], sort=sort, **cinfo)
            for (cname, cinfo) in classes.iteritems()}
        self.enumerations = {
            ename: Enumeration(name, ename, sort=sort, **einfo)
            for (ename, einfo) in enumerations.iteritems()}
        self.exceptions = {
            ename: ExceptionNode(name, ename, **einfo)
            for (ename, einfo) in exceptions.iteritems()}

        self.members = OrderedDict(
            (member.name, member) for member in sorted(members, key=sort))

    def remove(self, member_name):
        if member_name in self.classes:
            del self.classes[member_name]
        if member_name in self.enumerations:
            del self.enumerations[member_name]
        if member_name in self.exceptions:
            del self.exceptions[member_name]
        del self.members[member_name]


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
            if 'id' in pinfo:
                del pinfo['id']

            if Attributes.is_a_class_method(pname):
                members.append(ClassMethod(service_name, name, pname, **pinfo))

            elif Attributes.is_a_class_static_method(pname):
                members.append(ClassStaticMethod(
                    service_name, name, pname, **pinfo))

            elif Attributes.is_a_class_property_accessor(pname):
                propname = Attributes.get_class_member_name(pname)
                proc = Procedure(service_name, pname, **pinfo)
                if Attributes.is_a_class_property_getter(pname):
                    properties[propname]['getter'] = proc
                else:
                    properties[propname]['setter'] = proc

        for propname, prop in properties.iteritems():
            members.append(ClassProperty(service_name, name, propname, **prop))

        self.members = OrderedDict((member.name, member)
                                   for member in sorted(members, key=sort))


class Parameter(Appendable):
    # pylint: disable=redefined-builtin
    def __init__(self, name, type, documentation, default_value=None):
        super(Parameter, self).__init__()
        self.name = name
        self.type = as_type(self.types, type)
        self.has_default_value = default_value is not None
        if default_value is not None:
            default_value = decode_default_value(default_value, self.type)
        self.default_value = default_value
        self.documentation = documentation


class Procedure(Appendable):
    member_type = 'procedure'

    def __init__(self, service_name, name, parameters,
                 documentation, return_type=None, return_is_nullable=False):
        super(Procedure, self).__init__()
        self.service_name = service_name
        self.name = name
        self.fullname = service_name+'.'+name
        if return_type is not None:
            self.return_type = as_type(self.types, return_type)
        else:
            self.return_type = None
        self.return_is_nullable = return_is_nullable
        self.parameters = [Parameter(documentation=documentation, **info)
                           for info in parameters]
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
            self.type = setter.parameters[0].type
            self.documentation = setter.documentation
        self.getter = getter
        self.setter = setter
        self.cref = 'M:%s.%s' % (service_name, name)


class ClassMethod(Appendable):
    member_type = 'class_method'

    def __init__(self, service_name, class_name, name, parameters,
                 documentation, return_type=None, return_is_nullable=False):
        super(ClassMethod, self).__init__()
        name = Attributes.get_class_member_name(name)
        self.service_name = service_name
        self.class_name = class_name
        self.name = name
        self.fullname = service_name+'.'+class_name+'.'+name
        if return_type is not None:
            self.return_type = as_type(self.types, return_type)
        else:
            self.return_type = None
        self.return_is_nullable = return_is_nullable
        self.parameters = [Parameter(documentation=documentation, **info)
                           for info in parameters]
        self.documentation = documentation
        self.cref = 'M:%s.%s.%s' % (service_name, class_name, name)


class ClassStaticMethod(Appendable):
    member_type = 'class_static_method'

    def __init__(self, service_name, class_name, name, parameters,
                 documentation, return_type=None, return_is_nullable=False):
        super(ClassStaticMethod, self).__init__()
        name = Attributes.get_class_member_name(name)
        self.service_name = service_name
        self.class_name = class_name
        self.name = name
        self.fullname = service_name+'.'+class_name+'.'+name
        if return_type is not None:
            self.return_type = as_type(self.types, return_type)
        else:
            self.return_type = None
        self.return_is_nullable = return_is_nullable
        self.parameters = [Parameter(documentation=documentation, **info)
                           for info in parameters]
        self.documentation = documentation
        self.cref = 'M:%s.%s.%s' % (service_name, class_name, name)


class ClassProperty(Appendable):
    member_type = 'class_property'

    def __init__(self, service_name, class_name, name,
                 getter=None, setter=None):
        super(ClassProperty, self).__init__()
        self.service_name = service_name
        self.class_name = class_name
        if getter is not None:
            self.type = getter.return_type
            self.documentation = getter.documentation
        else:
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
        values = (EnumerationValue(service_name, name, **value)
                  for value in values)
        self.values = OrderedDict(
            (v.name, v) for v in sorted(values, key=sort))
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


class ExceptionNode(Appendable):
    def __init__(self, service_name, name, documentation):
        super(ExceptionNode, self).__init__()
        self.service_name = service_name
        self.name = name
        self.fullname = service_name+'.'+name
        self.documentation = documentation
        self.cref = 'T:%s.%s' % (service_name, name)
