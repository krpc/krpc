import collections

# pylint: disable=no-name-in-module
from krpc.schema.KRPC_pb2 import Type
from krpc.types import (
    ValueType,
    ClassType,
    EnumerationType,
    MessageType,
    StructType,
    TupleType,
    ListType,
    SetType,
    DictionaryType,
)
from krpc.utils import snake_case
from .generator import Generator
from .docparser import DocParser
from ..lang.cnano import CnanoLanguage
from ..utils import as_type


class CnanoGenerator(Generator):

    language = CnanoLanguage()

    def __init__(self, macro_template, service, definitions):
        super().__init__(macro_template, service, definitions)
        self._collection_types = set()

    def parse_name(self, name):
        if name in self.language.keywords:
            return "%s_" % name
        return name

    def parse_type(self, typ):
        """The C type of a value at a slot. A slot carries its null out of band, so the type
        there is named without its nullability."""
        return self._parse_type(typ)

    def parse_type_at_position(self, typ):
        """The C type of a value at a position inside another value. A value is stored by
        value, so there is nowhere to put a null: a position that can hold one takes a
        generated type of its own, holding the value beside a bool saying whether it is
        there."""
        value_type = self._parse_type(typ, in_collection=True)
        if not typ.nullable:
            return value_type
        name = "nullable_%s" % value_type["name"]
        return {
            "kind": "nullable",
            "name": name,
            "nullable": True,
            "is_class": False,
            "ctype": "krpc_%s_t" % name,
            "cvtype": "krpc_%s_t *" % name,
            "ccvtype": "const krpc_%s_t *" % name,
            "getval": "",
            "getptr": "",
            "structgetval": "&",
            "structgetptr": "&",
            "removeconst": "(krpc_%s_t*)" % name,
            "value_type": value_type,
        }

    def _parse_type(self, typ, in_collection=False):
        ptr = True
        self._collection_types.add(typ)
        if isinstance(typ, ValueType):
            ctype = self.language.type_map[typ.protobuf_type.code]
            ptr = False
        elif isinstance(typ, MessageType):
            ctype = "krpc_schema_%s" % typ.python_type.__name__
        elif isinstance(typ, ListType):
            ctype = "krpc_list_%s_t" % self.parse_type_name_at_position(typ.value_type)
        elif isinstance(typ, SetType):
            ctype = "krpc_set_%s_t" % self.parse_type_name_at_position(typ.value_type)
        elif isinstance(typ, DictionaryType):
            ctype = "krpc_dictionary_%s_%s_t" % (
                self.parse_type_name_at_position(typ.key_type),
                self.parse_type_name_at_position(typ.value_type),
            )
        elif isinstance(typ, TupleType):
            ctype = "krpc_tuple_%s_t" % "_".join(
                self.parse_type_name_at_position(t) for t in typ.value_types
            )
        elif isinstance(typ, StructType):
            ctype = "krpc_%s_%s_t" % (
                typ.protobuf_type.service,
                typ.protobuf_type.name,
            )
        elif isinstance(typ, (ClassType, EnumerationType)):
            if in_collection:
                if isinstance(typ, ClassType):
                    ctype = "krpc_object_t"
                else:
                    ctype = "krpc_enum_t"
            else:
                ctype = "krpc_%s_%s_t" % (
                    typ.protobuf_type.service,
                    typ.protobuf_type.name,
                )
            ptr = False
        else:
            raise RuntimeError("Unknown type " + str(typ))
        # Note:
        #  name - name of the type used in encode/decode function names
        #  ctype - C type for the kRPC type
        #  cvtype - C 'value' type that is, for example,
        #           passed as an argument to functions
        #  ccvtype - const version of cvtype
        #  getval - gets the value from a pointer
        #  getptr - gets a pointer for a value
        #  structgetval - gets a value from a struct
        #  structgetptr - gets a pointer for a value in a struct
        #                 (equivalent to structgetval then getptr)
        #  removeconst - removes constness from a pointer for a value
        #  nullable - whether the slot or position holding the value can be null
        #  is_class - whether the value is an instance of a class
        return {
            "name": self.parse_type_name(typ),
            "nullable": typ.nullable,
            "is_class": isinstance(typ, ClassType),
            "ctype": ctype,
            "cvtype": "%s *" % ctype if ptr else ctype,
            "ccvtype": (
                "const %s *" % ctype
                if ptr
                else ("const " + ctype if ctype.endswith("*") else ctype)
            ),
            "getval": "" if ptr else "*",
            "getptr": "" if ptr else "&",
            "structgetval": "&" if ptr else "",
            "structgetptr": "&",
            "removeconst": "(%s*)" % ctype if ptr else "",
        }

    def parse_collection_type(self, typ):
        # A structure carries the values of its fields in order, which is the same encoding as
        # a tuple of those values, so both are generated as a C struct holding one member per
        # value. The members of a tuple are named for their position; those of a structure are
        # named for the field they carry.
        result = self.parse_type(typ)
        if isinstance(typ, TupleType):
            members = [self.parse_type_at_position(t) for t in typ.value_types]
            for i, member in enumerate(members):
                member["member"] = "e%d" % i
            result.update({"kind": "tuple", "value_types": members})
        elif isinstance(typ, StructType):
            members = [self.parse_type_at_position(t) for t in typ.field_types]
            for name, member in zip(typ.field_names, members):
                member["member"] = self.parse_name(snake_case(name))
            result.update({"kind": "struct", "value_types": members})
        elif isinstance(typ, (ListType, SetType)):
            result.update(
                {
                    "kind": "list" if isinstance(typ, ListType) else "set",
                    "value_type": self.parse_type_at_position(typ.value_type),
                }
            )
        elif isinstance(typ, DictionaryType):
            result.update(
                {
                    "kind": "dictionary",
                    "key_type": self.parse_type_at_position(typ.key_type),
                    "value_type": self.parse_type_at_position(typ.value_type),
                }
            )
        else:
            raise RuntimeError("Unknown type " + str(typ))
        return result

    @staticmethod
    def nullable_types_in(collection_type):
        """The generated types the given one holds at a position that can hold null, which
        are declared before it"""
        members = collection_type.get("value_types", [])
        for key in ("key_type", "value_type"):
            if key in collection_type:
                members = members + [collection_type[key]]
        return [x for x in members if x.get("kind") == "nullable"]

    def parse_type_name_at_position(self, typ):
        name = self.parse_type_name(typ)
        return "nullable_%s" % name if typ.nullable else name

    def parse_type_name(self, typ):
        if isinstance(typ, ValueType):
            return self.language.type_name_map[typ.protobuf_type.code]
        if isinstance(typ, MessageType):
            return "message_%s" % typ.python_type.__name__
        if isinstance(typ, ListType):
            return "list_%s" % self.parse_type_name_at_position(typ.value_type)
        if isinstance(typ, SetType):
            return "set_%s" % self.parse_type_name_at_position(typ.value_type)
        if isinstance(typ, DictionaryType):
            return "dictionary_%s_%s" % (
                self.parse_type_name_at_position(typ.key_type),
                self.parse_type_name_at_position(typ.value_type),
            )
        if isinstance(typ, TupleType):
            return "tuple_%s" % "_".join(
                self.parse_type_name_at_position(t) for t in typ.value_types
            )
        if isinstance(typ, StructType):
            return "%s_%s" % (typ.protobuf_type.service, typ.protobuf_type.name)
        if isinstance(typ, ClassType):
            return "object"
        if isinstance(typ, EnumerationType):
            return "enum"
        raise RuntimeError("Unknown type " + str(typ))

    def parse_return_type(self, typ):
        if typ is None:
            return {"ctype": "void", "cvtype": None, "name": None}
        typ = self.parse_type(typ)
        typ["cvtype"] = typ["ctype"] + " *"
        typ["ccvtype"] = typ["cvtype"]
        return typ

    def parse_parameter_type(self, typ):
        return self.parse_type(typ)

    @staticmethod
    def _needs_pointer(typ):
        # By-value non-class scalars and enums have no null sentinel, so a nullable one
        # is passed as a pointer (NULL means null). Strings and collections are already
        # pointers, and classes use KRPC_NULL, so they are unchanged.
        if isinstance(typ, EnumerationType):
            return True
        if isinstance(typ, ValueType):
            return typ.protobuf_type.code != Type.STRING
        return False

    def generate_context_parameters(self, name, procedure):
        parameters = super().generate_context_parameters(name, procedure)
        for parameter, info in zip(parameters, procedure["parameters"]):
            typ = as_type(self.types, info["type"])
            if not typ.nullable:
                continue
            if isinstance(typ, ClassType):
                parameter["null_check"] = "%s == KRPC_NULL" % parameter["name"]
            else:
                parameter["null_check"] = "%s == NULL" % parameter["name"]
                if self._needs_pointer(typ):
                    ctype = parameter["type"]["ctype"]
                    parameter["type"]["ccvtype"] = "const %s *" % ctype
                    parameter["type"]["getptr"] = ""
        return parameters

    def parse_default_value(self, value, typ):  # pylint: disable=unused-argument
        # No default arguments in C
        return None

    @staticmethod
    def parse_documentation(documentation):
        documentation = CNanoDocParser().parse(documentation)
        if documentation == "":
            return ""
        lines = ["/**"] + [" * " + line for line in documentation.split("\n")] + [" */"]
        return "\n".join(line.rstrip() for line in lines)

    def parse_context(self, context):
        def return_type(typ):
            typ["cvtype"] = typ["ctype"] + " *"
            typ["ccvtype"] = typ["cvtype"]
            return typ

        properties = collections.OrderedDict()
        for name, info in context["properties"].items():
            if info["getter"]:
                properties[name] = {
                    "remote_id": info["getter"]["remote_id"],
                    "parameters": [],
                    "return_type": return_type(info["type"]),
                    "documentation": info["documentation"],
                    "deprecated": info["deprecated"],
                    "deprecated_reason": info["deprecated_reason"],
                }
            if info["setter"]:
                properties["set_" + name] = {
                    "remote_id": info["setter"]["remote_id"],
                    "parameters": self.generate_context_parameters(
                        info["setter"]["remote_name"], info["setter"]["procedure"]
                    ),
                    "return_type": {"ctype": "void", "cvtype": None, "name": None},
                    "documentation": info["documentation"],
                    "deprecated": info["deprecated"],
                    "deprecated_reason": info["deprecated_reason"],
                }

        for _, class_info in context["classes"].items():
            class_properties = collections.OrderedDict()
            for name, info in class_info["properties"].items():
                if info["getter"]:
                    class_properties[name] = {
                        "remote_id": info["getter"]["remote_id"],
                        "parameters": [],
                        "return_type": return_type(info["type"]),
                        "documentation": info["documentation"],
                        "deprecated": info["deprecated"],
                        "deprecated_reason": info["deprecated_reason"],
                    }
                if info["setter"]:
                    class_properties["set_" + name] = {
                        "remote_id": info["setter"]["remote_id"],
                        "parameters": [
                            self.generate_context_parameters(
                                info["setter"]["remote_name"],
                                info["setter"]["procedure"],
                            )[1]
                        ],
                        "return_type": {"ctype": "void", "cvtype": None, "name": None},
                        "return_set_client": False,
                        "documentation": info["documentation"],
                        "deprecated": info["deprecated"],
                        "deprecated_reason": info["deprecated_reason"],
                    }
            class_info["properties"] = class_properties

        context["properties"] = properties

        # C requires a struct to be declared before it is used, so a collection type has to
        # follow the collection types it contains. Several kRPC types share one C type, as
        # every class is a krpc_object_t and every enumeration a krpc_enum_t, so collections
        # are deduplicated by what they become in C rather than by what they are here.
        collection_types = collections.OrderedDict()
        for typ in self._definitions.collection_types(
            sorted(self._collection_types, key=self.parse_type_name)
        ):
            ctype = self.parse_collection_type(typ)
            for nullable_type in self.nullable_types_in(ctype):
                collection_types.setdefault(nullable_type["name"], nullable_type)
            collection_types.setdefault(ctype["name"], ctype)
        context["collection_types"] = list(collection_types.values())

        return context


class CNanoDocParser(DocParser):

    def parse_summary(self, node):
        return self.parse_node(node).strip()

    def parse_remarks(self, node):
        return "\n\n" + self.parse_node(node).strip()

    def parse_param(self, node):
        return "\n@param %s %s" % (node.attrib["name"], self.parse_node(node).strip())

    def parse_returns(self, node):
        return "\n@return %s" % self.parse_node(node).strip()

    def parse_see(self, node):
        return self.parse_cref(node.attrib["cref"])

    @staticmethod
    def parse_paramref(node):
        return node.attrib["name"]

    @staticmethod
    def parse_a(node):
        return '<a href="%s">%s</a>' % (node.attrib["href"], node.text)

    @staticmethod
    def parse_c(node):
        return node.text

    @staticmethod
    def parse_math(node):
        return node.text

    def parse_list(self, node):
        content = [
            "- %s\n" % self.parse_node(item[0], indent=2)[2:].rstrip() for item in node
        ]
        return "\n" + "".join(content)

    @staticmethod
    def parse_cref(cref):
        if cref[0] == "M":
            cref = cref[2:].split(".")
            member = snake_case(cref[-1])
            del cref[-1]
            return "::".join(cref) + "::" + member
        if cref[0] == "T":
            return cref[2:].replace(".", "::")
        raise RuntimeError("Unknown cref '%s'" % cref)
