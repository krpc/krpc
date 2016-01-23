#!/usr/bin/env python

import os.path
import argparse
import json
import collections
import jinja2
import re
import glob
from krpc.attributes import Attributes
import krpc.types
import krpc.decoder

krpc.types.add_search_path('krpc.test.schema')
Types = krpc.types.Types()

def main():
    parser = argparse.ArgumentParser(description='Generate C++ headers for kRPC services')
    parser.add_argument('template', action='store',
                        help='Path to template file')
    parser.add_argument('service', action='store',
                        help='Name of service to generate')
    parser.add_argument('output', action='store',
                        help='Path to output C++ header file to')
    parser.add_argument('definitions', nargs='*', default=[],
                        help='Paths to services definition files')
    args = parser.parse_args()

    services_info = {}
    for definition in args.definitions:
        for path in glob.glob(definition):
            with open(path, 'r') as f:
                services_info.update(json.load(f))

    if services_info == {}:
        print 'No services found in services definition files'
        exit(1)

    if args.service not in services_info.keys():
        print 'Service \'%s\' not found' % args.service
        exit(1)

    content = generate_service(args.template, args.service, services_info[args.service])
    with open(args.output, 'w') as f:
        f.write(content)

_regex_multi_uppercase = re.compile(r'([A-Z]+)([A-Z][a-z0-9])')
_regex_single_uppercase = re.compile(r'([a-z0-9])([A-Z])')
_regex_underscores = re.compile(r'(.)_')

def snake_case(camel_case):
    """ Convert camel case to snake case, e.g. GetServices -> get_services """
    result = re.sub(_regex_underscores, r'\1__', camel_case)
    result = re.sub(_regex_single_uppercase, r'\1_\2', result)
    return re.sub(_regex_multi_uppercase, r'\1_\2', result).lower()

def cpp_template_fix(typ):
    """ Ensure nested templates are separated by spaces for the C++ parser """
    if typ.endswith('>>'):
        return typ[:-2] + '> >'
    else:
        return typ

KEYWORDS = set([
    'alignas', 'alignof', 'and', 'and_eq', 'asm', 'auto', 'bitand', 'bitor', 'bool', 'break',
    'case', 'catch', 'char', 'char16_t', 'char32_t', 'class', 'compl', 'concept', 'const', 'constexpr',
    'const_cast', 'continue', 'decltype', 'default', 'delete', 'do', 'double', 'dynamic_cast', 'else',
    'enum', 'explicit', 'export', 'extern', 'false', 'float', 'for', 'friend', 'goto', 'if', 'inline',
    'int', 'long', 'mutable', 'namespace', 'new', 'noexcept', 'not', 'not_eq', 'nullptr', 'operator',
    'or', 'or_eq', 'private', 'protected', 'public', 'register', 'reinterpret_cast', 'requires',
    'return', 'short', 'signed', 'sizeof', 'static', 'static_assert', 'static_cast', 'struct', 'switch',
    'template', 'this', 'thread_local', 'throw', 'true', 'try', 'typedef', 'typeid', 'typename', 'union',
    'unsigned', 'using', 'virtual', 'void', 'volatile', 'wchar_t', 'while', 'xor', 'xor_eq'
])

def parse_name(name):
    name = snake_case(name)
    if name in KEYWORDS:
        name = '%s_' % name
    return name

def parse_type(typ):

    typ_string = 'void'
    typ_decode_fn = 'decode'
    typ_set_client = False

    if isinstance(typ, krpc.types.ValueType):
        typ = typ.protobuf_type
        if typ == 'string' or typ == 'bytes':
            typ_string = 'std::string'
        elif 'int' in typ:
            typ_string = 'google::protobuf::%s' % typ
        else:
            typ_string = typ

    elif isinstance(typ, krpc.types.MessageType):
        typ = typ.protobuf_type
        if typ.startswith('KRPC.'):
            _,_,x = typ.rpartition('.')
            typ_string = 'krpc::schema::%s' % x
        elif typ.startswith('Test.'):
            _,_,x = typ.rpartition('.')
            typ_string = 'Test::%s' % x
        else:
            print 'Unknown message type ', typ
            exit(1)

    elif isinstance(typ, krpc.types.ListType):
        typ_string = cpp_template_fix('std::vector<%s>' % parse_type(Types.as_type(typ.protobuf_type[5:-1]))[0])

    elif isinstance(typ, krpc.types.SetType):
        typ_string = cpp_template_fix('std::set<%s>' % parse_type(Types.as_type(typ.protobuf_type[4:-1]))[0])

    elif isinstance(typ, krpc.types.DictionaryType):
        key_type,value_type = tuple(typ.protobuf_type[11:-1].split(','))
        typ_string = cpp_template_fix('std::map<%s,%s>' % (parse_type(Types.as_type(key_type))[0], parse_type(Types.as_type(value_type))[0]))

    elif isinstance(typ, krpc.types.TupleType):
        value_types = typ.protobuf_type[6:-1].split(',')
        typ_string = cpp_template_fix('boost::tuple<%s>' % ','.join(parse_type(Types.as_type(t))[0] for t in value_types))

    elif isinstance(typ, krpc.types.ClassType):
        typ_string = typ.protobuf_type[6:-1].replace('.','::')
        typ_set_client = True

    elif isinstance(typ, krpc.types.ProtobufEnumType):
        typ_string = typ.protobuf_type.replace('.','::')
        x,_,y = typ_string.partition('::')
        typ_string = x.lower() + '::' + y
        typ_decode_fn = 'decode_enum'

    elif isinstance(typ, krpc.types.EnumType):
        typ_string = typ.protobuf_type[5:-1].replace('.','::')

    else:
        print 'Unknown type ', typ
        exit(1)

    return (typ_string, typ_decode_fn, typ_set_client)

def parse_return_type(procedure):
    return_type = None
    if 'return_type' in procedure is not None:
        typ = Types.get_return_type(procedure['return_type'], procedure['attributes'])
        return_type,return_decode_fn,return_set_client = parse_type(typ)
    else:
        return_type = 'void'
        return_decode_fn = 'decode'
        return_set_client = False
    return (return_type, return_decode_fn, return_set_client)

def parse_parameter_type(typ):
    return parse_type(typ)[0]

def parse_default_argument(value, typ):
    #TODO: following is a workaround for decoding EnumType, as set_values has not been called
    if not isinstance(typ, krpc.types.EnumType):
        value = krpc.decoder.Decoder.decode(str(bytearray(value)), typ)
    else:
        value = krpc.decoder.Decoder.decode(str(bytearray(value)), Types.as_type('int32'))
    if isinstance(typ, krpc.types.ValueType) and typ.protobuf_type == 'string':
        return '"%s"' % value
    if isinstance(typ, krpc.types.ValueType) and typ.protobuf_type == 'bool':
        if value:
            return 'true'
        else:
            return 'false'
    elif isinstance(typ, krpc.types.ClassType) and value is None:
        return parse_parameter_type(typ) + '()'
    elif isinstance(typ, krpc.types.EnumType):
        return 'static_cast<%s>(%s)' % (parse_parameter_type(typ), value)
    elif isinstance(typ, krpc.types.ProtobufEnumType):
        return 'static_cast<%s>(%s)' % (parse_parameter_type(typ), value)
    else:
        return value

def parse_parameters(procedure):
    parameters = []
    for i,parameter in enumerate(procedure['parameters']):
        typ = Types.get_parameter_type(i, parameter['type'], procedure['attributes'])
        info = {
            'name': snake_case(parameter['name']),
            'type': parse_parameter_type(typ),
        }
        if 'default_argument' in parameter:
            info['default_argument'] = parse_default_argument(parameter['default_argument'], typ)
        parameters.append(info)
    return parameters

def generate_service(template_file, service_name, info):

    procedures = {}
    properties = {}
    classes = {}
    enumerations = {}

    for name,cls in info['classes'].items():
        classes[name] = {'methods': {}, 'static_methods': {}, 'properties': {}}

    for name,enumeration in info['enumerations'].items():
        enumerations[name] = [{'name': parse_name(x['name']), 'value': x['value']} for x in enumeration['values']]

    for name,procedure in info['procedures'].items():

        if Attributes.is_a_procedure(procedure['attributes']):
            return_type,return_decode_fn,return_set_client = parse_return_type(procedure)
            procedures[parse_name(name)] = {
                'remote_name': name,
                'parameters': parse_parameters(procedure),
                'return_type': return_type,
                'return_decode_fn': return_decode_fn,
                'return_set_client': return_set_client
            }

        elif Attributes.is_a_property_getter(procedure['attributes']):
            property_name = Attributes.get_property_name(procedure['attributes'])
            return_type,return_decode_fn,return_set_client = parse_return_type(procedure)
            properties[parse_name(property_name)] = {
                'remote_name': name,
                'parameters': [],
                'return_type': return_type,
                'return_decode_fn': return_decode_fn,
                'return_set_client': return_set_client
            }

        elif Attributes.is_a_property_setter(procedure['attributes']):
            property_name = Attributes.get_property_name(procedure['attributes'])
            properties['set_%s' % parse_name(property_name)] = {
                'remote_name': name,
                'parameters': parse_parameters(procedure),
                'return_type': 'void',
                'return_decode_fn': 'decode',
                'return_set_client': False
            }

        elif Attributes.is_a_class_method(procedure['attributes']):
            class_name = Attributes.get_class_name(procedure['attributes'])
            method_name = parse_name(Attributes.get_class_method_name(procedure['attributes']))
            return_type,return_decode_fn,return_set_client = parse_return_type(procedure)
            classes[class_name]['methods'][method_name] = {
                'remote_name': name,
                'parameters': parse_parameters(procedure)[1:],
                'return_type': return_type,
                'return_decode_fn': return_decode_fn,
                'return_set_client': return_set_client
            }

        elif Attributes.is_a_class_static_method(procedure['attributes']):
            class_name = Attributes.get_class_name(procedure['attributes'])
            method_name = parse_name(Attributes.get_class_method_name(procedure['attributes']))
            return_type,return_decode_fn,return_set_client = parse_return_type(procedure)
            classes[class_name]['static_methods'][method_name] = {
                'remote_name': name,
                'parameters': parse_parameters(procedure),
                'return_type': return_type,
                'return_decode_fn': return_decode_fn,
                'return_set_client': return_set_client
            }

        elif Attributes.is_a_class_property_getter(procedure['attributes']):
            class_name = Attributes.get_class_name(procedure['attributes'])
            property_name = parse_name(Attributes.get_class_property_name(procedure['attributes']))
            return_type,return_decode_fn,return_set_client = parse_return_type(procedure)
            classes[class_name]['properties'][property_name] = {
                'remote_name': name,
                'parameters': [],
                'return_type': return_type,
                'return_decode_fn': return_decode_fn,
                'return_set_client': return_set_client
            }

        elif Attributes.is_a_class_property_setter(procedure['attributes']):
            class_name = Attributes.get_class_name(procedure['attributes'])
            property_name = 'set_%s' % parse_name(Attributes.get_class_property_name(procedure['attributes']))
            classes[class_name]['properties'][property_name] = {
                'remote_name': name,
                'parameters': [parse_parameters(procedure)[1]],
                'return_type': 'void',
                'return_decode_fn': 'decode',
                'return_set_client': False
            }

    def sort(d):
        if type(d) == dict:
            return collections.OrderedDict(sorted([(x,sort(y)) for x,y in d.items()], key=lambda (k,v): k))
        else:
            return d

    context = {
        'service_name': service_name,
        'procedures': sort(procedures),
        'properties': sort(properties),
        'classes': sort(classes),
        'enumerations': sort(enumerations)
    }

    loader = jinja2.FileSystemLoader(searchpath='./' )
    env = jinja2.Environment(
        loader=loader,
        trim_blocks=True,
        lstrip_blocks=True,
        undefined=jinja2.StrictUndefined
    )
    template = env.get_template(template_file)
    content = template.render(context)
    return content.rstrip()+'\n'

if __name__ == '__main__':
    main()
