import array
import base64
import codecs
import collections
import jinja2
from krpc.attributes import Attributes
import krpc.types
import krpc.decoder
from krpc.utils import snake_case
from ..utils import lower_camel_case, indent, single_line

class Generator(object):

    def __init__(self, macro_template, service, definitions):
        self._macro_template = macro_template
        self._service = service
        self._defs = definitions

    types = krpc.types.Types()

    def generate_file(self, path):
        content = self.generate()
        with codecs.open(path, 'w', encoding='utf8') as fp:
            fp.write(content)

    def generate(self):
        context = self.parse_context(self.generate_context())
        loader = jinja2.FileSystemLoader(searchpath='./')
        env = jinja2.Environment(
            loader=loader,
            trim_blocks=True,
            lstrip_blocks=True,
            undefined=jinja2.StrictUndefined
        )
        env.filters['snake_case'] = snake_case
        env.filters['lower_camel_case'] = lower_camel_case
        env.filters['indent'] = indent
        env.filters['singleline'] = single_line
        template = env.from_string(self._macro_template)
        content = template.render(context)
        return content.rstrip()+'\n'

    def generate_context_parameters(self, procedure):
        parameters = []
        for pos, parameter in enumerate(procedure['parameters']):
            typ = self.get_parameter_type(procedure, pos)
            info = {
                'name': self.parse_name(parameter['name']),
                'type': self.parse_parameter_type(typ),
            }
            if 'default_value' in parameter:
                value = self.decode_default_value(parameter['default_value'], typ)
                info['default_value'] = self.parse_default_value(value, typ)
            parameters.append(info)
        return parameters

    def generate_context(self):
        procedures = {}
        properties = {}
        classes = {}
        enumerations = {}

        for name, cls in self._defs['classes'].items():
            classes[name] = {
                'methods': {},
                'static_methods': {},
                'properties': {},
                'documentation': self.parse_documentation(cls['documentation'])
            }

        for name, enumeration in self._defs['enumerations'].items():
            enumerations[name] = {
                'values': [{
                    'name': self.parse_name(x['name']),
                    'value': x['value'],
                    'documentation': self.parse_documentation(x['documentation'])
                } for x in enumeration['values']],
                'documentation': self.parse_documentation(enumeration['documentation'])
            }

        for name, procedure in self._defs['procedures'].items():

            if Attributes.is_a_procedure(procedure['attributes']):
                procedures[self.parse_name(name)] = {
                    'procedure': procedure,
                    'remote_name': name,
                    'parameters': self.generate_context_parameters(procedure),
                    'return_type': self.parse_return_type(self.get_return_type(procedure)),
                    'documentation': self.parse_documentation(procedure['documentation'])
                }

            elif Attributes.is_a_property_getter(procedure['attributes']):
                property_name = self.parse_name(Attributes.get_property_name(procedure['attributes']))
                if property_name not in properties:
                    properties[property_name] = {
                        'type': self.parse_return_type(self.get_return_type(procedure)),
                        'getter': None,
                        'setter': None,
                        'documentation': self.parse_documentation(procedure['documentation'])
                    }
                properties[property_name]['getter'] = {
                    'procedure': procedure,
                    'remote_name': name
                }

            elif Attributes.is_a_property_setter(procedure['attributes']):
                property_name = self.parse_name(Attributes.get_property_name(procedure['attributes']))
                if property_name not in properties:
                    properties[property_name] = {
                        'type': self.generate_context_parameters(procedure)[0]['type'],
                        'getter': None,
                        'setter': None,
                        'documentation': self.parse_documentation(procedure['documentation'])
                    }
                properties[property_name]['setter'] = {
                    'procedure': procedure,
                    'remote_name': name
                }

            elif Attributes.is_a_class_method(procedure['attributes']):
                class_name = Attributes.get_class_name(procedure['attributes'])
                method_name = self.parse_name(Attributes.get_class_method_name(procedure['attributes']))
                classes[class_name]['methods'][method_name] = {
                    'procedure': procedure,
                    'remote_name': name,
                    'parameters': self.generate_context_parameters(procedure)[1:],
                    'return_type': self.parse_return_type(self.get_return_type(procedure)),
                    'documentation': self.parse_documentation(procedure['documentation'])
                }

            elif Attributes.is_a_class_static_method(procedure['attributes']):
                class_name = Attributes.get_class_name(procedure['attributes'])
                method_name = self.parse_name(Attributes.get_class_method_name(procedure['attributes']))
                classes[class_name]['static_methods'][method_name] = {
                    'procedure': procedure,
                    'remote_name': name,
                    'parameters': self.generate_context_parameters(procedure),
                    'return_type': self.parse_return_type(self.get_return_type(procedure)),
                    'documentation': self.parse_documentation(procedure['documentation'])
                }

            elif Attributes.is_a_class_property_getter(procedure['attributes']):
                class_name = Attributes.get_class_name(procedure['attributes'])
                property_name = self.parse_name(Attributes.get_class_property_name(procedure['attributes']))
                if property_name not in classes[class_name]['properties']:
                    classes[class_name]['properties'][property_name] = {
                        'type': self.parse_return_type(self.get_return_type(procedure)),
                        'getter': None,
                        'setter': None,
                        'documentation': self.parse_documentation(procedure['documentation'])
                    }
                classes[class_name]['properties'][property_name]['getter'] = {
                    'procedure': procedure,
                    'remote_name': name
                }

            elif Attributes.is_a_class_property_setter(procedure['attributes']):
                class_name = Attributes.get_class_name(procedure['attributes'])
                property_name = self.parse_name(Attributes.get_class_property_name(procedure['attributes']))
                if property_name not in classes[class_name]['properties']:
                    classes[class_name]['properties'][property_name] = {
                        'type': self.generate_context_parameters(procedure)[1]['type'],
                        'getter': None,
                        'setter': None,
                        'documentation': self.parse_documentation(procedure['documentation'])
                    }
                classes[class_name]['properties'][property_name]['setter'] = {
                    'procedure': procedure,
                    'remote_name': name
                }

        def sort(objs):
            if isinstance(objs, dict):
                return collections.OrderedDict(sorted([(x, sort(y)) for x, y in objs.items()], key=lambda x: x[0]))
            else:
                return objs

        return {
            'service_name': self._service,
            'procedures': sort(procedures),
            'properties': sort(properties),
            'classes': sort(classes),
            'enumerations': sort(enumerations)
        }

    def decode_default_value(self, value, typ):
        value = base64.b64decode(value)
        # Note: following is a workaround for decoding EnumType, as set_values has not been called
        value = array.array('B', value).tostring()
        if not isinstance(typ, krpc.types.EnumType):
            return krpc.decoder.Decoder.decode(value, typ)
        else:
            return krpc.decoder.Decoder.decode(value, self.types.as_type('int32'))

    def get_return_type(self, procedure):
        if 'return_type' not in procedure:
            return None
        return self.types.get_return_type(procedure['return_type'], procedure['attributes'])

    def get_parameter_type(self, procedure, pos):
        return self.types.get_parameter_type(pos, procedure['parameters'][pos]['type'], procedure['attributes'])
