import keyword
from collections import defaultdict
import xml.etree.ElementTree as ElementTree
from krpc.attributes import Attributes
from krpc.types import DynamicType, DefaultArgument
from krpc.decoder import Decoder
from krpc.utils import snake_case


def _signature(param_types, return_type):
    """ Generate a signature for a procedure that
        can be used as its docstring """
    if len(param_types) == 0 and return_type is None:
        return ''
    types = [x.python_type.__name__ for x in param_types]
    sig = ','.join(types)
    if len(types) == 0:
        sig = '()'
    elif len(types) > 1:
        sig = '(' + sig + ')'
    if return_type is not None:
        sig += ' -> ' + return_type.python_type.__name__
    return sig


def _as_literal(value, typ):
    if typ.python_type == str:
        return '\'' + value + '\''
    return str(value)


def _update_param_names(names):
    """ Given a list of parameter names, append underscores to
        reserved keywords without causing parameter names to clash """
    newnames = []
    for name in names:
        if keyword.iskeyword(name):
            name += '_'
            while name in names:
                name += '_'
        newnames.append(name)
    return newnames


def _construct_func(invoke, service_name, procedure_name,
                    prefix_param_names, param_names,
                    param_types, param_required, param_default, return_type):
    """ Build function to invoke a remote procedure """

    prefix_param_names = _update_param_names(prefix_param_names)
    param_names = _update_param_names(param_names)

    params = []
    for name, required, default, typ in zip(
            param_names, param_required, param_default, param_types):
        if not required:
            name += ' = DefaultArgument(' + \
                    repr(_as_literal(default, typ)) + ')'
        params.append(name)

    invoke_args = [
        '\'' + str(service_name) + '\'',
        '\'' + str(procedure_name) + '\'',
        '[' + ','.join(param_names) + ']',
        'param_names',
        'param_types',
        'return_type'
    ]
    code = 'lambda ' + ', '.join(prefix_param_names + params) + \
           ': invoke(' + ', '.join(invoke_args) + ')'
    context = {
        'invoke': invoke,
        'DefaultArgument': DefaultArgument,
        'param_names': param_names,
        'param_types': param_types,
        'return_type': return_type
    }
    return eval(code, context)  # pylint: disable=eval-used


def _indent(lines, level):
    result = []
    for line in lines:
        if line:
            result.append((' ' * level) + line)
        else:
            result.append(line)
    return result


def _parse_documentation_node(node):
    if node.tag == 'see':
        ref = node.attrib['cref']
        if ref[0] == 'M':
            ref = ref.split('.')
            ref[-1] = snake_case(ref[-1])
            ref = '.'.join(ref)
        return ref[2:]
    elif node.tag == 'paramref':
        return snake_case(node.attrib['name'])
    elif node.tag == 'c':
        replace = {'true': 'True', 'false': 'False', 'null': 'None'}
        if node.text in replace:
            return replace[node.text]
        else:
            return node.text
    elif node.tag == 'list':
        content = '\n'
        for item in node:
            item_content = _parse_documentation_content(item[0])
            content += '* %s\n' % '\n'.join(
                _indent(item_content.split('\n'), 2))[2:].rstrip()
        return content
    else:
        return node.text


def _parse_documentation_content(node):
    desc = node.text or ''
    for child in node:
        desc += _parse_documentation_node(child)
        if child.tail:
            desc += child.tail
    return desc.strip()


def _parse_documentation(xml):
    if xml.strip() == '':
        return ''
    parser = ElementTree.XMLParser(encoding='UTF-8')
    root = ElementTree.XML(xml.encode('UTF-8'), parser=parser)
    summary = ''
    params = []
    returns = ''
    note = ''
    for node in root:
        if node.tag == 'summary':
            summary = _parse_documentation_content(node)
        elif node.tag == 'param':
            doc = _parse_documentation_content(node).replace('\n', '')
            params.append('%s: %s' % (snake_case(node.attrib['name']), doc))
        elif node.tag == 'returns':
            returns = 'Returns:\n    %s' % \
                      _parse_documentation_content(node).replace('\n', '')
        elif node.tag == 'remarks':
            note = 'Note: %s' % _parse_documentation_content(node)
    if len(params) > 0:
        params_str = 'Args:\n%s' % '\n'.join('    ' + x for x in params)
    else:
        params_str = ''
    return '\n\n'.join(x for x in (summary, params_str, returns, note)
                       if x != '')


def create_service(client, service):
    """ Create a new service type """
    cls = type(
        str(service.name),
        (ServiceBase,),
        {
            '_client': client,
            '_name': service.name,
            '__doc__': _parse_documentation(service.documentation)
        }
    )

    # Add class types to service
    for cls2 in service.classes:
        cls._add_service_class(cls2)

    # Add enumeration types to service
    for enum in service.enumerations:
        cls._add_service_enumeration(enum)

    # Add procedures
    for procedure in service.procedures:
        if Attributes.is_a_procedure(procedure.attributes):
            cls._add_service_procedure(procedure)

    # Add properties
    properties = defaultdict(lambda: [None, None])
    for procedure in service.procedures:
        if Attributes.is_a_property_accessor(procedure.attributes):
            name = Attributes.get_property_name(procedure.attributes)
            if Attributes.is_a_property_getter(procedure.attributes):
                properties[name][0] = procedure
            else:
                properties[name][1] = procedure
    for name, procedures in properties.items():
        cls._add_service_property(name, procedures[0], procedures[1])

    # Add class methods
    for procedure in service.procedures:
        if Attributes.is_a_class_method(procedure.attributes):
            class_name = Attributes.get_class_name(procedure.attributes)
            method_name = Attributes.get_class_method_name(
                procedure.attributes)
            cls._add_service_class_method(
                class_name, method_name, procedure)

    # Add static class methods
    for procedure in service.procedures:
        if Attributes.is_a_class_static_method(procedure.attributes):
            class_name = Attributes.get_class_name(procedure.attributes)
            method_name = Attributes.get_class_method_name(
                procedure.attributes)
            cls._add_service_class_static_method(
                class_name, method_name, procedure)

    # Add class properties
    properties = defaultdict(lambda: [None, None])
    for procedure in service.procedures:
        if Attributes.is_a_class_property_accessor(procedure.attributes):
            class_name = Attributes.get_class_name(procedure.attributes)
            property_name = Attributes.get_class_property_name(
                procedure.attributes)
            key = (class_name, property_name)
            if Attributes.is_a_class_property_getter(procedure.attributes):
                properties[key][0] = procedure
            else:
                properties[key][1] = procedure
    for (class_name, property_name), procedures in properties.items():
        cls._add_service_class_property(
            class_name, property_name, procedures[0], procedures[1])

    return cls()


class ServiceBase(DynamicType):
    """ Base class for service objects, created at runtime
        using information received from the server. """

    @classmethod
    def _add_service_class(cls, remote_cls):
        """ Add a class type """
        name = remote_cls.name
        class_type = cls._client._types.as_type(
            'Class(' + cls._name + '.' + name + ')',
            _parse_documentation(remote_cls.documentation))
        setattr(cls, name, class_type.python_type)

    @classmethod
    def _add_service_enumeration(cls, enum):
        """ Add an enumeration type """
        name = enum.name
        enum_type = cls._client._types.as_type(
            'Enum(' + cls._name + '.' + name + ')',
            _parse_documentation(enum.documentation))
        enum_type.set_values(dict(
            (str(snake_case(x.name)), {
                'value': x.value, 'doc': _parse_documentation(x.documentation)
            }) for x in enum.values))
        setattr(cls, name, enum_type.python_type)

    @classmethod
    def _parse_procedure(cls, procedure):
        param_names = [snake_case(param.name)
                       for param in procedure.parameters]
        param_types = [
            cls._client._types.get_parameter_type(
                i, param.type, procedure.attributes)
            for i, param in enumerate(procedure.parameters)]
        param_required = [not param.has_default_value
                          for param in procedure.parameters]
        param_default = []
        for param, typ in zip(procedure.parameters, param_types):
            if param.has_default_value:
                param_default.append(Decoder.decode(param.default_value, typ))
            else:
                param_default.append(None)
        return_type = None
        if procedure.has_return_type:
            return_type = cls._client._types.get_return_type(
                procedure.return_type, procedure.attributes)
        return param_names, param_types, param_required, \
            param_default, return_type

    @classmethod
    def _add_service_procedure(cls, procedure):
        """ Add a procedure """
        param_names, param_types, param_required, \
            param_default, return_type = cls._parse_procedure(procedure)
        func = _construct_func(
            cls._client._invoke, cls._name, procedure.name, [],
            param_names, param_types, param_required, param_default,
            return_type)
        build_request = _construct_func(
            cls._client._build_request, cls._name, procedure.name, [],
            param_names, param_types, param_required, param_default,
            return_type)
        setattr(func, '_build_request', build_request)
        setattr(func, '_return_type', return_type)
        name = str(snake_case(procedure.name))
        return cls._add_static_method(
            name, func, doc=_parse_documentation(procedure.documentation))

    @classmethod
    def _add_service_property(cls, name, getter=None, setter=None):
        """ Add a property """
        doc = None
        if getter:
            doc = _parse_documentation(getter.documentation)
        elif setter:
            doc = _parse_documentation(setter.documentation)
        if getter:
            getter_name = getter.name
            _, _, _, _, return_type = cls._parse_procedure(getter)
            getter = _construct_func(
                cls._client._invoke, cls._name, getter_name, ['self'],
                [], [], [], [], return_type)
            build_request = _construct_func(
                cls._client._build_request, cls._name, getter_name, ['self'],
                [], [], [], [], return_type)
            setattr(getter, '_build_request', build_request)
            setattr(getter, '_return_type', return_type)
        if setter:
            param_names, param_types, _, _, _ = cls._parse_procedure(setter)
            setter = _construct_func(cls._client._invoke, cls._name,
                                     setter.name, ['self'],
                                     param_names, param_types,
                                     [True], [None], None)
        name = str(snake_case(name))
        return cls._add_property(name, getter, setter, doc=doc)

    @classmethod
    def _add_service_class_method(cls, class_name, method_name, procedure):
        """ Add a method to a class """
        class_cls = cls._client._types.as_type(
            'Class(' + cls._name + '.' + class_name + ')').python_type
        param_names, param_types, param_required, \
            param_default, return_type = cls._parse_procedure(procedure)
        # Rename this to self if it doesn't cause a name clash
        if 'self' not in param_names:
            param_names[0] = 'self'
        func = _construct_func(
            cls._client._invoke, cls._name, procedure.name, [],
            param_names, param_types, param_required, param_default,
            return_type)
        build_request = _construct_func(
            cls._client._build_request, cls._name, procedure.name, [],
            param_names, param_types, param_required, param_default,
            return_type)
        setattr(func, '_build_request', build_request)
        setattr(func, '_return_type', return_type)
        name = str(snake_case(method_name))
        class_cls._add_method(
            name, func, doc=_parse_documentation(procedure.documentation))

    @classmethod
    def _add_service_class_static_method(cls, class_name,
                                         method_name, procedure):
        """ Add a static method to a class """
        class_cls = cls._client._types.as_type(
            'Class(' + cls._name + '.' + class_name + ')').python_type
        param_names, param_types, param_required, \
            param_default, return_type = cls._parse_procedure(procedure)
        func = _construct_func(
            cls._client._invoke, cls._name, procedure.name, [],
            param_names, param_types, param_required, param_default,
            return_type)
        build_request = _construct_func(
            cls._client._build_request, cls._name, procedure.name, [],
            param_names, param_types, param_required, param_default,
            return_type)
        setattr(func, '_build_request', build_request)
        setattr(func, '_return_type', return_type)
        name = str(snake_case(method_name))
        class_cls._add_static_method(
            name, func, doc=_parse_documentation(procedure.documentation))

    @classmethod
    def _add_service_class_property(cls, class_name, property_name,
                                    getter=None, setter=None):
        """ Add a property to a class """
        class_cls = cls._client._types.as_type(
            'Class(' + cls._name + '.' + class_name + ')').python_type
        doc = None
        if getter:
            doc = _parse_documentation(getter.documentation)
        elif setter:
            doc = _parse_documentation(setter.documentation)
        if getter:
            getter_name = getter.name
            param_names, param_types, _, _, \
                return_type = cls._parse_procedure(getter)
            # Rename this to self if it doesn't cause a name clash
            if 'self' not in param_names:
                param_names[0] = 'self'
            getter = _construct_func(
                cls._client._invoke, cls._name, getter_name, [],
                param_names, param_types, [True], [None], return_type)
            build_request = _construct_func(
                cls._client._build_request, cls._name, getter_name, [],
                param_names, param_types, [True], [None], return_type)
            setattr(getter, '_build_request', build_request)
            setattr(getter, '_return_type', return_type)
        if setter:
            param_names, param_types, _, _, \
                return_type = cls._parse_procedure(setter)
            setter = _construct_func(
                cls._client._invoke, cls._name, setter.name, [],
                param_names, param_types, [True, True], [None, None], None)
        property_name = str(snake_case(property_name))
        return class_cls._add_property(property_name, getter, setter, doc=doc)
