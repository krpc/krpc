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
    parser = argparse.ArgumentParser(description='Generate C# source files for kRPC services')
    parser.add_argument('template', action='store',
                        help='Path to template file')
    parser.add_argument('service', action='store',
                        help='Name of service to generate')
    parser.add_argument('output', action='store',
                        help='Path to output C# source file to')
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

KEYWORDS = set([
    #TODO: add C# keywords here
])

def parse_name(name):
    if name in KEYWORDS:
        name = '%s_' % name
    return name

def parse_type(typ):

    typ_string = 'void'
    typ_set_client = False

    if isinstance(typ, krpc.types.ValueType):
        typ = typ.protobuf_type
        if typ == 'string':
            typ_string = 'String'
        elif typ == 'bytes':
            typ_string = 'byte[]'
        elif typ == 'float':
            typ_string = 'Single'
        elif typ == 'double':
            typ_string = 'Double'
        elif 'int' in typ:
            int_type_map = {
                'int16' : 'Int16',
                'uint16': 'UInt16',
                'int32' : 'Int32',
                'uint32': 'UInt32',
                'int64' : 'Int64',
                'uint64': 'UInt64'
            }
            typ_string = int_type_map[typ]
        else:
            typ_string = typ

    elif isinstance(typ, krpc.types.MessageType):
        typ = typ.protobuf_type
        if typ.startswith('KRPC.'):
            _,_,x = typ.rpartition('.')
            typ_string = 'global::KRPC.Schema.KRPC.%s' % x
        elif typ.startswith('Test.'):
            _,_,x = typ.rpartition('.')
            typ_string = 'global::Test.%s' % x
        else:
            print 'Unknown message type ', typ
            exit(1)

    elif isinstance(typ, krpc.types.ListType):
        typ_string = 'IList<%s>' % parse_type(Types.as_type(typ.protobuf_type[5:-1]))[0]

    elif isinstance(typ, krpc.types.SetType):
        typ_string = 'ISet<%s>' % parse_type(Types.as_type(typ.protobuf_type[4:-1]))[0]

    elif isinstance(typ, krpc.types.DictionaryType):
        key_type,value_type = tuple(typ.protobuf_type[11:-1].split(','))
        typ_string = 'IDictionary<%s,%s>' % (parse_type(Types.as_type(key_type))[0], parse_type(Types.as_type(value_type))[0])

    elif isinstance(typ, krpc.types.TupleType):
        value_types = typ.protobuf_type[6:-1].split(',')
        typ_string = 'Tuple<%s>' % ','.join(parse_type(Types.as_type(t))[0] for t in value_types)

    elif isinstance(typ, krpc.types.ClassType):
        typ_string = 'global::KRPC.Client.Services.%s' % typ.protobuf_type[6:-1]
        typ_set_client = True

    elif isinstance(typ, krpc.types.ProtobufEnumType):
        typ_string = 'global::%s' % typ.protobuf_type

    elif isinstance(typ, krpc.types.EnumType):
        typ_string = 'global::KRPC.Client.Services.%s' % typ.protobuf_type[5:-1]

    else:
        print 'Unknown type ', typ
        exit(1)

    return (typ_string, typ_set_client)

def parse_return_type(procedure):
    return_type = None
    if 'return_type' in procedure is not None:
        typ = Types.get_return_type(procedure['return_type'], procedure['attributes'])
        return_type,return_set_client = parse_type(typ)
    else:
        return_type = 'void'
        return_set_client = False
    return (return_type, return_set_client)

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
    elif isinstance(typ, krpc.types.ValueType) and typ.protobuf_type == 'float':
        return str(value) + "f"
    elif isinstance(typ, krpc.types.ClassType) and value is None:
        return 'null'
    elif isinstance(typ, krpc.types.EnumType):
        return '(global::KRPC.Client.Services.%s)%s' % (typ.protobuf_type[5:-1], value)
    elif isinstance(typ, krpc.types.ProtobufEnumType):
        return '(global::%s)%s' % (typ.protobuf_type, value)
    else:
        return value

def parse_parameters(procedure):
    parameters = []
    for i,parameter in enumerate(procedure['parameters']):
        typ = Types.get_parameter_type(i, parameter['type'], procedure['attributes'])
        info = {
            'name': parse_name(parameter['name']),
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
            return_type,return_set_client = parse_return_type(procedure)
            procedures[parse_name(name)] = {
                'remote_name': name,
                'parameters': parse_parameters(procedure),
                'return_type': return_type,
                'return_set_client': return_set_client
            }

        elif Attributes.is_a_property_getter(procedure['attributes']):
            property_name = parse_name(Attributes.get_property_name(procedure['attributes']))
            return_type,return_set_client = parse_return_type(procedure)
            if property_name not in properties:
                properties[property_name] = {
                    'type': return_type,
                    'getter': False,
                    'setter': False
                }
            properties[property_name]['getter'] = name

        elif Attributes.is_a_property_setter(procedure['attributes']):
            property_name = parse_name(Attributes.get_property_name(procedure['attributes']))
            if property_name not in properties:
                properties[property_name] = {
                    'type': parse_parameters(procedure)[0]['type'],
                    'getter': False,
                    'setter': False
                }
            properties[property_name]['setter'] = name

        elif Attributes.is_a_class_method(procedure['attributes']):
            class_name = Attributes.get_class_name(procedure['attributes'])
            method_name = parse_name(Attributes.get_class_method_name(procedure['attributes']))
            return_type,return_set_client = parse_return_type(procedure)
            classes[class_name]['methods'][method_name] = {
                'remote_name': name,
                'parameters': parse_parameters(procedure)[1:],
                'return_type': return_type,
                'return_set_client': return_set_client
            }

        elif Attributes.is_a_class_static_method(procedure['attributes']):
            class_name = Attributes.get_class_name(procedure['attributes'])
            method_name = parse_name(Attributes.get_class_method_name(procedure['attributes']))
            return_type,return_set_client = parse_return_type(procedure)
            classes[class_name]['static_methods'][method_name] = {
                'remote_name': name,
                'parameters': parse_parameters(procedure),
                'return_type': return_type,
                'return_set_client': return_set_client
            }

        elif Attributes.is_a_class_property_getter(procedure['attributes']):
            class_name = Attributes.get_class_name(procedure['attributes'])
            property_name = parse_name(Attributes.get_class_property_name(procedure['attributes']))
            return_type,return_set_client = parse_return_type(procedure)
            if property_name not in classes[class_name]['properties']:
                classes[class_name]['properties'][property_name] = {
                    'type': return_type,
                    'getter': False,
                    'setter': False
                }
            classes[class_name]['properties'][property_name]['getter'] = name

        elif Attributes.is_a_class_property_setter(procedure['attributes']):
            class_name = Attributes.get_class_name(procedure['attributes'])
            property_name = parse_name(Attributes.get_class_property_name(procedure['attributes']))
            if property_name not in classes[class_name]['properties']:
                classes[class_name]['properties'][property_name] = {
                    'type': parse_parameters(procedure)[1]['type'],
                    'getter': False,
                    'setter': False
                }
            classes[class_name]['properties'][property_name]['setter'] = name

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
