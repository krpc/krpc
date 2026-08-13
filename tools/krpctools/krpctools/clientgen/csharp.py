# pylint: disable=no-name-in-module
from krpc.schema.KRPC_pb2 import Type
from krpc.types import ValueType, EnumerationType
from .generator import Generator
from ..lang.csharp import CsharpLanguage
from ..utils import as_type


class CsharpGenerator(Generator):

    language = CsharpLanguage()

    @staticmethod
    def _is_nullable_value_type(typ):
        # C# reference types (string, byte[], classes and collections) carry null
        # naturally; only genuine value types need the nullable form T?.
        if isinstance(typ, EnumerationType):
            return True
        if isinstance(typ, ValueType):
            return typ.protobuf_type.code not in (Type.STRING, Type.BYTES)
        return False

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
            if parameter["nullable"] and self._is_nullable_value_type(typ):
                parameter["type"] += "?"
            if "default_value" not in parameter:
                parameter["name_value"] = parameter["name"]
                continue
            ctype = parameter["type"]
            default_value = parameter["default_value"]
            if (
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
        procedure = info["procedure"]
        nullable = procedure.get("return_is_nullable", False)
        info["return_is_nullable"] = nullable
        if nullable and self._is_nullable_value_type(self.get_return_type(procedure)):
            info["return_type"] += "?"

    def _apply_property_nullable(self, info):
        # A property's nullability spans its getter and setter; the value type is the
        # same on both, so read the flag from whichever accessor exists.
        nullable = False
        typ = None
        if info["getter"]:
            procedure = info["getter"]["procedure"]
            nullable = procedure.get("return_is_nullable", False)
            typ = self.get_return_type(procedure)
        elif info["setter"]:
            procedure = info["setter"]["procedure"]
            param = procedure["parameters"][-1]
            nullable = param.get("nullable", False)
            typ = as_type(self.types, param["type"])
        info["return_is_nullable"] = nullable
        if nullable and typ is not None and self._is_nullable_value_type(typ):
            info["type"] += "?"

    def parse_context(self, context):
        for info in context["procedures"].values():
            self._apply_return_nullable(info)
        for info in context["properties"].values():
            self._apply_property_nullable(info)
        for cls in context["classes"].values():
            for info in cls["methods"].values():
                self._apply_return_nullable(info)
            for info in cls["static_methods"].values():
                self._apply_return_nullable(info)
            for info in cls["properties"].values():
                self._apply_property_nullable(info)
        return context
