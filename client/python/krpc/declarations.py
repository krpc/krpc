"""What the generated service modules are built from. A module declares the classes and services
of one service for type checkers. Its enumerations, structures and exceptions are real classes.
"""

from __future__ import annotations

from typing import TYPE_CHECKING, Any

from krpc.utils import snake_case

# The base of a declared class, which gives type checkers the members every remote object has
if TYPE_CHECKING:
    from krpc.types import ClassBase as DeclaredObject
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
        return any(_names(base) == names for base in subclass.__mro__)
