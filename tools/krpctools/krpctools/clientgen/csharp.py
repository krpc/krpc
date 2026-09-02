# pylint: disable=no-name-in-module
from krpc.types import (
    StructType,
    ListType,
    SetType,
    DictionaryType,
    TupleType,
)
from .generator import Generator
from ..lang.csharp import CsharpLanguage
from ..utils import as_type


class CsharpGenerator(Generator):

    language = CsharpLanguage()

    def __init__(self, macro_template, service, definitions):
        super().__init__(macro_template, service, definitions)
        # The specs the generated stubs name, each held in a static field of its own
        self._type_specs = {}

    @staticmethod
    def parse_documentation(documentation):
        documentation = documentation.replace("<doc>", "").replace("</doc>", "").strip()
        if documentation == "":
            return ""
        lines = ["/// " + line for line in documentation.split("\n")]
        content = "\n".join(line.rstrip() for line in lines)
        content = content.replace("  <param", "<param")
        content = content.replace("  <returns", "<returns")
        content = content.replace("  <remarks", "<remarks")
        return content

    def generate_context_parameters(self, name, procedure):
        parameters = super().generate_context_parameters(name, procedure)
        for i, parameter in enumerate(parameters):
            typ = as_type(self.types, procedure["parameters"][i]["type"])
            parameter["spec"] = self.parse_type_specification(typ)
            if parameter["nullable"] and self.language.takes_the_nullable_form(typ):
                parameter["type"] += "?"
            if "default_value" not in parameter:
                parameter["name_value"] = parameter["name"]
                continue
            ctype = parameter["type"]
            default_value = parameter["default_value"]
            if isinstance(typ, StructType):
                # A structure is a value type, and its default value is not a compile-time
                # constant, so the parameter takes the nullable form and the default is
                # applied where the value is encoded
                if not ctype.endswith("?"):
                    parameter["type"] = ctype + "?"
                parameter["name_value"] = "%s ?? %s" % (
                    parameter["name"],
                    default_value,
                )
                parameter["default_value"] = "null"
            elif (
                ctype.startswith("systemAlias::Tuple")
                or ctype.startswith("global::System.Collections.Generic.IList")
                or ctype.startswith("genericCollectionsAlias::ISet")
                or ctype.startswith(
                    "global::System.Collections" + ".Generic.IDictionary"
                )
            ):
                parameter["name_value"] = "%s ?? %s" % (
                    parameter["name"],
                    default_value,
                )
                parameter["default_value"] = "null"
            else:
                parameter["name_value"] = parameter["name"]
        return parameters

    def _apply_return_nullable(self, info):
        typ = self.get_return_type(info["procedure"])
        info["return_spec"] = self.parse_type_specification(typ)
        if info["return_is_nullable"] and self.language.takes_the_nullable_form(typ):
            info["return_type"] += "?"

    def _apply_property_nullable(self, info):
        # A property's value type is the same on its getter and its setter, so read it
        # from whichever accessor exists.
        typ = None
        if info["getter"]:
            typ = self.get_return_type(info["getter"]["procedure"])
        elif info["setter"]:
            param = info["setter"]["procedure"]["parameters"][-1]
            typ = as_type(self.types, param["type"])
        info["return_spec"] = self.parse_type_specification(typ)
        if info["return_is_nullable"] and self.language.takes_the_nullable_form(typ):
            info["type"] += "?"

    def parse_type_specification(self, typ):
        """The expression a generated stub names in place of the type of a slot. A slot
        carries a null out of band, so its type is named without its nullability.

        Each spec is built once and held in a static field, as building one on every call
        would allocate where naming a type does not."""
        if typ is None:
            return "null"
        expression = (
            self._named_specification(typ)
            if self._names_a_position(typ)
            else self._plain_specification(typ)
        )
        if expression not in self._type_specs:
            self._type_specs[expression] = "TypeSpecs.Spec%d" % len(self._type_specs)
        return self._type_specs[expression]

    def _specification_at_position(self, typ):
        """The expression that builds the spec of a value at a position inside another value.
        A position carries a null in the value, so a nullable type is named as one."""
        if typ.nullable:
            return self._named_specification(typ, nullable=True)
        if self._names_a_position(typ):
            return self._named_specification(typ)
        return self._plain_specification(typ)

    def _named_specification(self, typ, nullable=False):
        # A spec names the values a type contains by their position, so one that says nothing
        # is kept in place and only trailing ones are dropped
        contained = self._contained_positions(typ)
        args = [self._specification_at_position(t) for t in contained]
        while args and args[-1] == self._plain_specification(contained[len(args) - 1]):
            args.pop()
        return "%s (typeof(%s)%s)" % (
            (
                "global::KRPC.Client.TypeSpec.Null"
                if nullable
                else "new global::KRPC.Client.TypeSpec"
            ),
            self.parse_type(typ),
            "".join(", " + arg for arg in args),
        )

    def _plain_specification(self, typ):
        """The spec of a type that names no position of its own, which the C# type gives"""
        return "global::KRPC.Client.TypeSpec.For (typeof(%s))" % self.parse_type(typ)

    @staticmethod
    def _contained_positions(typ):
        """The values the type contains, in the order the encoding holds them"""
        if isinstance(typ, (ListType, SetType)):
            return [typ.value_type]
        if isinstance(typ, DictionaryType):
            return [typ.key_type, typ.value_type]
        if isinstance(typ, TupleType):
            return list(typ.value_types)
        if isinstance(typ, StructType):
            return list(typ.field_types)
        return []

    def _names_a_position(self, typ):
        """Whether a position inside the type can hold null and cannot say so through its C#
        type, which is every reference-typed position. A structure field says so on the
        generated field, so only the positions nested inside one count."""
        declared = not isinstance(typ, StructType)
        return any(
            (declared and t.nullable and not self.language.takes_the_nullable_form(t))
            or self._names_a_position(t)
            for t in self._contained_positions(typ)
        )

    def _procedure_specs(self):
        """The specs of the procedures whose values a call built from an expression cannot
        read off the C# types it has. Such a call names a nullable reference-typed position
        inside a value nowhere else, so the stub's spec is looked up by procedure name.
        """
        specs = []
        for name, procedure in self._get_defs("procedures"):
            return_type = self.get_return_type(procedure)
            parameter_types = [
                as_type(self.types, x["type"]) for x in procedure["parameters"]
            ]
            types = parameter_types + ([] if return_type is None else [return_type])
            if not any(self._names_a_position(typ) for typ in types):
                continue
            specs.append(
                {
                    "name": name,
                    "return_spec": (
                        None
                        if return_type is None
                        else self.parse_type_specification(return_type)
                    ),
                    "parameter_specs": [
                        self.parse_type_specification(typ) for typ in parameter_types
                    ],
                }
            )
        return specs

    def add_struct_field_nullability(self, context):
        """Give every nullable field of a structure the C# type that holds a null. A value
        type takes the nullable form T?, and a reference type is marked with an attribute, as
        it is nullable in C# whether the service declares it so or not."""
        for struct_info in context["structs"].values():
            for field in struct_info["fields"]:
                if not field["krpc_type"].nullable:
                    continue
                if self.language.takes_the_nullable_form(field["krpc_type"]):
                    field["type"] += "?"
                else:
                    field["attribute"] = "global::KRPC.Client.Attributes.KRPCNullable"

    def parse_context(self, context):
        for info in context["procedures"].values():
            self._apply_return_nullable(info)
        for info in context["properties"].values():
            self._apply_property_nullable(info)
        for class_name, cls in context["classes"].items():
            # A class method passes the object it is called on as its first argument
            cls["spec"] = self.parse_type_specification(
                self.types.class_type(self.service_name, class_name)
            )
            for info in cls["methods"].values():
                self._apply_return_nullable(info)
            for info in cls["static_methods"].values():
                self._apply_return_nullable(info)
            for info in cls["properties"].values():
                self._apply_property_nullable(info)
        self.add_struct_field_nullability(context)
        context["procedure_specs"] = self._procedure_specs()
        context["type_specs"] = [
            {"name": name.split(".")[-1], "spec": expression}
            for expression, name in self._type_specs.items()
        ]
        return context
