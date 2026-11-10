import inspect
import unittest
from types import ModuleType

from krpc.declarations import DeclaredClass
from krpc.services import krpc as krpc_module
from krpc.services import testservice
from krpc.test.servertestcase import ServerTestCase
from krpc.types import DeprecatedDefinition


def _member(cls: type, name: str) -> object:
    """A member of a class, with a deprecated type on a built service unwrapped"""
    member = inspect.getattr_static(cls, name)
    return member.value if isinstance(member, DeprecatedDefinition) else member


def _members(cls: type) -> dict[str, object]:
    """The public members a class declares or is built with, by name"""
    return {name: _member(cls, name) for name in dir(cls) if not name.startswith("_")}


def _parameters(func: object, skip_first: bool) -> list[tuple[str, bool]]:
    """The names of a function's parameters, and whether each has a default"""
    parameters = list(inspect.signature(func).parameters.values())  # pyright: ignore[reportArgumentType]
    if skip_first:
        parameters = parameters[1:]
    return [(p.name, p.default is not inspect.Parameter.empty) for p in parameters]


def _signature(member: object) -> object:
    """What a member is, and the parameters it takes, in a form a declaration and a built
    member compare equal in"""
    if isinstance(member, property):
        # A declaration gives a property with only a setter a getter, to name its type
        return ("property", member.fset is not None)
    if isinstance(member, (staticmethod, classmethod)):
        # A built static method or procedure takes the class first
        return ("method", _parameters(member.__func__, isinstance(member, classmethod)))
    if inspect.isfunction(member):
        return ("method", _parameters(member, True))
    return ("type", member)


class TestDeclarationsDrift(ServerTestCase, unittest.TestCase):
    """Each declaration in a generated module against the service the client builds from the
    same definitions, so that a type checker sees the members the client has. A member compares
    by its kind and the names of its parameters, and a type by identity or as a subclass."""

    def check_class(self, declared: type, built: type) -> None:
        declared_members = _members(declared)
        built_members = _members(built)
        for name, member in declared_members.items():
            with self.subTest(member="%s.%s" % (declared.__name__, name)):
                self.assertIn(name, built_members)
                built_member = built_members[name]
                if isinstance(member, DeclaredClass):
                    assert isinstance(built_member, type)
                    self.assertTrue(issubclass(built_member, member))
                elif isinstance(member, type):
                    # The client uses the generated enumeration, structure or exception, as
                    # the server's definition matches
                    self.assertIs(member, built_member)
                else:
                    self.assertEqual(_signature(member), _signature(built_member))

    def check_module(self, module: ModuleType, service: object) -> None:
        service_name = type(service).__name__
        declared_service = getattr(module, service_name)
        self.check_class(declared_service, type(service))
        for declared in vars(module).values():
            if (
                isinstance(declared, DeclaredClass)
                and declared._service_name == service_name
                and declared._class_name is not None
            ):
                built = _member(type(service), declared._class_name)
                assert isinstance(built, type)
                self.check_class(declared, built)

    def test_test_service(self) -> None:
        self.check_module(testservice, self.conn.test_service)

    def test_krpc(self) -> None:
        self.check_module(krpc_module, self.conn.krpc)


if __name__ == "__main__":
    unittest.main()
