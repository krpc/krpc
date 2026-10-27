"""Generators for the dispatch of a conformance testee: the code that calls a procedure of a
service through the client's generated stub, given the name the server gives the procedure and
its arguments as conformance Values.

Each generator builds the conversion of a value from the helpers the testee defines, one per
kind of value, composed for a collection, and generates a conversion for each struct. It
covers the procedures whose parameters and return value are of a kind the testee protocol
carries. A testee reports any other procedure as one it cannot call.
"""

from __future__ import annotations

from krpc.attributes import Attributes
from krpc.schema.KRPC_pb2 import Type
from krpc.types import (
    ClassType,
    DictionaryType,
    EnumerationType,
    ListType,
    MessageType,
    SetType,
    StructType,
    TupleType,
    ValueType,
)
from krpc.utils import snake_case

from ..lang.cpp import CppLanguage
from ..lang.csharp import CsharpLanguage
from ..lang.java import JavaLanguage
from .cnano import CnanoGenerator
from .generator import Generator

# The kind of value each value type converts as, named after the field of a conformance Value
VALUE_KINDS = {
    Type.DOUBLE: "double",
    Type.FLOAT: "float",
    Type.SINT32: "sint32",
    Type.SINT64: "sint64",
    Type.UINT32: "uint32",
    Type.UINT64: "uint64",
    Type.BOOL: "bool",
    Type.STRING: "string",
    Type.BYTES: "bytes",
}

PROCEDURE = "procedure"
GETTER = "getter"
SETTER = "setter"
METHOD = "method"
STATIC_METHOD = "static_method"
CLASS_GETTER = "class_getter"
CLASS_SETTER = "class_setter"


class Unsupported(Exception):
    """A type the testee protocol, or the language's dispatch, cannot carry"""


def _kind(typ):
    """The kind of value a scalar, string or bytes type converts as"""
    if isinstance(typ, ValueType) and typ.protobuf_type.code in VALUE_KINDS:
        return VALUE_KINDS[typ.protobuf_type.code]
    raise Unsupported()


class DispatchGenerator(Generator):
    """The members of a service a testee can call, each with the code that calls it and, for
    a member that returns a value, the code that streams it. The conversion of a value is a
    dict of strings whose keys each language's template knows."""

    def generate_context(self):
        # The structs the covered members carry, each after the structs it holds
        self._structs = []
        members = []
        for name, procedure in sorted(self._get_defs("procedures")):
            member = self._member(name, procedure)
            if member is not None:
                members.append(member)
        return {
            "service_name": self._service,
            "members": members,
            "structs": self._structs,
            "exceptions": [name for name, _ in sorted(self._get_defs("exceptions"))],
            "includes": self.includes(),
        }

    def parse_context(self, context):
        return context

    def parse_documentation(self, documentation):
        return ""

    def includes(self):
        # The dispatch of a service includes only the service's own stubs
        return []

    def convert(self, typ):
        """The conversion of a value of a type, or raise Unsupported"""
        if isinstance(typ, StructType):
            return self.struct(typ, self._struct(typ))
        if isinstance(typ, ClassType):
            return self.object(typ)
        if isinstance(typ, EnumerationType):
            return self.enum(typ)
        if isinstance(typ, ListType):
            return self.list(typ, self.convert(typ.value_type))
        if isinstance(typ, SetType):
            return self.set(typ, self.convert(typ.value_type))
        if isinstance(typ, DictionaryType):
            return self.dictionary(
                typ, self.convert(typ.key_type), self.convert(typ.value_type)
            )
        if isinstance(typ, TupleType):
            return self.tuple(typ, [self.convert(t) for t in typ.value_types])
        if isinstance(typ, MessageType) and typ.protobuf_type.code == Type.EVENT:
            return self.event(typ)
        if isinstance(typ, MessageType):
            return self.message(typ)
        return self.scalar(typ, _kind(typ))

    def _struct(self, typ):
        """The context of a struct, generating its conversion on first use. Only a struct of
        this service can be converted, as its conversion is generated with the service."""
        service = typ.protobuf_type.service
        name = typ.protobuf_type.name
        for struct in self._structs:
            if struct["name"] == name:
                return struct
        defs = dict(self._get_defs("structs"))
        if service != self._service or name not in defs:
            raise Unsupported()
        fields = []
        for field in defs[name]["fields"]:
            conversion = self.convert(self.as_type(field["type"]))
            conversion["remote_name"] = field["name"]
            fields.append(conversion)
        struct = {
            "name": name,
            "class": self.class_type(service, name),
            "fields": fields,
        }
        self._structs.append(struct)
        return struct

    def _member(self, name, procedure):
        # A member that fails to convert must leave no struct behind
        structs = list(self._structs)
        try:
            return self._convert_member(name, procedure)
        except Unsupported:
            self._structs = structs
            return None

    def _convert_member(self, name, procedure):
        parameters = []
        for i, parameter in enumerate(procedure["parameters"]):
            value = self.parameter(self.as_type(parameter["type"]))
            value["name"] = "a%d" % i
            value["index"] = i
            parameters.append(value)
        return_type = self.get_return_type(procedure)
        result = None
        if return_type is not None:
            result = self.result(return_type)
        # An event is a handle to a stream, and a procedure that returns one or another
        # message is not streamed
        streamed = return_type is not None and not isinstance(return_type, MessageType)
        member_kind, cls, member_name = _classify(name)
        # A call for each number of arguments, where the stub fills in the defaults of the
        # arguments left off the end
        counts = [len(parameters)]
        if self.has_defaults:
            required = len(parameters)
            while (
                required > 0
                and "default_value" in procedure["parameters"][required - 1]
            ):
                required -= 1
            counts = list(range(required, len(parameters) + 1))
        variants = []
        for count in counts:
            stream = None
            procedure_call = None
            if streamed:
                stream = self.stream(member_kind, cls, member_name, parameters[:count])
                procedure_call = self.procedure_call(
                    member_kind, cls, member_name, parameters[:count]
                )
            variants.append(
                {
                    "count": count,
                    "parameters": parameters[:count],
                    "call": self.call(
                        member_kind, cls, member_name, parameters[:count]
                    ),
                    "stream": stream,
                    "procedure_call": procedure_call,
                }
            )
        return {
            "remote_name": name,
            "parameters": parameters,
            "result": result,
            "variants": variants,
            "stream": variants[-1]["stream"],
        }

    # Whether the stubs declare default arguments
    has_defaults = False

    def parameter(self, typ):
        """The conversion of an argument"""
        if isinstance(typ, MessageType):
            raise Unsupported()
        return self.convert(typ)

    def result(self, typ):
        """The conversion of a return value"""
        return self.convert(typ)

    def class_type(self, service, name):
        """The name of a class, enum or struct in the language, in full"""
        raise NotImplementedError

    def scalar(self, typ, kind):
        raise NotImplementedError

    def object(self, typ):
        raise NotImplementedError

    def enum(self, typ):
        raise NotImplementedError

    def struct(self, typ, struct):
        raise NotImplementedError

    def event(self, typ) -> dict:
        """The conversion of an event, which is only ever returned. Unsupported where the
        client has no events."""
        raise Unsupported()

    def message(self, typ) -> dict:
        """The conversion of a protobuf message, which is only ever returned. Unsupported
        where the testee cannot serialize one."""
        raise Unsupported()

    def list(self, typ, item):
        raise NotImplementedError

    def set(self, typ, item):
        raise NotImplementedError

    def dictionary(self, typ, key, value):
        raise NotImplementedError

    def tuple(self, typ, items):
        raise NotImplementedError

    def call(self, kind, cls, name, parameters):
        """The code that calls a member, given the names of its arguments"""
        raise NotImplementedError

    def procedure_call(self, kind, cls, name, parameters) -> str | None:
        """The code that builds the call a stream of a member makes, as a ProcedureCall"""
        return None

    def stream(self, kind, cls, name, parameters) -> str | None:
        """The code that streams a member, or None where the client has no streams"""
        return None


def _classify(name):
    """The kind of member a procedure is, its class, and the member's name on the server"""
    if Attributes.is_a_class_static_method(name):
        return STATIC_METHOD, Attributes.get_class_name(name), _class_member(name)
    if Attributes.is_a_class_property_getter(name):
        return CLASS_GETTER, Attributes.get_class_name(name), _class_member(name)
    if Attributes.is_a_class_property_setter(name):
        return CLASS_SETTER, Attributes.get_class_name(name), _class_member(name)
    if Attributes.is_a_class_method(name):
        return METHOD, Attributes.get_class_name(name), _class_member(name)
    if Attributes.is_a_property_getter(name):
        return GETTER, None, Attributes.get_property_name(name)
    if Attributes.is_a_property_setter(name):
        return SETTER, None, Attributes.get_property_name(name)
    return PROCEDURE, None, name


def _class_member(name):
    return Attributes.get_class_member_name(name)


def _arguments(parameters, start=0):
    return ", ".join(x["name"] for x in parameters[start:])


class CsharpDispatchGenerator(DispatchGenerator):
    """A conversion is an arg, a Func from a Value or a method group, and a result, a Func to
    a Value or a method group, with the C# type it converts."""

    language = CsharpLanguage()
    has_defaults = True

    def class_type(self, service, name):
        return "global::KRPC.Client.Services.%s.%s" % (service, name)

    def _nullable(self, typ, conversion):
        # A value type takes the nullable form T? where it can hold null
        if typ.nullable and self.language.takes_the_nullable_form(typ):
            return {
                "arg": "Arg.Nullable (%s)" % conversion["arg"],
                "result": "Result.Nullable<%s> (%s)"
                % (conversion["type"], conversion["result"]),
                "type": conversion["type"] + "?",
            }
        return conversion

    def scalar(self, typ, kind):
        conversion = {
            "arg": "Arg.%s" % kind.capitalize(),
            "result": "Result.%s" % kind.capitalize(),
            "type": self.language.parse_type(typ),
        }
        return self._nullable(typ, conversion)

    def object(self, typ):
        cls = self.language.parse_type(typ)
        return {"arg": "Arg.Object<%s>" % cls, "result": "Result.Object", "type": cls}

    def event(self, typ) -> dict:
        return {"result": "Result.Event", "type": "global::KRPC.Client.Event"}

    def message(self, typ) -> dict:
        return {"result": "Result.Message", "type": self.language.parse_type(typ)}

    def enum(self, typ):
        cls = self.language.parse_type(typ)
        conversion = {
            "arg": "Arg.Enum<%s>" % cls,
            "result": "Result.Enum<%s> ()" % cls,
            "type": cls,
        }
        return self._nullable(typ, conversion)

    def struct(self, typ, struct):
        name = "%s_%s" % (self._service, struct["name"])
        conversion = {
            "arg": "Arg_%s" % name,
            "result": "Result_%s" % name,
            "type": struct["class"],
        }
        return self._nullable(typ, conversion)

    def list(self, typ, item):
        return {
            "arg": "Arg.List (%s)" % item["arg"],
            "result": "Result.List<%s> (%s)" % (item["type"], item["result"]),
            "type": "global::System.Collections.Generic.IList<%s>" % item["type"],
        }

    def set(self, typ, item):
        return {
            "arg": "Arg.Set (%s)" % item["arg"],
            "result": "Result.Set<%s> (%s)" % (item["type"], item["result"]),
            "type": "genericCollectionsAlias::ISet<%s>" % item["type"],
        }

    def dictionary(self, typ, key, value):
        types = "%s,%s" % (key["type"], value["type"])
        return {
            "arg": "Arg.Dictionary (%s, %s)" % (key["arg"], value["arg"]),
            "result": "Result.Dictionary<%s> (%s, %s)"
            % (types, key["result"], value["result"]),
            "type": "global::System.Collections.Generic.IDictionary<%s>" % types,
        }

    def tuple(self, typ, items):
        types = ",".join(x["type"] for x in items)
        return {
            "arg": "Arg.Tuple (%s)" % ", ".join(x["arg"] for x in items),
            "result": "Result.Tuple<%s> (%s)"
            % (types, ", ".join(x["result"] for x in items)),
            "type": "systemAlias::Tuple<%s>" % types,
        }

    def call(self, kind, cls, name, parameters):
        service = "connection.%s ()" % self._service
        if kind == PROCEDURE:
            return "%s.%s (%s)" % (service, name, _arguments(parameters))
        if kind == GETTER:
            return "%s.%s" % (service, name)
        if kind == SETTER:
            return "%s.%s = a0" % (service, name)
        if kind == STATIC_METHOD:
            args = ", ".join(["connection"] + [x["name"] for x in parameters])
            return "%s.%s (%s)" % (self.class_type(self._service, cls), name, args)
        if kind == METHOD:
            return "a0.%s (%s)" % (name, _arguments(parameters, 1))
        if kind == CLASS_GETTER:
            return "a0.%s" % name
        return "a0.%s = a1" % name

    def stream(self, kind, cls, name, parameters):
        return "connection.AddStream (() => %s)" % self.call(
            kind, cls, name, parameters
        )

    def procedure_call(self, kind, cls, name, parameters):
        return "Connection.GetCall (() => %s)" % self.call(kind, cls, name, parameters)


class JavaDispatchGenerator(DispatchGenerator):
    """A conversion is an arg, a Function from a Value, and a result, a Function to a Value,
    with the boxed Java type it converts."""

    language = JavaLanguage()

    def class_type(self, service, name):
        return "krpc.client.services.%s.%s" % (service, name)

    def _nullable(self, typ, conversion):
        # A primitive takes its boxed form where it can hold null, and the conversion
        # passes null through
        if typ.nullable:
            conversion["arg"] = "Values.argNullable(%s)" % conversion["arg"]
        return conversion

    def _boxed(self, typ):
        # The form a collection holds a value in
        return self.language._at_position(typ)

    def scalar(self, typ, kind):
        conversion = {
            "arg": "Values::arg%s" % kind.capitalize(),
            "result": "Values::result%s" % kind.capitalize(),
            "boxed": self._boxed(typ),
        }
        if typ.nullable:
            conversion["result"] = "Values.resultNullable(%s)" % conversion["result"]
        return self._nullable(typ, conversion)

    def object(self, typ):
        cls = self.class_type(typ.protobuf_type.service, typ.protobuf_type.name)
        return {
            "arg": "Values.argObject(%s.class)" % cls,
            "result": "Values::resultObject",
            "boxed": cls,
        }

    def event(self, typ) -> dict:
        return {"result": "Values::resultEvent", "boxed": "krpc.client.Event"}

    def message(self, typ) -> dict:
        return {"result": "Values::resultMessage", "boxed": self._boxed(typ)}

    def enum(self, typ):
        cls = self.class_type(typ.protobuf_type.service, typ.protobuf_type.name)
        return {
            "arg": "Values.argEnum(%s::fromValue)" % cls,
            "result": "Values::resultEnum",
            "boxed": cls,
        }

    def struct(self, typ, struct):
        name = "%s%s" % (self._service, struct["name"])
        return {
            "arg": "Values.argNullable(%sDispatch::arg%s)" % (self._service, name),
            "result": "Values.resultNullable(%sDispatch::result%s)"
            % (self._service, name),
            "boxed": struct["class"],
        }

    def list(self, typ, item):
        return {
            "arg": "Values.argList(%s)" % item["arg"],
            "result": "Values.<%s>resultList(%s)" % (item["boxed"], item["result"]),
            "boxed": "java.util.List<%s>" % item["boxed"],
        }

    def set(self, typ, item):
        return {
            "arg": "Values.argSet(%s)" % item["arg"],
            "result": "Values.<%s>resultSet(%s)" % (item["boxed"], item["result"]),
            "boxed": "java.util.Set<%s>" % item["boxed"],
        }

    def dictionary(self, typ, key, value):
        types = "%s,%s" % (key["boxed"], value["boxed"])
        return {
            "arg": "Values.argMap(%s, %s)" % (key["arg"], value["arg"]),
            "result": "Values.<%s>resultMap(%s, %s)"
            % (types, key["result"], value["result"]),
            "boxed": "java.util.Map<%s>" % types,
        }

    def tuple(self, typ, items):
        name = self.language.get_tuple_class_name(typ.value_types)
        types = ",".join(x["boxed"] for x in items)
        return {
            "arg": "Values.arg%s(%s)" % (name, ", ".join(x["arg"] for x in items)),
            "result": "Values.<%s>result%s(%s)"
            % (types, name, ", ".join(x["result"] for x in items)),
            "boxed": "org.javatuples.%s<%s>" % (name, types),
        }

    def _name(self, name):
        return self.language.parse_name(name)

    def call(self, kind, cls, name, parameters):
        service = "%s.newInstance(connection)" % self._service
        if kind == PROCEDURE:
            return "%s.%s(%s)" % (service, self._name(name), _arguments(parameters))
        if kind == GETTER:
            return "%s.get%s()" % (service, name)
        if kind == SETTER:
            return "%s.set%s(a0)" % (service, name)
        if kind == STATIC_METHOD:
            args = ", ".join(["connection"] + [x["name"] for x in parameters])
            return "%s.%s(%s)" % (
                self.class_type(self._service, cls),
                self._name(name),
                args,
            )
        if kind == METHOD:
            return "a0.%s(%s)" % (self._name(name), _arguments(parameters, 1))
        if kind == CLASS_GETTER:
            return "a0.get%s()" % name
        return "a0.set%s(a1)" % name

    def stream(self, kind, cls, name, parameters):
        if kind == PROCEDURE:
            return 'connection.addStream(%s.class, "%s"%s)' % (
                self._service,
                self._name(name),
                "".join(", " + x["name"] for x in parameters),
            )
        if kind == GETTER:
            return 'connection.addStream(%s.class, "get%s")' % (self._service, name)
        if kind == STATIC_METHOD:
            # The stub of a static method takes the connection first
            return 'connection.addStream(%s.class, "%s", connection%s)' % (
                self.class_type(self._service, cls),
                self._name(name),
                "".join(", " + x["name"] for x in parameters),
            )
        if kind == METHOD:
            return 'connection.addStream(a0, "%s"%s)' % (
                self._name(name),
                "".join(", " + x["name"] for x in parameters[1:]),
            )
        return 'connection.addStream(a0, "get%s")' % name

    def procedure_call(self, kind, cls, name, parameters):
        return self.stream(kind, cls, name, parameters).replace(
            "connection.addStream(", "connection.getCall(", 1
        )


class CppDispatchGenerator(DispatchGenerator):
    """A conversion is a tag, a type with an arg and a result function, built from the tags
    in dispatch_values.hpp and a tag generated for each struct."""

    language = CppLanguage()
    has_defaults = True

    def class_type(self, service, name):
        return "::krpc::services::%s::%s" % (service, name)

    def _nullable(self, typ, tag):
        if typ.nullable:
            return {"tag": "Optional<%s>" % tag}
        return {"tag": tag}

    def scalar(self, typ, kind):
        return self._nullable(typ, kind.capitalize())

    def object(self, typ):
        cls = self.class_type(typ.protobuf_type.service, typ.protobuf_type.name)
        return self._nullable(typ, "Object<%s>" % cls)

    def event(self, typ) -> dict:
        return {"tag": "EventHandle"}

    def message(self, typ) -> dict:
        return {"tag": "Message<%s>" % self.language.parse_type(typ)}

    def enum(self, typ):
        cls = self.class_type(typ.protobuf_type.service, typ.protobuf_type.name)
        return self._nullable(typ, "Enum<%s>" % cls)

    def struct(self, typ, struct):
        struct["field_names"] = [self._name(x["remote_name"]) for x in struct["fields"]]
        return self._nullable(typ, "%s_%s" % (self._service, struct["name"]))

    def list(self, typ, item):
        return self._nullable(typ, "List<%s>" % item["tag"])

    def set(self, typ, item):
        return self._nullable(typ, "Set<%s>" % item["tag"])

    def dictionary(self, typ, key, value):
        return self._nullable(typ, "Dictionary<%s, %s>" % (key["tag"], value["tag"]))

    def tuple(self, typ, items):
        return self._nullable(typ, "Tuple<%s>" % ", ".join(x["tag"] for x in items))

    def _name(self, name):
        return self.language.parse_name(name)

    def call(self, kind, cls, name, parameters, suffix=""):
        service = "::krpc::services::%s(&client)" % self._service
        if kind == PROCEDURE:
            return "%s.%s%s(%s)" % (
                service,
                self._name(name),
                suffix,
                _arguments(parameters),
            )
        if kind == GETTER:
            return "%s.%s%s()" % (service, self._name(name), suffix)
        if kind == SETTER:
            return "%s.set_%s(a0)" % (service, self._name(name))
        if kind == STATIC_METHOD:
            args = ", ".join(["client"] + [x["name"] for x in parameters])
            return "%s::%s%s(%s)" % (
                self.class_type(self._service, cls),
                self._name(name),
                suffix,
                args,
            )
        if kind == METHOD:
            return "a0.%s%s(%s)" % (self._name(name), suffix, _arguments(parameters, 1))
        if kind == CLASS_GETTER:
            return "a0.%s%s()" % (self._name(name), suffix)
        return "a0.set_%s(a1)" % self._name(name)

    def stream(self, kind, cls, name, parameters):
        return self.call(kind, cls, name, parameters, "_stream")

    def procedure_call(self, kind, cls, name, parameters):
        return self.call(kind, cls, name, parameters, "_call")


class CnanoDispatchGenerator(DispatchGenerator, CnanoGenerator):
    """A conversion is a tag, as for C++, over the C types the C-nano generator names. The C
    type of a value depends on its position, so a conversion carries a tag for a slot and an
    element tag for a position inside another value. A tag is generated for each struct and
    each tuple, as their C types name their fields."""

    def generate_context(self):
        self._tuples = []
        context = super().generate_context()
        context["tuples"] = self._tuples
        return context

    def class_type(self, service, name):
        return "krpc_%s_%s_t" % (service, name)

    def convert(self, typ):
        element = self._tag(typ, True)
        if typ.nullable:
            # A position that can hold null holds the value beside a has_value flag
            ctype = self.parse_type_at_position(typ)["ctype"]
            element = "Nullable<%s, %s>" % (ctype, element)
        return {"tag": self._tag(typ, False), "element": element}

    def _tag(self, typ, in_collection):
        if isinstance(typ, StructType):
            return self._struct_tag(self._struct(typ))
        if isinstance(typ, ClassType):
            return "Object"
        if isinstance(typ, EnumerationType):
            return "Enum<%s>" % self._parse_type(typ, in_collection)["ctype"]
        if isinstance(typ, (ListType, SetType)):
            item = self.convert(typ.value_type)
            return "%s<%s, %s>" % (
                "List" if isinstance(typ, ListType) else "Set",
                self.parse_type(typ)["ctype"],
                item["element"],
            )
        if isinstance(typ, DictionaryType):
            key = self.convert(typ.key_type)
            value = self.convert(typ.value_type)
            return "Dictionary<%s, %s, %s>" % (
                self.parse_type(typ)["ctype"],
                key["element"],
                value["element"],
            )
        if isinstance(typ, TupleType):
            return self._tuple_tag(typ)
        if isinstance(typ, MessageType) and typ.protobuf_type.code != Type.EVENT:
            # A nanopb struct, encoded again with its field descriptions
            ctype = self.parse_type(typ)["ctype"]
            return "Message<%s, %s_fields>" % (ctype, ctype)
        return _kind(typ).capitalize()

    def _struct_tag(self, struct):
        struct["field_names"] = [snake_case(x["remote_name"]) for x in struct["fields"]]
        struct["ctype"] = self.class_type(self._service, struct["name"])
        return "%s_%s" % (self._service, struct["name"])

    def _tuple_tag(self, typ):
        ctype = self.parse_type(typ)["ctype"]
        name = ctype[len("krpc_") : -len("_t")]
        if all(x["name"] != name for x in self._tuples):
            items = [self.convert(t)["element"] for t in typ.value_types]
            self._tuples.append({"name": name, "ctype": ctype, "items": items})
        return name

    def parameter(self, typ):
        """How the client function takes an argument: a collection, struct or tuple by
        pointer, and a nullable scalar or enum by pointer, which is NULL for null"""
        if isinstance(typ, MessageType):
            raise Unsupported()
        conversion = self.convert(typ)
        by_pointer = isinstance(
            typ, (StructType, ListType, SetType, DictionaryType, TupleType)
        )
        conversion["optional"] = by_pointer and typ.nullable
        if self.language.passes_null_by_pointer(typ):
            conversion["optional"] = True
            by_pointer = True
        conversion["by_pointer"] = by_pointer
        # A string is NULL for null, which a parameter that is not nullable cannot take
        if isinstance(typ, ValueType) and _kind(typ) == "string" and not typ.nullable:
            conversion["tag"] = "NotNull<%s>" % conversion["tag"]
        return conversion

    def result(self, typ):
        conversion = self.convert(typ)
        conversion["ctype"] = self.parse_type(typ)["ctype"]
        conversion["null_flag"] = self.language.returns_null_flag(typ)
        return conversion

    def call(self, kind, cls, name, parameters):
        """The function a member calls. It takes the connection, a pointer to the result
        for a member that returns a value, then the arguments."""
        function = "krpc_%s_" % self._service
        if cls is not None:
            function += cls + "_"
        if kind in (SETTER, CLASS_SETTER):
            function += "set_"
        return function + name
