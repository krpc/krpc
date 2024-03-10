from __future__ import annotations
from typing import cast, Callable, DefaultDict, Iterable, List, Optional, Tuple, TYPE_CHECKING
import keyword
from collections import defaultdict
from xml.etree import ElementTree
from krpc.types import Types, TypeBase, DynamicType, DynamicClassBase, DefaultArgument
from krpc.decoder import Decoder
from krpc.utils import snake_case
from krpc.attributes import Attributes
import krpc.schema.KRPC_pb2 as KRPC
if TYPE_CHECKING:
    from krpc.client import Client


def _signature(param_types: Iterable[TypeBase], return_type: TypeBase) -> str:
    """ Generate a signature for a procedure that
        can be used as its docstring """
    if not param_types and return_type is None:
        return ''
    types = [x.python_type.__name__ for x in param_types]
    sig = ','.join(types)
    if not types:
        sig = '()'
    elif len(types) > 1:
        sig = '(' + sig + ')'
    if return_type is not None:
        sig += ' -> ' + return_type.python_type.__name__
    return sig


def _as_literal(value: object, typ: TypeBase) -> str:
    if typ.python_type == str:
        return '\'' + cast(str, value) + '\''
    return str(value)


def _member_name(name: str) -> str:
    return _update_names(snake_case(name))[0]


def _update_names(*names: str) -> List[str]:
    """ Given a list of names, append underscores to reserved keywords
        without causing names to clash """
    newnames = []
    for name in names:
        if keyword.iskeyword(name):
            name += '_'
            while name in names:
                name += '_'
        newnames.append(name)
    return newnames


def _construct_func(invoke: Callable,  # type: ignore[type-arg]
                    service_name: str,
                    procedure_name: str,
                    prefix_param_names: Iterable[str],
                    param_names: Iterable[str],
                    param_types: Iterable[TypeBase],
                    param_required: Iterable[bool],
                    param_default: Iterable[Optional[object]],
                    return_type: Optional[TypeBase]) -> Callable:  # type: ignore[type-arg]
    """ Build function to invoke a remote procedure """

    prefix_param_names = _update_names(*prefix_param_names)
    param_names = _update_names(*param_names)

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
    fn = eval(code, context)  # pylint: disable=eval-used
    return cast(Callable, fn)  # type: ignore[type-arg]


def _indent(lines: Iterable[str], level: int) -> List[str]:
    result = []
    for line in lines:
        if line:
            result.append((' ' * level) + line)
        else:
            result.append(line)
    return result


def _parse_documentation_node(node: ElementTree.Element) -> str:
    if node.tag == 'see':
        ref = node.attrib['cref']
        if ref[0] == 'M':
            refs = ref.split('.')
            refs[-1] = snake_case(refs[-1])
            ref = '.'.join(refs)
        return ref[2:]
    if node.tag == 'paramref':
        return snake_case(node.attrib['name'])
    if node.tag == 'c':
        replace = {'true': 'True', 'false': 'False', 'null': 'None'}
        if node.text in replace:
            return replace[node.text]
        return node.text or ''
    if node.tag == 'list':
        content = '\n'
        for item in node:
            item_content = _parse_documentation_content(item[0])
            content += '* %s\n' % '\n'.join(
                _indent(item_content.split('\n'), 2))[2:].rstrip()
        return content
    return node.text or ''


def _parse_documentation_content(node: ElementTree.Element) -> str:
    desc = node.text or ''
    for child in node:
        desc += _parse_documentation_node(child)
        if child.tail:
            desc += child.tail
    return desc.strip()


def _parse_documentation(xml: str) -> str:
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
    if params:
        params_str = 'Args:\n%s' % '\n'.join('    ' + x for x in params)
    else:
        params_str = ''
    return '\n\n'.join(x for x in (summary, params_str, returns, note)
                       if x != '')


def create_service(client: Client, service: KRPC.Service) -> object:
    """ Create a new service type """
    cls = cast(ServiceBase, type(
        str(service.name),
        (ServiceBase,),
        {
            '_client': client,
            '_name': service.name,
            '__doc__': _parse_documentation(service.documentation)
        }
    ))

    # Add class types to service
    for cls2 in service.classes:
        cls._add_service_class(cls2)

    # Add enumeration types to service
    for enumeration in service.enumerations:
        cls._add_service_enumeration(enumeration)

    # Add exception types to service
    for exception in service.exceptions:
        cls._add_service_exception(exception)

    # Add procedures
    for procedure in service.procedures:
        if Attributes.is_a_procedure(procedure.name):
            cls._add_service_procedure(procedure)

    # Add properties
    properties: DefaultDict[str, List[Optional[KRPC.Procedure]]] = \
        defaultdict(lambda: [None, None])
    for procedure in service.procedures:
        if Attributes.is_a_property_accessor(procedure.name):
            name = Attributes.get_property_name(procedure.name)
            if Attributes.is_a_property_getter(procedure.name):
                properties[name][0] = procedure
            else:
                properties[name][1] = procedure
    for name, procedures in properties.items():
        cls._add_service_property(name, procedures[0], procedures[1])

    # Add class methods
    for procedure in service.procedures:
        if Attributes.is_a_class_method(procedure.name):
            class_name = Attributes.get_class_name(procedure.name)
            method_name = Attributes.get_class_member_name(procedure.name)
            cls._add_service_class_method(class_name, method_name, procedure)

    # Add static class methods
    for procedure in service.procedures:
        if Attributes.is_a_class_static_method(procedure.name):
            class_name = Attributes.get_class_name(procedure.name)
            method_name = Attributes.get_class_member_name(procedure.name)
            cls._add_service_class_static_method(
                class_name, method_name, procedure)

    # Add class properties
    class_properties: DefaultDict[Tuple[str, str], List[Optional[KRPC.Procedure]]] = \
        defaultdict(lambda: [None, None])
    for procedure in service.procedures:
        if Attributes.is_a_class_property_accessor(procedure.name):
            class_name = Attributes.get_class_name(procedure.name)
            property_name = Attributes.get_class_member_name(procedure.name)
            key = (class_name, property_name)
            if Attributes.is_a_class_property_getter(procedure.name):
                class_properties[key][0] = procedure
            else:
                class_properties[key][1] = procedure
    for (class_name, property_name), procedures in class_properties.items():
        cls._add_service_class_property(
            class_name, property_name, procedures[0], procedures[1])

    return cls()  # type: ignore[operator]


class ServiceBase(DynamicType):
    """ Base class for service objects, created at runtime
        using information received from the server. """

    _client: Client
    _name: str

    @classmethod
    def _add_service_class(cls, remote_cls: KRPC.Class) -> None:
        """ Add a class type """
        name = remote_cls.name
        class_type = cls._client._types.class_type(
            cls._name, name, _parse_documentation(remote_cls.documentation))
        setattr(cls, name, class_type.python_type)

    @classmethod
    def _add_service_enumeration(cls, enumeration: KRPC.Enumeration) -> None:
        """ Add an enum type """
        name = enumeration.name
        enumeration_type = cls._client._types.enumeration_type(
            cls._name, name, _parse_documentation(enumeration.documentation))
        enumeration_type.set_values(dict(
            (str(snake_case(x.name)), {
                'value': x.value, 'doc': _parse_documentation(x.documentation)
            }) for x in enumeration.values))
        setattr(cls, name, enumeration_type.python_type)

    @classmethod
    def _add_service_exception(cls, exception: KRPC.Exception) -> None:
        """ Add an exception type """
        name = exception.name
        exception_type = cls._client._types.exception_type(
            cls._name, name, _parse_documentation(exception.documentation))
        setattr(cls, name, exception_type)

    @classmethod
    def _parse_procedure(
            cls, procedure: KRPC.Procedure
    ) -> Tuple[List[str], List[TypeBase], List[bool], List[Optional[object]], Optional[TypeBase]]:
        param_names = [snake_case(param.name)
                       for param in procedure.parameters]
        param_types = [cls._client._types.as_type(param.type)
                       for param in procedure.parameters]
        param_required = [not param.default_value
                          for param in procedure.parameters]
        param_default: List[Optional[object]] = []
        for param, typ in zip(procedure.parameters, param_types):
            if param.default_value:
                param_default.append(
                    Decoder.decode(cls._client, param.default_value, typ)
                )
            else:
                param_default.append(None)
        return_type: Optional[TypeBase] = None
        if not Types.is_none_type(procedure.return_type):
            return_type = cls._client._types.as_type(procedure.return_type)
        return param_names, param_types, param_required, \
            param_default, return_type

    @classmethod
    def _add_service_procedure(cls, procedure: KRPC.Procedure) -> None:
        """ Add a procedure """
        param_names, param_types, param_required, \
            param_default, return_type = cls._parse_procedure(procedure)
        func = _construct_func(
            cls._client._invoke, cls._name, procedure.name, ['cls'],
            param_names, param_types,
            param_required, param_default, return_type)
        build_call = _construct_func(
            cls._client._build_call, cls._name, procedure.name, ['cls'],
            param_names, param_types,
            param_required, param_default, return_type)
        name = _member_name(procedure.name)
        cls._add_class_method(
            name, func, doc=_parse_documentation(procedure.documentation))
        cls._add_class_method('_build_call_' + name, build_call)
        cls._add_class_method('_return_type_' + name, lambda cls: return_type)

    @classmethod
    def _add_service_property(cls, name: str,
                              getter: Optional[KRPC.Procedure] = None,
                              setter: Optional[KRPC.Procedure] = None) -> None:
        """ Add a property """
        doc = None
        if getter:
            doc = _parse_documentation(getter.documentation)
        elif setter:
            doc = _parse_documentation(setter.documentation)
        getter_fn = None
        setter_fn = None
        if getter:
            getter_name = getter.name
            _, _, _, _, return_type = cls._parse_procedure(getter)
            getter_fn = _construct_func(
                cls._client._invoke, cls._name, getter_name, ['self'],
                [], [], [], [], return_type)
            build_call = _construct_func(
                cls._client._build_call, cls._name, getter_name, ['self'],
                [], [], [], [], return_type)
            getter_return_type = return_type
        if setter:
            param_names, param_types, _, _, _ = cls._parse_procedure(setter)
            setter_fn = _construct_func(cls._client._invoke, cls._name,
                                        setter.name, ['self'],
                                        param_names, param_types,
                                        [True], [None], None)
        name = _member_name(name)
        cls._add_property(name, getter_fn, setter_fn, doc=doc)
        if getter:
            cls._add_method('_build_call_' + name, build_call)
            cls._add_method('_return_type_' + name, lambda self: getter_return_type)

    @classmethod
    def _add_service_class_method(cls, class_name: str, method_name: str,
                                  procedure: KRPC.Procedure) -> None:
        """ Add a method to a class """
        class_cls = cast(DynamicClassBase, cls._client._types.class_type(
            cls._name, class_name).python_type)
        param_names, param_types, param_required, \
            param_default, return_type = cls._parse_procedure(procedure)
        # Rename this to self if it doesn't cause a name clash
        if 'self' not in param_names:
            param_names[0] = 'self'
        func = _construct_func(
            cls._client._invoke, cls._name, procedure.name, [],
            param_names, param_types,
            param_required, param_default, return_type)
        build_call = _construct_func(
            cls._client._build_call, cls._name, procedure.name, [],
            param_names, param_types,
            param_required, param_default, return_type)
        name = _member_name(method_name)
        class_cls._add_method(
            name, func, doc=_parse_documentation(procedure.documentation))
        class_cls._add_method('_build_call_' + name, build_call)
        class_cls._add_method('_return_type_' + name, lambda self: return_type)

    @classmethod
    def _add_service_class_static_method(cls, class_name: str, method_name: str,
                                         procedure: KRPC.Procedure) -> None:
        """ Add a static method to a class """
        class_cls = cast(DynamicClassBase, cls._client._types.class_type(
            cls._name, class_name).python_type)
        param_names, param_types, param_required, \
            param_default, return_type = cls._parse_procedure(procedure)
        func = _construct_func(
            cls._client._invoke, cls._name, procedure.name, ['cls'],
            param_names, param_types,
            param_required, param_default, return_type)
        build_call = _construct_func(
            cls._client._build_call, cls._name, procedure.name, ['cls'],
            param_names, param_types,
            param_required, param_default, return_type)
        name = _member_name(method_name)
        class_cls._add_class_method(
            name, func, doc=_parse_documentation(procedure.documentation))
        class_cls._add_class_method('_build_call_' + name, build_call)
        class_cls._add_class_method('_return_type_' + name, lambda cls: return_type)

    @classmethod
    def _add_service_class_property(cls, class_name: str, property_name: str,
                                    getter: Optional[KRPC.Procedure] = None,
                                    setter: Optional[KRPC.Procedure] = None) -> None:
        """ Add a property to a class """
        class_cls = cast(DynamicClassBase, cls._client._types.class_type(
            cls._name, class_name).python_type)
        doc = None
        if getter:
            doc = _parse_documentation(getter.documentation)
        elif setter:
            doc = _parse_documentation(setter.documentation)
        getter_fn: Optional[Callable] = None  # type: ignore[type-arg]
        setter_fn: Optional[Callable] = None  # type: ignore[type-arg]
        if getter:
            getter_name = getter.name
            param_names, param_types, _, _, \
                return_type = cls._parse_procedure(getter)
            # Rename this to self if it doesn't cause a name clash
            if 'self' not in param_names:
                param_names[0] = 'self'
            getter_fn = _construct_func(
                cls._client._invoke, cls._name, getter_name, [],
                param_names, param_types, [True], [None], return_type)
            build_call = _construct_func(
                cls._client._build_call, cls._name, getter_name, [],
                param_names, param_types, [True], [None], return_type)
            getter_return_type = return_type
        if setter:
            param_names, param_types, _, _, \
                return_type = cls._parse_procedure(setter)
            setter_fn = _construct_func(
                cls._client._invoke, cls._name, setter.name, [],
                param_names, param_types, [True, True], [None, None], None)
        property_name = _member_name(property_name)
        class_cls._add_property(property_name, getter_fn, setter_fn, doc=doc)
        if getter:
            class_cls._add_method('_build_call_' + property_name, build_call)
            class_cls._add_method('_return_type_' + property_name, lambda self: getter_return_type)
