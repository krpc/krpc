#!/usr/bin/env python

# Build API docs

import sys
import os
import re
import json
import xml.etree.ElementTree as ElementTree
from Cheetah.Template import Template
from krpc.attributes import Attributes
import krpc.types
from krpc.types import Types
types = Types()

language = sys.argv[1]
src = sys.argv[2]
dst = sys.argv[3]

with open('services.json', 'r') as f:
    services_info = json.load(f)

with open('order.txt', 'r') as f:
    member_ordering = [x.strip() for x in f.readlines()]

def sorted_members(members):
    def key_fn(x):
        if x.name not in member_ordering:
            print 'Don\'t know how to order member', x.name
            return float('inf')
        return member_ordering.index(x.name)
    return sorted(members, key=key_fn)

domains = {
    'python': 'py',
    'lua': 'lua'
}

type_map = {
    'double': 'float',
    'float': 'float',
    'int32': 'int',
    'int64': 'long',
    'uint32': 'int',
    'uint64': 'long',
    'bool': 'bool',
    'string': 'str',
    'bytes': 'bytes',
    'KRPC.Tuple': 'tuple',
    'KRPC.Dictionary': 'dict',
    'KRPC.List': 'list',
    'KRPC.Status': 'KRPC.Status',
    'KRPC.Services': 'KRPC.Services'
}

value_map = {
    'null': 'None',
    'true': 'True',
    'false': 'False'
}

#conf = {
#    'python': {
#        'snake_case': True,
#        'types': {
#            'double': 'float',
#            'int32': 'float',
#            'Dictionary': 'dict',
#            'List': 'list'
#        },
#        'replace': {
#            '``null``': '``None``',
#            '``true``': '``True``',
#            '``false``': '``False``',
#            '``string``': '``str``',
#            '``double``': '``float``',
#            '``int32``': '``int``',
#            ':class:`Dictionary`': '``dict``',
#            ':class:`List`': '``list``'
#        }
#    },
#    'lua': {
#        'snake_case': True,
#        'types': {
#            'double': 'number',
#            'int32': 'number',
#            'Dictionary': 'Map'
#        },
#        'replace': {
#            '``null``': '``nil``',
#            '``true``': '``True``',
#            '``false``': '``False``',
#            '``string``': '``string``',
#            '``double``': '``number``',
#            '``int32``': '``number``',
#            ':class:`Dictionary`': '``Map``',
#            ':class:`List`': '``List``'
#        }
#    }
#}

_regex_multi_uppercase = re.compile(r'([A-Z]+)([A-Z][a-z0-9])')
_regex_single_uppercase = re.compile(r'([a-z0-9])([A-Z])')
_regex_underscores = re.compile(r'(.)_')

def snake_case(name):
    if '.' in name:
        cls,name = name.split('.')
        return cls+'.'+snake_case(name)
    else:
        result = re.sub(_regex_underscores, r'\1__', name)
        result = re.sub(_regex_single_uppercase, r'\1_\2', result)
        return re.sub(_regex_multi_uppercase, r'\1_\2', result).lower()

def _indent(lines, indent):
    result = []
    for line in lines:
        if line:
            result.append((' '*indent)+line)
        else:
            result.append(line)
    return result

#################### REFERENCES ###########################3

refs = {}
def add_ref(name, obj):
    refs[name] = obj

def get_ref(name):
    if name not in refs:
        raise RuntimeError('Ref not found %s' % name)
    return refs[name]

####################### LOW LEVEL PARSING ###############################

def parse_value(x, typ=None):
    if isinstance(typ, krpc.types.ClassType):
        result = 'null'
    else:
        result = str(x)
        if type(x) == float and result.endswith('.0'):
            result = result[:-2]
    if result in value_map:
        return value_map[result]
    return result

def parse_type(typ):
    if typ in type_map:
        return '%s' % type_map[typ]
    if typ.startswith('Class('):
        return ':class:`%s`' % typ[6:-1].split('.')[1]
    if typ.startswith('Enum('):
        return ':class:`%s`' % typ[5:-1].split('.')[1]
    if typ.startswith('Tuple('):
        subtyps = [parse_type(x) for x in typ[6:-1].split(',')]
        return 'tuple of (%s)' % ', '.join(subtyps)
    if typ.startswith('Dictionary('):
        param = typ[11:-1]
        key,value = param.split(',')
        return 'dict from %s to %s' % (parse_type(key), parse_type(value))
    if typ.startswith('List('):
        return 'list of %s' % parse_type(typ[5:-1])
    return typ

def parse_type_ref(typ):
    if typ in type_map:
        return '%s' % type_map[typ]
    if typ.startswith('Class('):
        return '%s' % typ[6:-1].split('.')[1]
    if typ.startswith('Enum('):
        return '%s' % typ[5:-1].split('.')[1]
    if typ.startswith('Tuple('):
        return 'tuple'
    if typ.startswith('Dictionary('):
        param = typ[11:-1]
        key,value = param.split(',')
        return 'dict'
    if typ.startswith('List('):
        return 'list'
    return typ

def parse_parameter_type(pos, typ, attrs):
    typ = types.get_parameter_type(pos, typ, attrs)
    return parse_type_ref(typ.protobuf_type)

def parse_return_type(typ, attrs):
    typ = types.get_return_type(typ, attrs)
    return parse_type(typ.protobuf_type)

def parse_ref(name):
    obj = get_ref(name)

    if isinstance(obj, Service) or isinstance(obj, Class) or isinstance(obj, Enumeration):
        prefix = 'class'
    elif isinstance(obj, StaticMethod) or isinstance(obj, ClassMethod) or isinstance(obj, ClassStaticMethod):
        prefix = 'meth'
    elif isinstance(obj, Property) or isinstance(obj, ClassProperty) or isinstance(obj, EnumerationValue):
        prefix = 'attr'
    elif obj == None:
        prefix = 'ref'
    else:
        raise RuntimeError('Unknown type for ref %s' % str(type(obj)))

    typ,_,name = name.partition(':')
    name,_,_ = name.partition('(')
    name = name.split('.')
    if typ == 'T':
        name = name[1:]
    else: # typ == 'M'
        name = [name[-2],name[-1]]
        name[-1] = snake_case(name[-1])
    ref = '.'.join(name)

    return ':%s:`%s`' % (prefix, ref)

def parse_paramref(name):
    return snake_case(name)

def parse_description_node(node):
    if node.tag == 'see':
        return parse_ref(node.attrib['cref'])
    elif node.tag == 'paramref':
        return '*%s*' % parse_paramref(node.attrib['name'])
    elif node.tag == 'a':
        return '`%s <%s>`_' % (node.text.replace('\n',''), node.attrib['href'])
    elif node.tag == 'c':
        return '``%s``' % parse_value(node.text)
    elif node.tag == 'math':
        return ':math:`%s`' % node.text
    elif node.tag == 'list':
        content = '\n'
        for item in node:
            item_content = parse_description(item[0])
            content += '* %s\n' % '\n'.join(_indent(item_content.split('\n'), 2))[2:].rstrip()
        return content
    else:
        raise RuntimeError('Unhandled node type %s' % node.tag)

def parse_description(node):
    desc = node.text
    for child in node:
        desc += parse_description_node(child)
        if child.tail:
            desc += child.tail
    return desc.strip()

def parse_documentation(xml, info=None):
    if xml.strip() == '':
        return '', []
    parser = ElementTree.XMLParser(encoding='UTF-8')
    root = ElementTree.XML(xml.encode('UTF-8'), parser=parser)
    description = ''
    objs = []
    for node in root:
        if node.tag == 'summary':
            description = parse_description(node)
        elif node.tag == 'param':
            name = node.attrib['name']
            desc = parse_description(node)
            pinfo = None
            for parameter in info['parameters']:
                if parameter['name'] == name:
                    pinfo = parameter
            pos = filter(lambda x: x[1]['name'] == name, enumerate(info['parameters']))[0][0]
            objs.append(Param(name, desc, pos, pinfo, info['attributes']))
        elif node.tag == 'returns':
            objs.append(Returns(parse_description(node)))
        elif node.tag == 'remarks':
            objs.append(Note(parse_description(node)))
        elif node.tag == 'seealso':
            #objs.append(Note(parse_description(node)))
            #TODO: implement
            pass
        else:
            raise RuntimeError('Unhandled documentation tag type %s' % node.tag)
    return description, objs

######### OPTIONS ######################################

class Option(object):
    def __init__(self, option, value):
        self.option = option
        self.value = value

    def __call__(self, indent=0):
        value = ' '.join(self.value.split('\n'))
        return (' '*indent) + (':%s: %s' % (self.option, value)).rstrip()

class Param(Option):
    def __init__(self, name, desc, pos, info, attrs):
        name = snake_case(name)
        option = 'param %s %s' % (parse_parameter_type(pos, info['type'], attrs), name)
        super(Param, self).__init__(option, desc)

class Returns(Option):
    def __init__(self, desc):
        super(Returns, self).__init__('returns', desc)

class ReturnType(Option):
    def __init__(self, typ, attrs):
        super(ReturnType, self).__init__('rtype', parse_return_type(typ, attrs))

######## DIRECTIVES ###########################################

def sort_options(options):
    def key_fn(x):
        if isinstance(x, Param):
            return 0
        elif isinstance(x, Returns):
            return 1
        elif isinstance(x, ReturnType):
            return 2
        else: #Option
            return 3
    return sorted(options, key=key_fn)

class Directive(object):
    def __init__(self, directivename, argument, options, content):
        self.directivename = directivename
        self.argument = argument
        self.options = options
        self.content = content

    def __call__(self, indent=0):
        lines = []
        lines.append('.. %s:: %s' % (self.directivename, self.argument))
        if self.content:
            lines.append('')
            lines.extend(_indent(self.content.split('\n'), 3))
        if len(self.options) > 0:
            for option in sort_options(self.options):
                lines.append('')
                lines.extend(option(3).split('\n'))
        return '\n'.join(_indent((x.rstrip() for x in lines), indent)) + '\n\n'

class Note(Directive):
    def __init__(self, content):
        super(Note, self).__init__('note', '', [], content)

    def __call__(self, indent=0):
        result = super(Note, self).__call__(indent)
        return result.rstrip()

####### PYTHON

def build_args(info, skip=0):
    def _arg(i,x):
        if x['default_argument'] != None:
            typ = types.get_parameter_type(i, x['type'], info['attributes'])
            return '[%s = %s]' % (snake_case(x['name']), parse_value(x['default_argument'], typ))
        return snake_case(x['name'])
    args = [_arg(i,x) for i,x in enumerate(info['parameters'])]
    return '(%s)' % ', '.join(args[skip:])

class Service(Directive):
    def __init__(self, service_name, desc, members):
        self.name = service_name
        self.desc = desc
        self.members = sorted_members(members)
        super(Service, self).__init__('class', service_name, [], None)

    def __call__(self, indent=0):
        desc,_ = parse_documentation(self.desc)
        self.content = desc + '\n\n' + '\n'.join(x() for x in self.members)
        return super(Service, self).__call__(indent)

class ServiceMember(Directive):
    def __init__(self, directivename, service_name, name, args, info):
        self.name = '%s.%s' % (service_name, name)
        name = snake_case(name)
        self.info = info
        super(ServiceMember, self).__init__(directivename, name+args, None, None)

    def __call__(self, indent=0):
        description, options = parse_documentation(self.info['documentation'], self.info)
        if self.info['return_type']:
            options.append(ReturnType(self.info['return_type'], self.info['attributes']))
        self.content = description
        self.options = options
        return super(ServiceMember, self).__call__(indent)

class Property(ServiceMember):
    def __init__(self, service_name, name, info):
        super(Property, self).__init__('attribute', service_name, name, '', info)

    def merge(self, other):
        if not self.info['return_type']:
            self.info['return_type'] = other.info['return_type']
        self.info['attributes'].extend(other.info['attributes'])

class StaticMethod(ServiceMember):
    def __init__(self, service_name, name, info):
        super(StaticMethod, self).__init__('staticmethod', service_name, name, build_args(info), info)

class Enumeration(Directive):
    def __init__(self, service_name, enum_name, desc, values):
        self.name = '%s.%s' % (service_name, enum_name)
        self.desc = desc
        self.values = sorted_members(values)
        super(Enumeration, self).__init__('class', enum_name, [], desc)

    def __call__(self, indent=0):
        desc,_ = parse_documentation(self.desc)
        self.content = desc + '\n\n' + '\n'.join(x(3) for x in self.values)
        return super(Enumeration, self).__call__(indent)

class EnumerationValue(Directive):
    def __init__(self, service_name, enum_name, name, desc):
        self.name = '%s.%s.%s' % (service_name, enum_name, name)
        self.desc = desc
        name = snake_case(name)
        super(EnumerationValue, self).__init__('data', name, [], None)

    def __call__(self, indent=0):
        self.content,_ = parse_documentation(self.desc)
        return super(EnumerationValue, self).__call__(indent)

class Class(Directive):
    def __init__(self, service_name, class_name, desc, members):
        self.name = '%s.%s' % (service_name, class_name)
        self.desc = desc
        self.members = sorted_members(members)
        super(Class, self).__init__('class', class_name, [], desc)

    def __call__(self, indent=0, members=True):
        desc,options = parse_documentation(self.desc)
        self.content = desc + '\n\n' + '\n'.join(x() for x in options)
        if members:
            self.content += '\n\n' + '\n'.join(x() for x in self.members)
        return super(Class, self).__call__(indent)

class ClassMember(Directive):
    def __init__(self, directivename, service_name, class_name, name, args, info):
        self.name = '%s.%s.%s' % (service_name, class_name, name)
        name = snake_case(name)
        self.info = info
        class_name = Attributes.get_class_name(info['attributes'])
        super(ClassMember, self).__init__(directivename, name+args, None, None)

    def __call__(self, indent=0):
        description, options = parse_documentation(self.info['documentation'], self.info)
        if self.info['return_type']:
            options.append(ReturnType(self.info['return_type'], self.info['attributes']))
        self.content = description
        self.options = options
        return super(ClassMember, self).__call__(indent)

class ClassProperty(ClassMember):
    def __init__(self, service_name, class_name, name, info):
        super(ClassProperty, self).__init__('attribute', service_name, class_name, name, '', info)

    def merge(self, other):
        if not self.info['return_type']:
            self.info['return_type'] = other.info['return_type']
        self.info['attributes'].extend(other.info['attributes'])

class ClassMethod(ClassMember):
    def __init__(self, service_name, class_name, name, info):
        super(ClassMethod, self).__init__('method', service_name, class_name, name, build_args(info, 1), info)

class ClassStaticMethod(ClassMember):
    def __init__(self, service_name, class_name, name, info):
        super(ClassStaticMethod, self).__init__('staticmethod', service_name, class_name, name, build_args(info, 1), info)

############################################

def merge_properties(props):
    result = props[0]
    for prop in props[1:]:
        result.merge(prop)
    return result

def process_file(path):

    services = {}
    classes = {}
    enumerations = {}

    for service_name,service_info in services_info.items():

        procedures = {}
        properties = {}
        for procedure_name,procedure_info in service_info['procedures'].items():
            if Attributes.is_a_procedure(procedure_info['attributes']):
                procedures[procedure_name] = StaticMethod(service_name, procedure_name, procedure_info)
            elif Attributes.is_a_property_accessor(procedure_info['attributes']):
                name = Attributes.get_property_name(procedure_info['attributes'])
                if name not in properties:
                    properties[name] = []
                properties[name].append(Property(service_name, name, procedure_info))

        members = []

        for name,procedure in procedures.items():
            add_ref('M:%s.%s' % (service_name, name), procedure)
            members.append(procedure)

        for name,props in properties.items():
            prop = merge_properties(props)
            add_ref('M:%s.%s' % (service_name, name), prop)
            members.append(prop)

        service = Service(service_name, service_info['documentation'], members)
        services[service_name] = service
        add_ref('T:%s' % service_name, service)

        for class_name,class_info in service_info['classes'].items():

            methods = {}
            properties = {}

            for procedure_name,procedure_info in service_info['procedures'].items():
                if Attributes.is_a_class_member(procedure_info['attributes']) and \
                   Attributes.get_class_name(procedure_info['attributes']) == class_name:
                    if Attributes.is_a_class_method(procedure_info['attributes']):
                        name = Attributes.get_class_method_name(procedure_info['attributes'])
                        methods[name] = ClassMethod(service_name, class_name, name, procedure_info)
                    elif Attributes.is_a_class_static_method(procedure_info['attributes']):
                        name = Attributes.get_class_method_name(procedure_info['attributes'])
                        methods[name] = ClassStaticMethod(service_name, class_name, name, procedure_info)
                    elif Attributes.is_a_class_property_accessor(procedure_info['attributes']):
                        name = Attributes.get_class_property_name(procedure_info['attributes'])
                        if name not in properties:
                            properties[name] = []
                        properties[name].append(ClassProperty(service_name, class_name, name, procedure_info))

            members = []

            for name,method in methods.items():
                add_ref('M:%s.%s.%s' % (service_name, class_name, name), method)
                members.append(method)

            for name,props in properties.items():
                prop = merge_properties(props)
                add_ref('M:%s.%s.%s' % (service_name, class_name, name), prop)
                members.append(prop)

            name = 'T:%s.%s' % (service_name, class_name)
            cls = Class(service_name, class_name, class_info['documentation'], members)
            classes[name[2:]] = cls
            add_ref(name, cls)

        for enum_name,enum_info in service_info['enumerations'].items():
            values = []
            for value in enum_info['values']:
                enum_value = EnumerationValue(service_name, enum_name, value['name'], value['documentation'])
                add_ref('M:%s.%s.%s' % (service_name, enum_name, value['name']), enum_value)
                values.append(enum_value)
            name = 'T:%s.%s' % (service_name, enum_name)
            enum = Enumeration(service_name, enum_name, enum_info['documentation'], values)
            enumerations[name[2:]] = enum
            add_ref(name, enum)

    namespace = {
        'language': language,
        'domain': domains[language],
        'services': services,
        'classes': classes,
        'enumerations': enumerations,
        'ref': parse_ref,
        'value': parse_value
    }
    template = Template(file=path, searchList=[namespace])
    return str(template)

for dirname,dirnames,filenames in os.walk(src):
    for filename in filenames:
        if filename.endswith('.tmpl'):
            src_path = os.path.join(dirname, filename)
            dst_path = os.path.join(dst, src_path[len(src)+1:][:-4]+'rst')
            content = process_file(src_path)

            # Skip if already up to date
            if os.path.exists(dst_path):
                try:
                    old_content = open(dst_path, 'r').read()
                    if content == old_content:
                        continue
                except IOError:
                    pass

            # Update
            print src_path+' -> '+dst_path
            if not os.path.exists(os.path.dirname(dst_path)):
                os.makedirs(os.path.dirname(dst_path))
            with open(dst_path, 'w') as f:
                f.write(content)
