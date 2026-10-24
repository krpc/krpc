from typing import NamedTuple

SERVICE = "service"
CLASS = "class"
ENUMERATION = "enumeration"
STRUCT = "struct"
EXCEPTION = "exception"
PROCEDURE = "procedure"
PROPERTY = "property"
CLASS_METHOD = "class_method"
CLASS_STATIC_METHOD = "class_static_method"
CLASS_PROPERTY = "class_property"
ENUMERATION_VALUE = "enumeration_value"
STRUCT_FIELD = "struct_field"


class Cref(NamedTuple):
    """What a service-level cref refers to: a type, a member of a type, or a member of the
    service itself, which has no type"""

    kind: str
    service: str
    type: str | None = None
    member: str | None = None
    # Whether a property has a getter, as one with only a setter is referred to by it
    getter: bool = True


def resolve_cref(cref, services):
    """What the given cref refers to in the given service definitions, or None if they do not
    define it."""
    if len(cref) < 3 or cref[1] != ":":
        return None
    parts = cref[2:].split(".")
    service = services.get(parts[0])
    if service is None or len(parts) > 3:
        return None
    if cref[0] == "T":
        return _resolve_type(service, parts)
    if cref[0] == "M" and len(parts) == 2:
        return _resolve_service_member(service, parts)
    if cref[0] == "M" and len(parts) == 3:
        return _resolve_type_member(service, parts)
    return None


def _resolve_type(service, parts):
    if len(parts) == 1:
        return Cref(SERVICE, parts[0])
    if len(parts) != 2:
        return None
    for key, kind in (
        ("classes", CLASS),
        ("enumerations", ENUMERATION),
        ("structs", STRUCT),
        ("exceptions", EXCEPTION),
    ):
        if parts[1] in service.get(key, {}):
            return Cref(kind, parts[0], parts[1])
    return None


def _resolve_service_member(service, parts):
    service_name, name = parts
    procedures = service.get("procedures", {})
    if name in procedures:
        return Cref(PROCEDURE, service_name, member=name)
    getter = "get_" + name in procedures
    if getter or "set_" + name in procedures:
        return Cref(PROPERTY, service_name, member=name, getter=getter)
    return None


def _resolve_type_member(service, parts):
    service_name, name, member = parts
    if name in service.get("classes", {}):
        procedures = service.get("procedures", {})
        if name + "_" + member in procedures:
            return Cref(CLASS_METHOD, service_name, name, member)
        if name + "_static_" + member in procedures:
            return Cref(CLASS_STATIC_METHOD, service_name, name, member)
        getter = name + "_get_" + member in procedures
        if getter or name + "_set_" + member in procedures:
            return Cref(CLASS_PROPERTY, service_name, name, member, getter=getter)
        return None
    enumeration = service.get("enumerations", {}).get(name)
    if enumeration is not None:
        if any(value["name"] == member for value in enumeration["values"]):
            return Cref(ENUMERATION_VALUE, service_name, name, member)
        return None
    struct = service.get("structs", {}).get(name)
    if struct is not None:
        if any(field["name"] == member for field in struct["fields"]):
            return Cref(STRUCT_FIELD, service_name, name, member)
    return None
