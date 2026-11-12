from __future__ import annotations

import functools
import keyword
import warnings
from collections import defaultdict
from collections.abc import Callable, Iterable, Iterator
from enum import Enum, Flag
from typing import (
    TYPE_CHECKING,
    Any,
    cast,
)
from xml.etree import ElementTree

import krpc.schema.KRPC_pb2 as KRPC
from krpc.attributes import Attributes
from krpc.declarations import Lookup, registered
from krpc.definitions import (
    CLASS,
    ENUMERATION,
    EXCEPTION,
    STRUCT,
    Definition,
    decode_default_value,
)
from krpc.docmath import to_text
from krpc.types import (
    DefaultArgument,
    DeprecatedDefinition,
    DynamicClassBase,
    DynamicType,
    TypeBase,
    Types,
    UnknownTypeError,
    check_type_is_known,
    deprecation_message,
    is_a_known_type,
    native_exception,
    warn_deprecated,
)
from krpc.utils import snake_case

if TYPE_CHECKING:
    from krpc.connection import Connection


def _as_literal(value: object, typ: TypeBase) -> str:
    if value is None:
        return "None"
    if typ.python_type is str:
        return "'" + cast(str, value) + "'"
    return str(value)


def _member_name(name: str) -> str:
    return _update_names(snake_case(name))[0]


def _update_names(*names: str) -> list[str]:
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
    invoke: Callable,
    service_name: str,
    procedure_name: str,
    prefix_param_names: Iterable[str],
    param_names: Iterable[str],
    param_types: Iterable[TypeBase],
    param_required: Iterable[bool],
    param_default: Iterable[object | None],
    return_type: TypeBase | None,
) -> Callable:
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
    fn = eval(code, context)
    return cast(Callable, fn)


def _parse_deprecation_reason(reason: str, service_name: str) -> str:
    """Flatten a deprecation reason to plain text. The reason may contain the
    same see-cref markup as documentation strings; references are converted to
    Python-cased dotted names, with the current service's prefix dropped to
    match the generated modules."""
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


def inherit_deprecation(service: KRPC.Service) -> None:
    """Deprecate each member of a deprecated service, class, enumeration or structure. A member
    with no reason of its own takes the reason of its container."""

    def inherit(member: Any, container: Any) -> None:
        if container.deprecated:
            member.deprecated = True
            member.deprecated_reason = (
                member.deprecated_reason or container.deprecated_reason
            )

    # A class's reason comes before its service's
    classes = {cls.name: cls for cls in service.classes}
    for procedure in service.procedures:
        if Attributes.is_a_class_member(procedure.name):
            inherit(procedure, classes[Attributes.get_class_name(procedure.name)])
    for members in (
        service.classes,
        service.enumerations,
        service.structs,
        service.exceptions,
        service.procedures,
    ):
        for member in members:
            inherit(member, service)
    for enumeration in service.enumerations:
        for value in enumeration.values:
            inherit(value, enumeration)
    for struct in service.structs:
        for field in struct.fields:
            inherit(field, struct)


def _deprecated_doc(doc: str, reason: str, service_name: str) -> str:
    """Prepend a deprecation notice to a constructed docstring"""
    reason = _parse_deprecation_reason(reason, service_name)
    line = "Deprecated. " + reason if reason else "Deprecated."
    return line + "\n\n" + doc if doc else line


def _wrap_deprecated(
    func: Callable,
    qualified_name: str,
    reason: str,
    service_name: str,
) -> Callable:
    """Wrap an invoker so that calling it emits a DeprecationWarning"""
    message = deprecation_message(
        qualified_name, _parse_deprecation_reason(reason, service_name)
    )

    @functools.wraps(func)
    def wrapper(*args: object, **kwargs: object) -> object:
        warn_deprecated(message)
        return func(*args, **kwargs)

    # Connection.get_call warns with it, as building a call skips the wrapper
    wrapper._deprecation = message  # pyright: ignore[reportAttributeAccessIssue]
    return wrapper


def _indent(lines: Iterable[str], level: int) -> list[str]:
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
    if node.tag in ("math", "unit"):
        return to_text(node.text or "", unit=node.tag == "unit")
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


def _same_values(
    generated: type | None, values: dict[str, dict[str, object]], flags: bool
) -> bool:
    """Whether a generated enumeration has the names and values of the server's definition,
    and is a flags enumeration when the definition is one"""
    if not (isinstance(generated, type) and issubclass(generated, Enum)):
        return False
    if issubclass(generated, Flag) != flags:
        return False
    return {name: member.value for name, member in generated.__members__.items()} == {
        name: value["value"] for name, value in values.items()
    }


def deprecation_warning(member: Any, service_name: str, qualified_name: str) -> str:
    """The text of the warning for a use of a member, or an empty string for a member that is
    not deprecated"""
    if not member.deprecated:
        return ""
    reason = _parse_deprecation_reason(member.deprecated_reason, service_name)
    return deprecation_message(qualified_name, reason)


def service_definitions(
    service: KRPC.Service, lookup: Lookup = registered
) -> Iterator[Definition]:
    """The types a service defines, as records that can be registered in a type registry. An
    enumeration, structure or exception uses the generated class the lookup finds for it, when
    the server's definition matches.

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
                {
                    "value": value.value,
                    "doc": _documentation(value, name),
                    "deprecated": deprecation_warning(
                        value,
                        name,
                        "%s.%s.%s" % (name, enumeration.name, _member_name(value.name)),
                    ),
                },
            )
            for value in enumeration.values
        )

        def register(types: Types) -> None:
            generated = lookup(name, enumeration.name)
            if not _same_values(generated, values, enumeration.flags):
                generated = None
            types.enumeration_type(name, enumeration.name, doc).set_values(
                values, generated, enumeration.flags
            )

        return register

    def register_exception(exception: KRPC.Exception) -> Callable[[Types], None]:
        doc = _documentation(exception, name)

        def register(types: Types) -> None:
            generated = lookup(name, exception.name)
            if not (
                isinstance(generated, type)
                and generated.__bases__ == (native_exception(exception.mapped_type),)
            ):
                generated = None
            types.exception_type(
                name, exception.name, doc, exception.mapped_type, generated
            )

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
                    "type this client knows about" % (name, struct.name),
                    stacklevel=2,
                )
                return
            fields = [
                (field_name, types.as_type(field.type))
                for field_name, field in zip(field_names, struct.fields)
            ]
            generated = lookup(name, struct.name)
            if getattr(generated, "_fields", None) != tuple(field_names):
                generated = None
            deprecated_fields = {
                field_name: deprecation_warning(
                    field, name, "%s.%s.%s" % (name, struct.name, field_name)
                )
                for field_name, field in zip(field_names, struct.fields)
                if field.deprecated
            }
            types.struct_type(name, struct.name, doc).set_fields(
                fields, generated, deprecated_fields=deprecated_fields
            )

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
    what: str,
    build: Callable[..., None],
    *args: object,
) -> None:
    """Run one step of building a service, skipping it with a warning if it names a type this
    client does not know about, rather than failing to create the service at all. That is
    what a definition from a newer server looks like, where a member has been added whose
    type is from a later version of the protocol."""
    try:
        build(*args)
    except UnknownTypeError as exn:
        warnings.warn("Skipping %s: %s" % (what, exn), stacklevel=2)


def create_service(connection: Connection, service: KRPC.Service) -> object:
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
                "_connection": connection,
                "_name": service.name,
                "_service_name": service.name,
                "_class_name": None,
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
        if connection._types.struct_type(service.name, struct.name).has_fields:
            cls._add_service_struct(struct)

    # Add procedures. An internal procedure is for the client library only, which calls it
    # directly
    for procedure in service.procedures:
        if Attributes.is_a_procedure(procedure.name) and not procedure.internal:
            _skipping_unknown_types(
                "%s.%s" % (service.name, procedure.name),
                cls._add_service_procedure,
                procedure,
            )

    # Add properties
    properties: defaultdict[str, list[KRPC.Procedure | None]] = defaultdict(
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

    return cls()  # pyright: ignore[reportCallIssue]


def _add_class_members(cls: ServiceBase, service: KRPC.Service) -> None:
    """Attach the members that a service declares for its classes to the class types."""

    def attach(
        class_name: str,
        member_name: str,
        add: Callable[..., None],
        *args: object,
    ) -> None:
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
    class_properties: defaultdict[tuple[str, str], list[KRPC.Procedure | None]] = (
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


class ServiceBase(DynamicType):
    """Base class for service objects, created at runtime
    using information received from the server."""

    _connection: Connection
    _name: str

    # The types a service defines are registered from its definitions, before any service is
    # created, so the following take the types they were registered as and make them members of
    # the service

    @classmethod
    def _add_service_type(cls, member: Any, python_type: object) -> None:
        """Make a type a member of the service"""
        warning = deprecation_warning(
            member, cls._name, "%s.%s" % (cls._name, member.name)
        )
        if warning:
            python_type = DeprecatedDefinition(member.name, warning, python_type)
        setattr(cls, member.name, python_type)

    @classmethod
    def _add_service_class(cls, remote_cls: KRPC.Class) -> None:
        """Add a class type"""
        class_type = cls._connection._types.class_type(cls._name, remote_cls.name)
        cls._add_service_type(remote_cls, class_type.python_type)

    @classmethod
    def _add_service_enumeration(cls, enumeration: KRPC.Enumeration) -> None:
        """Add an enum type"""
        enumeration_type = cls._connection._types.enumeration_type(
            cls._name, enumeration.name
        )
        cls._add_service_type(enumeration, enumeration_type.python_type)

    @classmethod
    def _add_service_struct(cls, struct: KRPC.Struct) -> None:
        """Add a structure type"""
        struct_type = cls._connection._types.struct_type(cls._name, struct.name)
        cls._add_service_type(struct, struct_type.python_type)

    @classmethod
    def _add_service_exception(cls, exception: KRPC.Exception) -> None:
        """Add an exception type"""
        exception_type = cls._connection._types.exception_type(
            cls._name, exception.name, mapped_type=exception.mapped_type
        )
        cls._add_service_type(exception, exception_type)

    @classmethod
    def _parse_procedure(
        cls, procedure: KRPC.Procedure
    ) -> tuple[
        list[str],
        list[TypeBase],
        list[bool],
        list[object | None],
        TypeBase | None,
    ]:
        param_names = [snake_case(param.name) for param in procedure.parameters]
        param_types = [
            cls._connection._types.as_type(param.type) for param in procedure.parameters
        ]
        return_type: TypeBase | None = None
        if not Types.is_none_type(procedure.return_type):
            return_type = cls._connection._types.as_type(procedure.return_type)
        # Checked before anything is built from the types, so that a procedure naming a
        # structure whose definition was skipped is skipped along with it
        for typ in param_types:
            check_type_is_known(typ)
        if return_type is not None:
            check_type_is_known(return_type)
        param_required = [not param.has_default_value for param in procedure.parameters]
        param_default: list[object | None] = []
        for param, typ in zip(procedure.parameters, param_types):
            if param.has_default_value and not param.default_value_is_null:
                location = "%s.%s parameter %s" % (
                    cls._name,
                    procedure.name,
                    snake_case(param.name),
                )
                param_default.append(
                    decode_default_value(
                        cls._connection, param.default_value, typ, location
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
            cls._connection._invoke,
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
            cls._connection._build_call,
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
        cls._add_class_method("_build_call__" + name, build_call)
        cls._add_class_method("_return_type__" + name, lambda cls: return_type)

    @classmethod
    def _add_service_property(
        cls,
        name: str,
        getter: KRPC.Procedure | None = None,
        setter: KRPC.Procedure | None = None,
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
                cls._connection._invoke,
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
                cls._connection._build_call,
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
            cls._add_method("_build_call__" + member_name, build_call)
            cls._add_method(
                "_return_type__" + member_name, lambda self: getter_return_type
            )
        if setter:
            param_names, param_types, _, _, _ = cls._parse_procedure(setter)
            setter_fn = _construct_func(
                cls._connection._invoke,
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
            build_set_call = _construct_func(
                cls._connection._build_call,
                cls._name,
                setter.name,
                ["self"],
                param_names,
                param_types,
                [True],
                [None],
                None,
            )
            cls._add_method("_build_call_set__" + member_name, build_set_call)
        cls._add_property(member_name, getter_fn, setter_fn, doc=doc)

    @classmethod
    def _add_service_class_method(
        cls, class_name: str, method_name: str, procedure: KRPC.Procedure
    ) -> None:
        """Add a method to a class"""
        class_cls = cast(
            DynamicClassBase,
            cls._connection._types.class_type(cls._name, class_name).python_type,
        )
        param_names, param_types, param_required, param_default, return_type = (
            cls._parse_procedure(procedure)
        )
        # Rename this to self if it doesn't cause a name clash
        if "self" not in param_names:
            param_names[0] = "self"
        func = _construct_func(
            cls._connection._invoke,
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
            cls._connection._build_call,
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
        class_cls._add_method("_build_call__" + name, build_call)
        class_cls._add_method("_return_type__" + name, lambda self: return_type)

    @classmethod
    def _add_service_class_static_method(
        cls, class_name: str, method_name: str, procedure: KRPC.Procedure
    ) -> None:
        """Add a static method to a class"""
        class_cls = cast(
            DynamicClassBase,
            cls._connection._types.class_type(cls._name, class_name).python_type,
        )
        param_names, param_types, param_required, param_default, return_type = (
            cls._parse_procedure(procedure)
        )
        func = _construct_func(
            cls._connection._invoke,
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
            cls._connection._build_call,
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
        class_cls._add_class_method("_build_call__" + name, build_call)
        class_cls._add_class_method("_return_type__" + name, lambda cls: return_type)

    @classmethod
    def _add_service_class_property(
        cls,
        class_name: str,
        property_name: str,
        getter: KRPC.Procedure | None = None,
        setter: KRPC.Procedure | None = None,
    ) -> None:
        """Add a property to a class"""
        class_cls = cast(
            DynamicClassBase,
            cls._connection._types.class_type(cls._name, class_name).python_type,
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
        getter_fn: Callable | None = None
        setter_fn: Callable | None = None
        if getter:
            getter_name = getter.name
            param_names, param_types, _, _, return_type = cls._parse_procedure(getter)
            # Rename this to self if it doesn't cause a name clash
            if "self" not in param_names:
                param_names[0] = "self"
            getter_fn = _construct_func(
                cls._connection._invoke,
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
                cls._connection._build_call,
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
            class_cls._add_method("_build_call__" + member_name, build_call)
            class_cls._add_method(
                "_return_type__" + member_name, lambda self: getter_return_type
            )
        if setter:
            param_names, param_types, _, _, return_type = cls._parse_procedure(setter)
            if "self" not in param_names:
                param_names[0] = "self"
            setter_fn = _construct_func(
                cls._connection._invoke,
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
            build_set_call = _construct_func(
                cls._connection._build_call,
                cls._name,
                setter.name,
                [],
                param_names,
                param_types,
                [True, True],
                [None, None],
                None,
            )
            class_cls._add_method("_build_call_set__" + member_name, build_set_call)
        class_cls._add_property(member_name, getter_fn, setter_fn, doc=doc)
