from krpc.types import (
    ValueType,
    ClassType,
    EnumerationType,
    StructType,
    MessageType,
    TupleType,
    ListType,
    SetType,
    DictionaryType,
)
from krpc.utils import snake_case
from .domain import Domain
from .nodes import (
    Procedure,
    Property,
    Class,
    ClassMethod,
    ClassStaticMethod,
    ClassProperty,
)
from .nodes import Enumeration, EnumerationValue, Struct, StructField
from ..lang.python import PythonLanguage


class PythonDomain(Domain):
    name = "python"
    prettyname = "Python"
    sphinxname = "py"
    highlight = "py"
    codeext = "py"
    language = PythonLanguage()

    def currentmodule(self, name):
        super().currentmodule(name)
        return ".. currentmodule:: %s" % name

    def method_name(self, name):
        name = snake_case(name)
        if name in self.language.keywords:
            return "%s_" % name
        return name

    def type_description(self, typ):
        description = self._type_description(typ)
        # Python writes a value that can be null as the type or None
        return "%s or None" % description if typ.nullable else description

    def _type_description(self, typ):
        if isinstance(typ, ValueType):
            return self.language.parse_type(typ)
        if isinstance(typ, MessageType):
            return ":class:`krpc.schema.KRPC.%s`" % typ.python_type.__name__
        if isinstance(typ, ClassType):
            return ":class:`%s`" % self.type(typ)
        if isinstance(typ, EnumerationType):
            return ":class:`%s`" % self.type(typ)
        if isinstance(typ, StructType):
            return ":class:`%s`" % self.type(typ)
        if isinstance(typ, ListType):
            return "list(%s)" % self.type_description(typ.value_type)
        if isinstance(typ, DictionaryType):
            return "dict(%s, %s)" % (
                self.type_description(typ.key_type),
                self.type_description(typ.value_type),
            )
        if isinstance(typ, SetType):
            return "set(%s)" % self.type_description(typ.value_type)
        if isinstance(typ, TupleType):
            return "tuple(%s)" % ", ".join(
                self.type_description(t) for t in typ.value_types
            )
        raise RuntimeError("Unknown type '%s'" % str(typ))

    def ref(self, obj):
        name = obj.fullname
        # A procedure, property or method is documented under the name the client
        # generates for it, which escapes a python keyword; an enumeration value
        # or a structure field is documented under its plain snake case name
        if any(
            isinstance(obj, cls)
            for cls in (
                Procedure,
                Property,
                ClassMethod,
                ClassStaticMethod,
                ClassProperty,
            )
        ):
            name = name.split(".")
            name[-1] = self.method_name(name[-1])
            name = ".".join(name)
        elif any(isinstance(obj, cls) for cls in (EnumerationValue, StructField)):
            name = name.split(".")
            name[-1] = snake_case(name[-1])
            name = ".".join(name)
        return self.shorten_ref(name)

    def see(self, obj):
        if any(
            isinstance(obj, cls)
            for cls in (Property, ClassProperty, EnumerationValue, StructField)
        ):
            prefix = "attr"
        elif any(
            isinstance(obj, cls) for cls in (Procedure, ClassMethod, ClassStaticMethod)
        ):
            prefix = "meth"
        elif any(isinstance(obj, cls) for cls in (Class, Enumeration, Struct)):
            prefix = "class"
        else:
            raise RuntimeError(str(obj))
        return ":%s:`%s`" % (prefix, self.ref(obj))

    @staticmethod
    def paramref(name):
        return Domain.paramref(snake_case(name))
