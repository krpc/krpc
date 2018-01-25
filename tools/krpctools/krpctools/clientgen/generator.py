import codecs
import collections
import jinja2
from krpc.attributes import Attributes
from krpc.types import Types
from krpc.utils import snake_case
from ..utils import \
    lower_camel_case, indent, single_line, \
    as_type, decode_default_value


class Generator(object):

    def __init__(self, macro_template, service, definitions):
        self._macro_template = macro_template
        self._service = service
        self._defs = definitions

    @property
    def service_name(self):
        return self._service

    types = Types()

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
        for parameter in procedure['parameters']:
            typ = as_type(self.types, parameter['type'])
            info = {
                'name': self.parse_name(parameter['name']),
                'type': self.parse_parameter_type(typ),
            }
            if 'default_value' in parameter:
                value = decode_default_value(parameter['default_value'], typ)
                info['default_value'] = self.parse_default_value(value, typ)
            parameters.append(info)
        return parameters

    def _get_defs(self, key):
        return self._defs.get(key, {}).items()

    def generate_context(self):
        context = {
            'service_name': self._service,
            'service_id': self._defs['id'],
            'procedures': {},
            'properties': {},
            'classes': {},
            'enumerations': {},
            'exceptions': {}
        }

        for name, cls in self._get_defs('classes'):
            context['classes'][name] = {
                'methods': {},
                'static_methods': {},
                'properties': {},
                'documentation': self.parse_documentation(
                    cls['documentation'])
            }

        for name, enumeration in self._get_defs('enumerations'):
            context['enumerations'][name] = {
                'values': [{
                    'name': self.parse_name(x['name']),
                    'value': x['value'],
                    'documentation': self.parse_documentation(
                        x['documentation'])
                } for x in enumeration['values']],
                'documentation': self.parse_documentation(
                    enumeration['documentation'])
            }

        for name, exception in self._get_defs('exceptions'):
            context['exceptions'][name] = {
                'documentation': self.parse_documentation(
                    exception['documentation'])
            }

        for name, procedure in self._get_defs('procedures'):
            if Attributes.is_a_procedure(name):
                context['procedures'][self.parse_name(name)] = {
                    'procedure': procedure,
                    'remote_name': name,
                    'remote_id': procedure['id'],
                    'parameters': self.generate_context_parameters(
                        procedure),
                    'return_type': self.parse_return_type(
                        self.get_return_type(procedure)),
                    'documentation': self.parse_documentation(
                        procedure['documentation'])
                }

            elif Attributes.is_a_property_getter(name):
                property_name = self.parse_name(
                    Attributes.get_property_name(name))
                if property_name not in context['properties']:
                    context['properties'][property_name] = {
                        'type': self.parse_return_type(
                            self.get_return_type(procedure)),
                        'getter': None,
                        'setter': None,
                        'documentation': self.parse_documentation(
                            procedure['documentation'])
                    }
                context['properties'][property_name]['getter'] = {
                    'procedure': procedure,
                    'remote_name': name,
                    'remote_id': procedure['id']
                }

            elif Attributes.is_a_property_setter(name):
                property_name = self.parse_name(
                    Attributes.get_property_name(name))
                params = self.generate_context_parameters(procedure)
                if property_name not in context['properties']:
                    context['properties'][property_name] = {
                        'type': params[0]['type'],
                        'getter': None,
                        'setter': None,
                        'documentation': self.parse_documentation(
                            procedure['documentation'])
                    }
                context['properties'][property_name]['setter'] = {
                    'procedure': procedure,
                    'remote_name': name,
                    'remote_id': procedure['id']
                }

            elif Attributes.is_a_class_method(name):
                class_name = Attributes.get_class_name(name)
                method_name = self.parse_name(
                    Attributes.get_class_member_name(name))
                params = self.generate_context_parameters(procedure)
                context['classes'][class_name]['methods'][method_name] = {
                    'procedure': procedure,
                    'remote_name': name,
                    'remote_id': procedure['id'],
                    'parameters': params[1:],
                    'return_type': self.parse_return_type(
                        self.get_return_type(procedure)),
                    'documentation': self.parse_documentation(
                        procedure['documentation'])
                }

            elif Attributes.is_a_class_static_method(name):
                class_name = Attributes.get_class_name(name)
                cls = context['classes'][class_name]
                method_name = self.parse_name(
                    Attributes.get_class_member_name(name))
                cls['static_methods'][method_name] = {
                    'procedure': procedure,
                    'remote_name': name,
                    'remote_id': procedure['id'],
                    'parameters': self.generate_context_parameters(
                        procedure),
                    'return_type': self.parse_return_type(
                        self.get_return_type(procedure)),
                    'documentation': self.parse_documentation(
                        procedure['documentation'])
                }

            elif Attributes.is_a_class_property_getter(name):
                class_name = Attributes.get_class_name(name)
                cls = context['classes'][class_name]
                property_name = self.parse_name(
                    Attributes.get_class_member_name(name))
                if property_name not in cls['properties']:
                    cls['properties'][property_name] = {
                        'type': self.parse_return_type(
                            self.get_return_type(procedure)),
                        'getter': None,
                        'setter': None,
                        'documentation': self.parse_documentation(
                            procedure['documentation'])
                    }
                cls['properties'][property_name]['getter'] = {
                    'procedure': procedure,
                    'remote_name': name,
                    'remote_id': procedure['id']
                }

            elif Attributes.is_a_class_property_setter(name):
                class_name = Attributes.get_class_name(name)
                cls = context['classes'][class_name]
                property_name = self.parse_name(
                    Attributes.get_class_member_name(name))
                if property_name not in cls['properties']:
                    params = self.generate_context_parameters(procedure)
                    cls['properties'][property_name] = {
                        'type': params[1]['type'],
                        'getter': None,
                        'setter': None,
                        'documentation': self.parse_documentation(
                            procedure['documentation'])
                    }
                cls['properties'][property_name]['setter'] = {
                    'procedure': procedure,
                    'remote_name': name,
                    'remote_id': procedure['id']
                }

        # Sort the context
        def sort_dict(x):
            return collections.OrderedDict(
                sorted(x.items(), key=lambda x: x[0]))

        context['procedures'] = sort_dict(context['procedures'])
        context['properties'] = sort_dict(context['properties'])
        context['enumerations'] = sort_dict(context['enumerations'])
        context['classes'] = sort_dict(context['classes'])
        context['exceptions'] = sort_dict(context['exceptions'])
        for cls in context['classes'].values():
            cls['methods'] = sort_dict(cls['methods'])
            cls['static_methods'] = sort_dict(cls['static_methods'])
            cls['properties'] = sort_dict(cls['properties'])

        return context

    def get_return_type(self, procedure):
        if 'return_type' not in procedure:
            return None
        return as_type(self.types, procedure['return_type'])

    def parse_name(self, name):
        return self.language.parse_name(name)

    def parse_type(self, typ):
        return self.language.parse_type(typ)

    def parse_return_type(self, typ):
        return self.parse_type(typ)

    def parse_parameter_type(self, typ):
        return self.parse_type(typ)

    def parse_default_value(self, value, typ):
        return self.language.parse_default_value(value, typ)
