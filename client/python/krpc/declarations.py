"""What the generated service modules are built from. A module declares the classes and services
of one service for type checkers. Its enumerations, structures and exceptions are real classes.
"""

from __future__ import annotations

from typing import TYPE_CHECKING, Any

from krpc.types import ClassBase
from krpc.utils import snake_case

# The base of a declared class, which gives type checkers the members every remote object has
if TYPE_CHECKING:
    DeclaredObject = ClassBase
else:
    DeclaredObject = object


def declaration_error(service_name: str, class_name: str | None) -> TypeError:
    """The error raised by calling a declaration, which names the class reached through a
    connection in its place."""
    name = service_name if class_name is None else service_name + "." + class_name
    reached = "conn." + snake_case(service_name)
    if class_name is not None:
        reached += "." + class_name
    return TypeError(
        "%s is a declaration for type checkers. Reach it through a connection, as %s"
        % (name, reached)
    )


def _names(cls: type) -> tuple[object, object] | None:
    """The service and class names a class is declared or built with, from its own namespace
    so that a subclass does not inherit them"""
    namespace = vars(cls)
    if "_service_name" not in namespace:
        return None
    return namespace["_service_name"], namespace.get("_class_name")


class DeclaredClass(type):
    """The metaclass of a declaration. A declaration is never instantiated. A class built from
    the server's definitions is an instance or subclass of it when one of its bases has the same
    service and class names."""

    _service_name: str
    _class_name: str | None

    def __call__(cls, *_args: Any, **_kwargs: Any) -> Any:
        raise declaration_error(cls._service_name, cls._class_name)

    def __instancecheck__(cls, instance: object) -> bool:
        return cls.__subclasscheck__(type(instance))

    def __subclasscheck__(cls, subclass: object) -> bool:
        if not isinstance(subclass, type):
            return False
        names = _names(cls)
        # A program's subclass of a declaration names no class, and compares as an
        # ordinary class
        if names is None:
            return type.__subclasscheck__(cls, subclass)
        return any(_names(base) == names for base in subclass.__mro__)


def is_class_type(value: object) -> bool:
    """Whether a value is a class a service defines: one built for a client, or a declaration
    of one"""
    if not isinstance(value, type):
        return False
    if issubclass(value, ClassBase):
        return True
    return isinstance(value, DeclaredClass) and value._class_name is not None


# The enumerations, structures and exceptions of the generated modules, by service and name
_registered: dict[tuple[str, str], type] = {}


def register(service_name: str, *classes: type) -> None:
    """Register the enumerations, structures and exceptions of a generated module. A client
    uses a registered class in place of building its own when the server's definition of it
    matches. A later registration replaces an earlier one."""
    for cls in classes:
        cls._service_name = service_name
        cls._class_name = cls.__name__
        _registered[(service_name, cls.__name__)] = cls


def registered(service_name: str, name: str) -> type | None:
    """The class registered for a service's enumeration, structure or exception"""
    return _registered.get((service_name, name))
