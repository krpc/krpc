import collections
import jinja2
from krpc.attributes import Attributes
from krpc.definitions import topological_order
from krpc.types import (
    DictionaryType,
    ListType,
    SetType,
    StructType,
    TupleType,
)
from krpc.utils import snake_case
from ..utils import (
    lower_camel_case,
    indent,
    single_line,
    as_type,
    decode_default_value,
    is_nullable,
)
from .docparser import flatten_deprecation_reason


class Generator:

    def __init__(self, macro_template, service, definitions):
        self._macro_template = macro_template
        self._service = service
        self._definitions = definitions
        self._defs = definitions.services[service]
        # The registry every service in the definitions was registered in, so that a type
        # another service defines resolves to what that service defines it as
        self.types = definitions.types

    @property
    def service_name(self):
        return self._service

    def generate_file(self, path):
        content = self.generate()
        with open(path, "w", encoding="utf8") as fp:
            fp.write(content)

    def generate(self):
        context = self.parse_context(self.generate_context())
        loader = jinja2.FileSystemLoader(searchpath="./")
        env = jinja2.Environment(
            loader=loader,
            trim_blocks=True,
            lstrip_blocks=True,
            undefined=jinja2.StrictUndefined,
        )
        env.filters["snake_case"] = snake_case
        env.filters["lower_camel_case"] = lower_camel_case
        env.filters["indent"] = indent
        env.filters["singleline"] = single_line
        template = env.from_string(self._macro_template)
        content = template.render(context)
        return content.rstrip() + "\n"

    # Separator between name parts when flattening a cref to a plain name
    plain_cref_separator = "."

    @staticmethod
    def parse_plain_cref_member(name):
        return name

    def parse_plain_cref(self, cref):
        """Convert a service-level cref to a plain language-specific name for
        embedding in deprecation messages. The current service's prefix is
        dropped and the member name (the last part of M: crefs) is converted
        to the language's naming convention."""
        name = cref[2:]
        prefix = self._service + "."
        if name.startswith(prefix):
            name = name[len(prefix) :]
        parts = name.split(".")
        if cref[0] == "M":
            parts[-1] = self.parse_plain_cref_member(parts[-1])
        return self.plain_cref_separator.join(parts)

    def parse_deprecation_reason(self, reason):
        """Flatten a deprecation reason to plain text, mapping cref markup to
        language-specific names."""
        return flatten_deprecation_reason(reason, self.parse_plain_cref)

    def generate_context_parameters(self, name, procedure):
        parameters = []
        for parameter in procedure["parameters"]:
            typ = as_type(self.types, parameter["type"])
            info = {
                "name": self.parse_name(parameter["name"]),
                "type": self.parse_parameter_type(typ),
            }
            if "default_value" in parameter:
                location = "%s.%s parameter %s" % (
                    self._service,
                    name,
                    parameter["name"],
                )
                value = decode_default_value(parameter["default_value"], typ, location)
                info["default_value"] = self.parse_default_value(value, typ)
            info["nullable"] = typ.nullable
            parameters.append(info)
        return parameters

    def _get_defs(self, key):
        return self._defs.get(key, {}).items()

    def generate_context(self):
        context = {
            "service_name": self._service,
            "service_id": self._defs["id"],
            "service_documentation": self.parse_documentation(
                self._defs.get("documentation", "")
            ),
            "procedures": {},
            "properties": {},
            "classes": {},
            "enumerations": {},
            "structs": {},
            "exceptions": {},
        }

        for name, cls in self._get_defs("classes"):
            context["classes"][name] = {
                "methods": {},
                "static_methods": {},
                "properties": {},
                "documentation": self.parse_documentation(cls["documentation"]),
                "deprecated": cls.get("deprecated", False),
                "deprecated_reason": self.parse_deprecation_reason(
                    cls.get("deprecated_reason", "")
                ),
            }

        for name, enumeration in self._get_defs("enumerations"):
            context["enumerations"][name] = {
                "values": [
                    {
                        "name": self.parse_name(x["name"]),
                        "value": x["value"],
                        "documentation": self.parse_documentation(x["documentation"]),
                        "deprecated": x.get("deprecated", False),
                        "deprecated_reason": self.parse_deprecation_reason(
                            x.get("deprecated_reason", "")
                        ),
                    }
                    for x in enumeration["values"]
                ],
                "documentation": self.parse_documentation(enumeration["documentation"]),
                "deprecated": enumeration.get("deprecated", False),
                "deprecated_reason": self.parse_deprecation_reason(
                    enumeration.get("deprecated_reason", "")
                ),
            }

        for name, struct in self._get_defs("structs"):
            context["structs"][name] = {
                "fields": [
                    {
                        "name": self.parse_name(x["name"]),
                        "remote_name": x["name"],
                        "krpc_type": self.as_type(x["type"]),
                        "type": self.parse_type(self.as_type(x["type"])),
                        "documentation": self.parse_documentation(x["documentation"]),
                        "deprecated": x.get("deprecated", False),
                        "deprecated_reason": self.parse_deprecation_reason(
                            x.get("deprecated_reason", "")
                        ),
                    }
                    for x in struct["fields"]
                ],
                "documentation": self.parse_documentation(struct["documentation"]),
                "deprecated": struct.get("deprecated", False),
                "deprecated_reason": self.parse_deprecation_reason(
                    struct.get("deprecated_reason", "")
                ),
            }

        for name, exception in self._get_defs("exceptions"):
            context["exceptions"][name] = {
                "documentation": self.parse_documentation(exception["documentation"]),
                "deprecated": exception.get("deprecated", False),
                "deprecated_reason": self.parse_deprecation_reason(
                    exception.get("deprecated_reason", "")
                ),
            }

        for name, procedure in self._get_defs("procedures"):
            return_type = self.get_return_type(procedure)
            if Attributes.is_a_procedure(name):
                context["procedures"][self.parse_name(name)] = {
                    "procedure": procedure,
                    "remote_name": name,
                    "remote_id": procedure["id"],
                    "parameters": self.generate_context_parameters(name, procedure),
                    "return_type": self.parse_return_type(return_type),
                    "return_is_nullable": is_nullable(return_type),
                    "documentation": self.parse_documentation(
                        procedure["documentation"]
                    ),
                    "deprecated": procedure.get("deprecated", False),
                    "deprecated_reason": self.parse_deprecation_reason(
                        procedure.get("deprecated_reason", "")
                    ),
                }

            elif Attributes.is_a_property_getter(name):
                property_name = self.parse_name(Attributes.get_property_name(name))
                if property_name not in context["properties"]:
                    context["properties"][property_name] = {
                        "type": self.parse_return_type(return_type),
                        "return_is_nullable": is_nullable(return_type),
                        "getter": None,
                        "setter": None,
                        "documentation": self.parse_documentation(
                            procedure["documentation"]
                        ),
                        "deprecated": procedure.get("deprecated", False),
                        "deprecated_reason": self.parse_deprecation_reason(
                            procedure.get("deprecated_reason", "")
                        ),
                    }
                context["properties"][property_name]["getter"] = {
                    "procedure": procedure,
                    "remote_name": name,
                    "remote_id": procedure["id"],
                }

            elif Attributes.is_a_property_setter(name):
                property_name = self.parse_name(Attributes.get_property_name(name))
                params = self.generate_context_parameters(name, procedure)
                if property_name not in context["properties"]:
                    context["properties"][property_name] = {
                        "type": params[0]["type"],
                        "return_is_nullable": params[0]["nullable"],
                        "getter": None,
                        "setter": None,
                        "documentation": self.parse_documentation(
                            procedure["documentation"]
                        ),
                        "deprecated": procedure.get("deprecated", False),
                        "deprecated_reason": self.parse_deprecation_reason(
                            procedure.get("deprecated_reason", "")
                        ),
                    }
                context["properties"][property_name]["setter"] = {
                    "procedure": procedure,
                    "remote_name": name,
                    "remote_id": procedure["id"],
                }

            elif Attributes.is_a_class_method(name):
                class_name = Attributes.get_class_name(name)
                method_name = self.parse_name(Attributes.get_class_member_name(name))
                params = self.generate_context_parameters(name, procedure)
                context["classes"][class_name]["methods"][method_name] = {
                    "procedure": procedure,
                    "remote_name": name,
                    "remote_id": procedure["id"],
                    "parameters": params[1:],
                    "return_type": self.parse_return_type(return_type),
                    "return_is_nullable": is_nullable(return_type),
                    "documentation": self.parse_documentation(
                        procedure["documentation"]
                    ),
                    "deprecated": procedure.get("deprecated", False),
                    "deprecated_reason": self.parse_deprecation_reason(
                        procedure.get("deprecated_reason", "")
                    ),
                }

            elif Attributes.is_a_class_static_method(name):
                class_name = Attributes.get_class_name(name)
                cls = context["classes"][class_name]
                method_name = self.parse_name(Attributes.get_class_member_name(name))
                cls["static_methods"][method_name] = {
                    "procedure": procedure,
                    "remote_name": name,
                    "remote_id": procedure["id"],
                    "parameters": self.generate_context_parameters(name, procedure),
                    "return_type": self.parse_return_type(return_type),
                    "return_is_nullable": is_nullable(return_type),
                    "documentation": self.parse_documentation(
                        procedure["documentation"]
                    ),
                    "deprecated": procedure.get("deprecated", False),
                    "deprecated_reason": self.parse_deprecation_reason(
                        procedure.get("deprecated_reason", "")
                    ),
                }

            elif Attributes.is_a_class_property_getter(name):
                class_name = Attributes.get_class_name(name)
                cls = context["classes"][class_name]
                property_name = self.parse_name(Attributes.get_class_member_name(name))
                if property_name not in cls["properties"]:
                    cls["properties"][property_name] = {
                        "type": self.parse_return_type(return_type),
                        "return_is_nullable": is_nullable(return_type),
                        "getter": None,
                        "setter": None,
                        "documentation": self.parse_documentation(
                            procedure["documentation"]
                        ),
                        "deprecated": procedure.get("deprecated", False),
                        "deprecated_reason": self.parse_deprecation_reason(
                            procedure.get("deprecated_reason", "")
                        ),
                    }
                cls["properties"][property_name]["getter"] = {
                    "procedure": procedure,
                    "remote_name": name,
                    "remote_id": procedure["id"],
                }

            elif Attributes.is_a_class_property_setter(name):
                class_name = Attributes.get_class_name(name)
                cls = context["classes"][class_name]
                property_name = self.parse_name(Attributes.get_class_member_name(name))
                if property_name not in cls["properties"]:
                    params = self.generate_context_parameters(name, procedure)
                    cls["properties"][property_name] = {
                        "type": params[1]["type"],
                        "return_is_nullable": params[1]["nullable"],
                        "getter": None,
                        "setter": None,
                        "documentation": self.parse_documentation(
                            procedure["documentation"]
                        ),
                        "deprecated": procedure.get("deprecated", False),
                        "deprecated_reason": self.parse_deprecation_reason(
                            procedure.get("deprecated_reason", "")
                        ),
                    }
                cls["properties"][property_name]["setter"] = {
                    "procedure": procedure,
                    "remote_name": name,
                    "remote_id": procedure["id"],
                }

        # Sort the context
        def sort_dict(x):
            return collections.OrderedDict(sorted(x.items(), key=lambda x: x[0]))

        context["procedures"] = sort_dict(context["procedures"])
        context["properties"] = sort_dict(context["properties"])
        context["enumerations"] = sort_dict(context["enumerations"])
        context["structs"] = _ordered_structs(
            sort_dict(context["structs"]), self.service_name
        )
        context["classes"] = sort_dict(context["classes"])
        context["exceptions"] = sort_dict(context["exceptions"])
        for cls in context["classes"].values():
            cls["methods"] = sort_dict(cls["methods"])
            cls["static_methods"] = sort_dict(cls["static_methods"])
            cls["properties"] = sort_dict(cls["properties"])

        return context

    def as_type(self, type_info):
        """Convert a type parsed from the service definitions into a type object"""
        return as_type(self.types, type_info)

    def get_return_type(self, procedure):
        if "return_type" not in procedure:
            return None
        return as_type(self.types, procedure["return_type"])

    def parse_name(self, name):
        return self.language.parse_name(name)

    def parse_type(self, typ):
        return self.language.parse_type(typ)

    def parse_return_type(self, typ):
        return self.parse_type(typ)

    def parse_parameter_type(self, typ):
        return self.parse_type(typ)

    def parse_default_value(self, value, typ):
        return self.language.parse_default_value(value, typ)


def _ordered_structs(structs, service_name):
    """The given structures, which are the ones the named service defines, ordered so that
    each follows the ones its fields carry. A generated declaration of a structure names the
    types of its fields, and C and C++ need a type to be declared before it is named."""

    def dependencies(item):
        _, struct = item
        names = set()
        for field in struct["fields"]:
            names.update(_struct_names_in(field["krpc_type"]))
        return [
            (name, structs[name])
            for service, name in sorted(names)
            if service == service_name and name in structs
        ]

    ordered = topological_order(structs.items(), lambda item: item[0], dependencies)
    return collections.OrderedDict(ordered)


def _struct_names_in(typ):
    """The service and name of every structure the given type is, or holds in a collection"""
    if isinstance(typ, StructType):
        yield (typ.protobuf_type.service, typ.protobuf_type.name)
    elif isinstance(typ, TupleType):
        for value_type in typ.value_types:
            yield from _struct_names_in(value_type)
    elif isinstance(typ, (ListType, SetType)):
        yield from _struct_names_in(typ.value_type)
    elif isinstance(typ, DictionaryType):
        yield from _struct_names_in(typ.key_type)
        yield from _struct_names_in(typ.value_type)
