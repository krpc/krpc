from krpc.attributes import Attributes
from options import *
import utils

def build_args(env, info, skip=0):
    def _arg(i,x):
        if x['default_argument'] != None:
            typ = env.types.get_parameter_type(i, x['type'], info['attributes'])
            return '[%s = %s]' % (env.domain.parse_param(x['name']), env.domain.parse_value(x['default_argument'], typ))
        return env.domain.parse_param(x['name'])
    args = [_arg(i,x) for i,x in enumerate(info['parameters'])]
    return '(%s)' % ', '.join(args[skip:])

def sort_options(env, options):
    def key_fn(x):
        if isinstance(x, Param):
            return 0
        if isinstance(x, ReadOnlyProperty) or \
           isinstance(x, WriteOnlyProperty) or \
           isinstance(x, ReadWriteProperty):
            return 1
        elif isinstance(x, Returns):
            return 2
        elif isinstance(x, ReturnType):
            return 3
        else: #Option
            return 4
    return sorted(options, key=key_fn)

class Directive(object):
    def __init__(self, env, directivename, argument, options, content):
        self.env = env
        self.directivename = directivename
        self.argument = argument
        self.options = options
        self.content = content

    def __call__(self, indent=0):
        lines = []
        lines.append('.. %s:: %s' % (self.directivename, self.argument))
        if self.content:
            lines.append('')
            lines.extend(utils.indent(self.content.split('\n'), 3))
        if len(self.options) > 0:
            for option in sort_options(self.env, self.options):
                lines.append('')
                lines.extend(option(3).split('\n'))
        return '\n'.join(utils.indent((x.rstrip() for x in lines), indent)) + '\n\n'

class Note(Directive):
    def __init__(self, env, content):
        super(Note, self).__init__(env, 'note', '', [], content)

    def __call__(self, indent=0):
        result = super(Note, self).__call__(indent)
        return result.rstrip()

class Service(Directive):
    def __init__(self, env, service_name, desc, members):
        self.name = service_name
        self.desc = desc
        self.members = env.sorted_members(members)
        super(Service, self).__init__(env, 'class', service_name, [], None)

    def __call__(self, indent=0):
        desc,_ = self.env.parse_documentation(self.desc)
        self.content = desc + '\n\n' + '\n'.join(x() for x in self.members)
        return super(Service, self).__call__(indent)

class ServiceMember(Directive):
    def __init__(self, env, directivename, service_name, name, args, info):
        self.name = '%s.%s' % (service_name, name)
        name = env.domain.parse_member(name)
        self.info = info
        super(ServiceMember, self).__init__(env, directivename, name+args, None, None)

    def __call__(self, indent=0, moreoptions=None):
        description, options = self.env.parse_documentation(self.info['documentation'], self.info)
        if moreoptions:
            options.extend(moreoptions)
        if self.info['return_type']:
            options.append(ReturnType(self.env, self.info['return_type'], self.info['attributes']))
        self.content = description
        self.options = options
        return super(ServiceMember, self).__call__(indent)

class Property(ServiceMember):
    def __init__(self, env, service_name, name, info):
        super(Property, self).__init__(env, 'attribute', service_name, name, '', info)

    def merge(self, other):
        if not self.info['return_type']:
            self.info['return_type'] = other.info['return_type']
        self.info['attributes'].extend(other.info['attributes'])

    def __call__(self, indent=0):
        getter = Attributes.is_a_property_getter(self.info['attributes'])
        setter = Attributes.is_a_property_setter(self.info['attributes'])
        options = []
        if getter and setter:
            options = [ReadWriteProperty(self.env)]
        elif getter:
            options = [ReadOnlyProperty(self.env)]
        elif setter:
            options = [WriteOnlyProperty(self.env)]
        return super(Property, self).__call__(indent, options)

class StaticMethod(ServiceMember):
    def __init__(self, env, service_name, name, info):
        super(StaticMethod, self).__init__(env, 'staticmethod', service_name, name, build_args(env, info), info)

class Enumeration(Directive):
    def __init__(self, env, service_name, enum_name, desc, values):
        self.name = '%s.%s' % (service_name, enum_name)
        self.desc = desc
        self.values = env.sorted_members(values)
        super(Enumeration, self).__init__(env, 'class', enum_name, [], desc)

    def __call__(self, indent=0):
        desc,_ = self.env.parse_documentation(self.desc)
        self.content = desc + '\n\n' + ''.join(x() for x in self.values).rstrip()
        return super(Enumeration, self).__call__(indent)

class EnumerationValue(Directive):
    def __init__(self, env, service_name, enum_name, name, desc):
        self.name = '%s.%s.%s' % (service_name, enum_name, name)
        self.desc = desc
        name = env.domain.parse_member(name)
        super(EnumerationValue, self).__init__(env, 'data', name, [], None)

    def __call__(self, indent=0):
        self.content,_ = self.env.parse_documentation(self.desc)
        return super(EnumerationValue, self).__call__(indent)

class Class(Directive):
    def __init__(self, env, service_name, class_name, desc, members):
        self.name = '%s.%s' % (service_name, class_name)
        self.desc = desc
        self.members = env.sorted_members(members)
        super(Class, self).__init__(env, 'class', class_name, [], desc)

    def __call__(self, indent=0, members=True):
        desc,options = self.env.parse_documentation(self.desc)
        self.content = desc
        if len(options) > 0:
            self.content += '\n\n' + '\n'.join(x() for x in options)
        if members:
            self.content += '\n\n' + '\n'.join(x() for x in self.members)
        return super(Class, self).__call__(indent)

class ClassMember(Directive):
    def __init__(self, env, directivename, service_name, class_name, name, args, info):
        self.name = '%s.%s.%s' % (service_name, class_name, name)
        name = env.domain.parse_member(name)
        self.info = info
        class_name = Attributes.get_class_name(info['attributes'])
        super(ClassMember, self).__init__(env, directivename, name+args, None, None)

    def __call__(self, indent=0, moreoptions=None):
        description, options = self.env.parse_documentation(self.info['documentation'], self.info)
        if moreoptions:
            options.extend(moreoptions)
        if self.info['return_type']:
            options.append(ReturnType(self.env, self.info['return_type'], self.info['attributes']))
        self.content = description
        self.options = options
        return super(ClassMember, self).__call__(indent)

class ClassProperty(ClassMember):
    def __init__(self, env, service_name, class_name, name, info):
        super(ClassProperty, self).__init__(env, 'attribute', service_name, class_name, name, '', info)

    def merge(self, other):
        if not self.info['return_type']:
            self.info['return_type'] = other.info['return_type']
        self.info['attributes'].extend(other.info['attributes'])

    def __call__(self, indent=0):
        getter = Attributes.is_a_class_property_getter(self.info['attributes'])
        setter = Attributes.is_a_class_property_setter(self.info['attributes'])
        options = []
        if getter and setter:
            options = [ReadWriteProperty(self.env)]
        elif getter:
            options = [ReadOnlyProperty(self.env)]
        elif setter:
            options = [WriteOnlyProperty(self.env)]
        return super(ClassProperty, self).__call__(indent, options)

class ClassMethod(ClassMember):
    def __init__(self, env, service_name, class_name, name, info):
        super(ClassMethod, self).__init__(env, 'method', service_name, class_name, name, build_args(env, info, 1), info)

class ClassStaticMethod(ClassMember):
    def __init__(self, env, service_name, class_name, name, info):
        super(ClassStaticMethod, self).__init__(env, 'staticmethod', service_name, class_name, name, build_args(env, info, 1), info)
