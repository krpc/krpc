from __future__ import annotations
from typing import (
    cast,
    Any,
    Callable,
    DefaultDict,
    Dict,
    Iterable,
    Iterator,
    List,
    Optional,
    Set,
    Tuple,
    TYPE_CHECKING,
)
import keyword
import warnings
from collections import defaultdict
from xml.etree import ElementTree
from krpc.definitions import (
    CLASS,
    ENUMERATION,
    EXCEPTION,
    STRUCT,
    Definition,
    decode_default_value,
)
from krpc.types import (
    Types,
    TypeBase,
    DynamicType,
    DynamicClassBase,
    DefaultArgument,
    UnknownTypeError,
    WrappedClass,
    check_type_is_known,
    is_a_known_type,
)
from krpc.utils import snake_case
from krpc.attributes import Attributes
import krpc.schema.KRPC_pb2 as KRPC

if TYPE_CHECKING:
    from krpc.client import Client


def _signature(param_types: Iterable[TypeBase], return_type: TypeBase) -> str:
    """Generate a signature for a procedure that
    can be used as its docstring"""
    if not param_types and return_type is None:
        return ""
    types = [x.python_type.__name__ for x in param_types]
    sig = ",".join(types)
    if not types:
        sig = "()"
    elif len(types) > 1:
        sig = "(" + sig + ")"
    if return_type is not None:
        sig += " -> " + return_type.python_type.__name__
    return sig


def _as_literal(value: object, typ: TypeBase) -> str:
    if value is None:
        return "None"
    if typ.python_type == str:
        return "'" + cast(str, value) + "'"
    return str(value)


def _member_name(name: str) -> str:
    return _update_names(snake_case(name))[0]


def _update_names(*names: str) -> List[str]:
    """Given a list of names, append underscores to reserved keywords
    without causing names to clash"""
    newnames = []
    for name in names:
        if keyword.iskeyword(name):
            name += "_"
            while name in names:
                name += "_"
        newnames.append(name)
    return newnames


def _construct_func(
    invoke: Callable,  # type: ignore[type-arg]
    service_name: str,
    procedure_name: str,
    prefix_param_names: Iterable[str],
    param_names: Iterable[str],
    param_types: Iterable[TypeBase],
    param_required: Iterable[bool],
    param_default: Iterable[Optional[object]],
    return_type: Optional[TypeBase],
) -> Callable:  # type: ignore[type-arg]
    """Build function to invoke a remote procedure"""

    prefix_param_names = _update_names(*prefix_param_names)
    param_names = _update_names(*param_names)

    params = []
    for name, required, default, typ in zip(
        param_names, param_required, param_default, param_types
    ):
        if not required:
            name += " = DefaultArgument(" + repr(_as_literal(default, typ)) + ")"
        params.append(name)

    invoke_args = [
        "'" + str(service_name) + "'",
        "'" + str(procedure_name) + "'",
        "[" + ",".join(param_names) + "]",
        "param_types",
        "return_type",
    ]
    code = (
        "lambda "
        + ", ".join(prefix_param_names + params)
        + ": invoke("
        + ", ".join(invoke_args)
        + ")"
    )
    context = {
        "invoke": invoke,
        "DefaultArgument": DefaultArgument,
        "param_types": param_types,
        "return_type": return_type,
    }
    fn = eval(code, context)  # pylint: disable=eval-used
    return cast(Callable, fn)  # type: ignore[type-arg]


def _parse_deprecation_reason(reason: str, service_name: str) -> str:
    """Flatten a deprecation reason to plain text. The reason may contain the
    same see-cref markup as documentation strings; references are converted to
    Python-cased dotted names, with the current service's prefix dropped to
    match the pre-generated stubs."""
    if "<" not in reason:
        return reason

    def parse_cref(ref: str) -> str:
        name = _parse_cref(ref)
        prefix = service_name + "."
        if name.startswith(prefix):
            name = name[len(prefix) :]
        return name

    parser = ElementTree.XMLParser(encoding="UTF-8")
    node = ElementTree.XML(("<doc>%s</doc>" % reason).encode("UTF-8"), parser=parser)
    return _parse_documentation_content(node, parse_cref)


def _deprecated_doc(doc: str, reason: str, service_name: str) -> str:
    """Prepend a deprecation notice to a constructed docstring"""
    reason = _parse_deprecation_reason(reason, service_name)
    line = "Deprecated. " + reason if reason else "Deprecated."
    return line + "\n\n" + doc if doc else line


def _wrap_deprecated(
    func: Callable,  # type: ignore[type-arg]
    qualified_name: str,
    reason: str,
    service_name: str,
) -> Callable:  # type: ignore[type-arg]
    """Wrap an invoker so that calling it emits a DeprecationWarning"""
    message = qualified_name + " is deprecated"
    if reason:
        message += ": " + _parse_deprecation_reason(reason, service_name)

    def wrapper(*args: object, **kwargs: object) -> object:
        warnings.warn(message, DeprecationWarning, stacklevel=2)
        return func(*args, **kwargs)

    return wrapper


def _indent(lines: Iterable[str], level: int) -> List[str]:
    result = []
    for line in lines:
        if line:
            result.append((" " * level) + line)
        else:
            result.append(line)
    return result


def _parse_cref(ref: str) -> str:
    """Convert a service-level cref to a Python-cased dotted name"""
    if ref[0] == "M":
        refs = ref.split(".")
        refs[-1] = snake_case(refs[-1])
        ref = ".".join(refs)
    return ref[2:]


def _parse_documentation_node(
    node: ElementTree.Element,
    parse_cref: Callable[[str], str] = _parse_cref,
) -> str:
    if node.tag == "see":
        return parse_cref(node.attrib["cref"])
    if node.tag == "paramref":
        return snake_case(node.attrib["name"])
    if node.tag == "c":
        replace = {"true": "True", "false": "False", "null": "None"}
        if node.text in replace:
            return replace[node.text]
        return node.text or ""
    if node.tag == "list":
        content = "\n"
        for item in node:
            item_content = _parse_documentation_content(item[0], parse_cref)
            content += (
                "* %s\n" % "\n".join(_indent(item_content.split("\n"), 2))[2:].rstrip()
            )
        return content
    return node.text or ""


def _parse_documentation_content(
    node: ElementTree.Element,
    parse_cref: Callable[[str], str] = _parse_cref,
) -> str:
    desc = node.text or ""
    for child in node:
        desc += _parse_documentation_node(child, parse_cref)
        if child.tail:
            desc += child.tail
    return desc.strip()


def _parse_documentation(xml: str) -> str:
    if xml.strip() == "":
        return ""
    parser = ElementTree.XMLParser(encoding="UTF-8")
    root = ElementTree.XML(xml.encode("UTF-8"), parser=parser)
    summary = ""
    params = []
    returns = ""
    note = ""
    for node in root:
        if node.tag == "summary":
            summary = _parse_documentation_content(node)
        elif node.tag == "param":
            doc = _parse_documentation_content(node).replace("\n", "")
            params.append("%s: %s" % (snake_case(node.attrib["name"]), doc))
        elif node.tag == "returns":
            returns = "Returns:\n    %s" % _parse_documentation_content(node).replace(
                "\n", ""
            )
        elif node.tag == "remarks":
            note = "Note: %s" % _parse_documentation_content(node)
    if params:
        params_str = "Args:\n%s" % "\n".join("    " + x for x in params)
    else:
        params_str = ""
    return "\n\n".join(x for x in (summary, params_str, returns, note) if x != "")


def _documentation(member: Any, service_name: str) -> str:
    """The documentation of a service member, including a notice when it is deprecated"""
    doc = _parse_documentation(member.documentation)
    if member.deprecated:
        doc = _deprecated_doc(doc, member.deprecated_reason, service_name)
    return doc


def service_definitions(service: KRPC.Service) -> Iterator[Definition]:
    """The types a service defines, as records that can be registered in a type registry.

    A service's procedures are built from types that this or any other service defines, so
    every service's definitions are registered before any service is created."""
    name = service.name

    def register_class(cls: KRPC.Class) -> Callable[[Types], None]:
        doc = _documentation(cls, name)

        def register(types: Types) -> None:
            types.class_type(name, cls.name, doc)

        return register

    def register_enumeration(enumeration: KRPC.Enumeration) -> Callable[[Types], None]:
        doc = _documentation(enumeration, name)
        values = dict(
            (
                _member_name(value.name),
                {"value": value.value, "doc": _documentation(value, name)},
            )
            for value in enumeration.values
        )

        def register(types: Types) -> None:
            types.enumeration_type(name, enumeration.name, doc).set_values(values)

        return register

    def register_exception(exception: KRPC.Exception) -> Callable[[Types], None]:
        doc = _documentation(exception, name)

        def register(types: Types) -> None:
            types.exception_type(name, exception.name, doc)

        return register

    def register_struct(struct: KRPC.Struct) -> Callable[[Types], None]:
        doc = _documentation(struct, name)
        field_names = _update_names(
            *[snake_case(field.name) for field in struct.fields]
        )

        def register(types: Types) -> None:
            # A structure whose fields cannot all be resolved is left without any, so that
            # whatever names it is skipped and the service still builds
            if not all(is_a_known_type(field.type) for field in struct.fields):
                warnings.warn(
                    "Skipping the struct %s.%s, as the type of one of its fields is not a "
                    "type this client knows about" % (name, struct.name)
                )
                return
            fields = [
                (field_name, types.as_type(field.type))
                for field_name, field in zip(field_names, struct.fields)
            ]
            types.struct_type(name, struct.name, doc).set_fields(fields)

        return register

    for cls in service.classes:
        yield Definition(CLASS, name, cls.name, [], register_class(cls))
    for enumeration in service.enumerations:
        yield Definition(
            ENUMERATION, name, enumeration.name, [], register_enumeration(enumeration)
        )
    for exception in service.exceptions:
        yield Definition(
            EXCEPTION, name, exception.name, [], register_exception(exception)
        )
    # A structure is registered after the definitions its field types name, as building its
    # fields resolves those types
    for struct in service.structs:
        yield Definition(
            STRUCT,
            name,
            struct.name,
            [field.type for field in struct.fields],
            register_struct(struct),
        )


def _skipping_unknown_types(
    what: str, build: Callable[..., None], *args: object  # type: ignore[type-arg]
) -> None:
    """Run one step of building a service, skipping it with a warning if it names a type this
    client does not know about, rather than failing to create the service at all. That is
    what a definition from a newer server looks like, where a member has been added whose
    type is from a later version of the protocol."""
    try:
        build(*args)
    except UnknownTypeError as exn:
        warnings.warn("Skipping %s: %s" % (what, exn))


def create_service(client: Client, service: KRPC.Service) -> object:
    """Create a new service type"""
    doc = _parse_documentation(service.documentation)
    if service.deprecated:
        doc = _deprecated_doc(doc, service.deprecated_reason, service.name)
    cls = cast(
        ServiceBase,
        type(
            str(service.name),
            (ServiceBase,),
            {
                "_client": client,
                "_name": service.name,
                "__doc__": doc,
            },
        ),
    )

    # Add class types to service
    for cls2 in service.classes:
        cls._add_service_class(cls2)

    # Add enumeration types to service
    for enumeration in service.enumerations:
        cls._add_service_enumeration(enumeration)

    # Add exception types to service
    for exception in service.exceptions:
        cls._add_service_exception(exception)

    # Add structure types to service
    for struct in service.structs:
        if client._types.struct_type(service.name, struct.name).has_fields:
            cls._add_service_struct(struct)

    # Add procedures
    for procedure in service.procedures:
        if Attributes.is_a_procedure(procedure.name):
            _skipping_unknown_types(
                "%s.%s" % (service.name, procedure.name),
                cls._add_service_procedure,
                procedure,
            )

    # Add properties
    properties: DefaultDict[str, List[Optional[KRPC.Procedure]]] = defaultdict(
        lambda: [None, None]
    )
    for procedure in service.procedures:
        if Attributes.is_a_property_accessor(procedure.name):
            name = Attributes.get_property_name(procedure.name)
            if Attributes.is_a_property_getter(procedure.name):
                properties[name][0] = procedure
            else:
                properties[name][1] = procedure
    for name, procedures in properties.items():
        _skipping_unknown_types(
            "%s.%s" % (service.name, name),
            cls._add_service_property,
            name,
            procedures[0],
            procedures[1],
        )

    _add_class_members(cls, service)

    return cls()  # type: ignore[operator]


def _add_class_members(
    cls: ServiceBase,
    service: KRPC.Service,
    skip: Optional[Callable[[str, str], bool]] = None,
) -> None:
    """Attach the members that a service declares for its classes to the class types.

    skip takes a class and member name, and returns whether the member is already
    present."""

    def attach(
        class_name: str,
        member_name: str,
        add: Callable[..., None],
        *args: object,
    ) -> None:
        if skip is not None and skip(class_name, member_name):
            return
        _skipping_unknown_types(
            "%s.%s.%s" % (service.name, class_name, member_name),
            add,
            class_name,
            member_name,
            *args,
        )

    # Add class methods
    for procedure in service.procedures:
        if Attributes.is_a_class_method(procedure.name):
            attach(
                Attributes.get_class_name(procedure.name),
                Attributes.get_class_member_name(procedure.name),
                cls._add_service_class_method,
                procedure,
            )

    # Add static class methods
    for procedure in service.procedures:
        if Attributes.is_a_class_static_method(procedure.name):
            attach(
                Attributes.get_class_name(procedure.name),
                Attributes.get_class_member_name(procedure.name),
                cls._add_service_class_static_method,
                procedure,
            )

    # Add class properties
    class_properties: DefaultDict[Tuple[str, str], List[Optional[KRPC.Procedure]]] = (
        defaultdict(lambda: [None, None])
    )
    for procedure in service.procedures:
        if Attributes.is_a_class_property_accessor(procedure.name):
            key = (
                Attributes.get_class_name(procedure.name),
                Attributes.get_class_member_name(procedure.name),
            )
            if Attributes.is_a_class_property_getter(procedure.name):
                class_properties[key][0] = procedure
            else:
                class_properties[key][1] = procedure
    for (class_name, property_name), procedures in class_properties.items():
        attach(
            class_name,
            property_name,
            cls._add_service_class_property,
            procedures[0],
            procedures[1],
        )


def _stub_is_missing(
    classes: Dict[str, type], class_name: str, member_name: str
) -> bool:
    """Whether the stubs lack a member that a service declares for one of its classes.

    A class the stubs omit lacks nothing, as there is nothing to add a member to."""
    python_type = classes.get(class_name)
    return python_type is not None and not hasattr(
        python_type, _member_name(member_name)
    )


def extended_stub_classes(service: KRPC.Service, stub: object) -> Set[str]:
    """The classes of a service whose members the stubs do not all have.

    The stubs are generated from one version of a service. The server may declare more
    members for its classes, either because another mod adds them or because the server
    is newer."""
    classes = stub._classes  # type: ignore[attr-defined]
    names: Set[str] = set()
    for procedure in service.procedures:
        if not Attributes.is_a_class_member(procedure.name):
            continue
        class_name = Attributes.get_class_name(procedure.name)
        member_name = Attributes.get_class_member_name(procedure.name)
        if _stub_is_missing(classes, class_name, member_name):
            names.add(class_name)
    return names


def merge_service(client: Client, service: KRPC.Service, stub: object) -> None:
    """Attach the class members that only a service's definition declares.

    The members go on this client's subclass of each pre-generated class, which the type
    registry already holds (see extended_stub_classes). Members the stubs already have are
    left alone, as are classes the stubs omit."""
    classes = stub._classes  # type: ignore[attr-defined]
    cls = cast(
        ServiceBase,
        type(
            str(service.name),
            (ServiceBase,),
            {"_client": client, "_name": service.name},
        ),
    )

    def already_present(class_name: str, member_name: str) -> bool:
        return not _stub_is_missing(classes, class_name, member_name)

    _add_class_members(cls, service, already_present)

    # The class reached through the service is the pre-generated one, so point it at the
    # subclass the members went on
    for class_name in extended_stub_classes(service, stub):
        python_type = client._types.class_type(service.name, class_name).python_type
        stub.__dict__[class_name] = WrappedClass(client, python_type)


class ServiceBase(DynamicType):
    """Base class for service objects, created at runtime
    using information received from the server."""

    _client: Client
    _name: str

    # The types a service defines are registered from its definitions, before any service is
    # created, so the following take the types they were registered as and make them members of
    # the service

    @classmethod
    def _add_service_class(cls, remote_cls: KRPC.Class) -> None:
        """Add a class type"""
        class_type = cls._client._types.class_type(cls._name, remote_cls.name)
        setattr(cls, remote_cls.name, class_type.python_type)

    @classmethod
    def _add_service_enumeration(cls, enumeration: KRPC.Enumeration) -> None:
        """Add an enum type"""
        enumeration_type = cls._client._types.enumeration_type(
            cls._name, enumeration.name
        )
        setattr(cls, enumeration.name, enumeration_type.python_type)

    @classmethod
    def _add_service_struct(cls, struct: KRPC.Struct) -> None:
        """Add a structure type"""
        struct_type = cls._client._types.struct_type(cls._name, struct.name)
        setattr(cls, struct.name, struct_type.python_type)

    @classmethod
    def _add_service_exception(cls, exception: KRPC.Exception) -> None:
        """Add an exception type"""
        exception_type = cls._client._types.exception_type(cls._name, exception.name)
        setattr(cls, exception.name, exception_type)

    @classmethod
    def _parse_procedure(cls, procedure: KRPC.Procedure) -> Tuple[
        List[str],
        List[TypeBase],
        List[bool],
        List[Optional[object]],
        Optional[TypeBase],
    ]:
        param_names = [snake_case(param.name) for param in procedure.parameters]
        param_types = [
            cls._client._types.as_type(param.type) for param in procedure.parameters
        ]
        return_type: Optional[TypeBase] = None
        if not Types.is_none_type(procedure.return_type):
            return_type = cls._client._types.as_type(procedure.return_type)
        # Checked before anything is built from the types, so that a procedure naming a
        # structure whose definition was skipped is skipped along with it
        for typ in param_types:
            check_type_is_known(typ)
        if return_type is not None:
            check_type_is_known(return_type)
        param_required = [not param.has_default_value for param in procedure.parameters]
        param_default: List[Optional[object]] = []
        for param, typ in zip(procedure.parameters, param_types):
            if param.has_default_value and not param.default_value_is_null:
                location = "%s.%s parameter %s" % (
                    cls._name,
                    procedure.name,
                    snake_case(param.name),
                )
                param_default.append(
                    decode_default_value(
                        cls._client, param.default_value, typ, location
                    )
                )
            else:
                param_default.append(None)
        return param_names, param_types, param_required, param_default, return_type

    @classmethod
    def _add_service_procedure(cls, procedure: KRPC.Procedure) -> None:
        """Add a procedure"""
        param_names, param_types, param_required, param_default, return_type = (
            cls._parse_procedure(procedure)
        )
        func = _construct_func(
            cls._client._invoke,
            cls._name,
            procedure.name,
            ["cls"],
            param_names,
            param_types,
            param_required,
            param_default,
            return_type,
        )
        build_call = _construct_func(
            cls._client._build_call,
            cls._name,
            procedure.name,
            ["cls"],
            param_names,
            param_types,
            param_required,
            param_default,
            return_type,
        )
        name = _member_name(procedure.name)
        doc = _parse_documentation(procedure.documentation)
        if procedure.deprecated:
            func = _wrap_deprecated(
                func, cls._name + "." + name, procedure.deprecated_reason, cls._name
            )
            doc = _deprecated_doc(doc, procedure.deprecated_reason, cls._name)
        cls._add_class_method(name, func, doc=doc)
        cls._add_class_method("_build_call_" + name, build_call)
        cls._add_class_method("_return_type_" + name, lambda cls: return_type)

    @classmethod
    def _add_service_property(
        cls,
        name: str,
        getter: Optional[KRPC.Procedure] = None,
        setter: Optional[KRPC.Procedure] = None,
    ) -> None:
        """Add a property"""
        member_name = _member_name(name)
        qualified_name = cls._name + "." + member_name
        doc = None
        if getter:
            doc = _parse_documentation(getter.documentation)
        elif setter:
            doc = _parse_documentation(setter.documentation)
        if getter and getter.deprecated:
            doc = _deprecated_doc(doc or "", getter.deprecated_reason, cls._name)
        elif setter and setter.deprecated:
            doc = _deprecated_doc(doc or "", setter.deprecated_reason, cls._name)
        getter_fn = None
        setter_fn = None
        if getter:
            getter_name = getter.name
            _, _, _, _, return_type = cls._parse_procedure(getter)
            getter_fn = _construct_func(
                cls._client._invoke,
                cls._name,
                getter_name,
                ["self"],
                [],
                [],
                [],
                [],
                return_type,
            )
            if getter.deprecated:
                getter_fn = _wrap_deprecated(
                    getter_fn, qualified_name, getter.deprecated_reason, cls._name
                )
            build_call = _construct_func(
                cls._client._build_call,
                cls._name,
                getter_name,
                ["self"],
                [],
                [],
                [],
                [],
                return_type,
            )
            getter_return_type = return_type
        if setter:
            param_names, param_types, _, _, _ = cls._parse_procedure(setter)
            setter_fn = _construct_func(
                cls._client._invoke,
                cls._name,
                setter.name,
                ["self"],
                param_names,
                param_types,
                [True],
                [None],
                None,
            )
            if setter.deprecated:
                setter_fn = _wrap_deprecated(
                    setter_fn, qualified_name, setter.deprecated_reason, cls._name
                )
        name = member_name
        cls._add_property(name, getter_fn, setter_fn, doc=doc)
        if getter:
            cls._add_method("_build_call_" + name, build_call)
            cls._add_method("_return_type_" + name, lambda self: getter_return_type)

    @classmethod
    def _add_service_class_method(
        cls, class_name: str, method_name: str, procedure: KRPC.Procedure
    ) -> None:
        """Add a method to a class"""
        class_cls = cast(
            DynamicClassBase,
            cls._client._types.class_type(cls._name, class_name).python_type,
        )
        param_names, param_types, param_required, param_default, return_type = (
            cls._parse_procedure(procedure)
        )
        # Rename this to self if it doesn't cause a name clash
        if "self" not in param_names:
            param_names[0] = "self"
        func = _construct_func(
            cls._client._invoke,
            cls._name,
            procedure.name,
            [],
            param_names,
            param_types,
            param_required,
            param_default,
            return_type,
        )
        build_call = _construct_func(
            cls._client._build_call,
            cls._name,
            procedure.name,
            [],
            param_names,
            param_types,
            param_required,
            param_default,
            return_type,
        )
        name = _member_name(method_name)
        doc = _parse_documentation(procedure.documentation)
        if procedure.deprecated:
            func = _wrap_deprecated(
                func,
                cls._name + "." + class_name + "." + name,
                procedure.deprecated_reason,
                cls._name,
            )
            doc = _deprecated_doc(doc, procedure.deprecated_reason, cls._name)
        class_cls._add_method(name, func, doc=doc)
        class_cls._add_method("_build_call_" + name, build_call)
        class_cls._add_method("_return_type_" + name, lambda self: return_type)

    @classmethod
    def _add_service_class_static_method(
        cls, class_name: str, method_name: str, procedure: KRPC.Procedure
    ) -> None:
        """Add a static method to a class"""
        class_cls = cast(
            DynamicClassBase,
            cls._client._types.class_type(cls._name, class_name).python_type,
        )
        param_names, param_types, param_required, param_default, return_type = (
            cls._parse_procedure(procedure)
        )
        func = _construct_func(
            cls._client._invoke,
            cls._name,
            procedure.name,
            ["cls"],
            param_names,
            param_types,
            param_required,
            param_default,
            return_type,
        )
        build_call = _construct_func(
            cls._client._build_call,
            cls._name,
            procedure.name,
            ["cls"],
            param_names,
            param_types,
            param_required,
            param_default,
            return_type,
        )
        name = _member_name(method_name)
        doc = _parse_documentation(procedure.documentation)
        if procedure.deprecated:
            func = _wrap_deprecated(
                func,
                cls._name + "." + class_name + "." + name,
                procedure.deprecated_reason,
                cls._name,
            )
            doc = _deprecated_doc(doc, procedure.deprecated_reason, cls._name)
        class_cls._add_class_method(name, func, doc=doc)
        class_cls._add_class_method("_build_call_" + name, build_call)
        class_cls._add_class_method("_return_type_" + name, lambda cls: return_type)

    @classmethod
    def _add_service_class_property(
        cls,
        class_name: str,
        property_name: str,
        getter: Optional[KRPC.Procedure] = None,
        setter: Optional[KRPC.Procedure] = None,
    ) -> None:
        """Add a property to a class"""
        class_cls = cast(
            DynamicClassBase,
            cls._client._types.class_type(cls._name, class_name).python_type,
        )
        member_name = _member_name(property_name)
        qualified_name = cls._name + "." + class_name + "." + member_name
        doc = None
        if getter:
            doc = _parse_documentation(getter.documentation)
        elif setter:
            doc = _parse_documentation(setter.documentation)
        if getter and getter.deprecated:
            doc = _deprecated_doc(doc or "", getter.deprecated_reason, cls._name)
        elif setter and setter.deprecated:
            doc = _deprecated_doc(doc or "", setter.deprecated_reason, cls._name)
        getter_fn: Optional[Callable] = None  # type: ignore[type-arg]
        setter_fn: Optional[Callable] = None  # type: ignore[type-arg]
        if getter:
            getter_name = getter.name
            param_names, param_types, _, _, return_type = cls._parse_procedure(getter)
            # Rename this to self if it doesn't cause a name clash
            if "self" not in param_names:
                param_names[0] = "self"
            getter_fn = _construct_func(
                cls._client._invoke,
                cls._name,
                getter_name,
                [],
                param_names,
                param_types,
                [True],
                [None],
                return_type,
            )
            if getter.deprecated:
                getter_fn = _wrap_deprecated(
                    getter_fn, qualified_name, getter.deprecated_reason, cls._name
                )
            build_call = _construct_func(
                cls._client._build_call,
                cls._name,
                getter_name,
                [],
                param_names,
                param_types,
                [True],
                [None],
                return_type,
            )
            getter_return_type = return_type
        if setter:
            param_names, param_types, _, _, return_type = cls._parse_procedure(setter)
            setter_fn = _construct_func(
                cls._client._invoke,
                cls._name,
                setter.name,
                [],
                param_names,
                param_types,
                [True, True],
                [None, None],
                None,
            )
            if setter.deprecated:
                setter_fn = _wrap_deprecated(
                    setter_fn, qualified_name, setter.deprecated_reason, cls._name
                )
        property_name = member_name
        class_cls._add_property(property_name, getter_fn, setter_fn, doc=doc)
        if getter:
            class_cls._add_method("_build_call_" + property_name, build_call)
            class_cls._add_method(
                "_return_type_" + property_name, lambda self: getter_return_type
            )
