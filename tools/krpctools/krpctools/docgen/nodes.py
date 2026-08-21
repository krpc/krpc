from collections import OrderedDict, defaultdict
from krpc.attributes import Attributes
from ..utils import as_type, decode_default_value


class Appendable:
    def __init__(self):
        self._appended = []

    def append(self, value):
        self._appended.append(value)

    @property
    def appended(self):
        return "\n\n".join(self._appended)


class Service(Appendable):
    # pylint: disable=too-many-arguments,too-many-locals
    def __init__(
        self,
        name,
        procedures,
        classes,
        enumerations,
        exceptions,
        documentation,
        sort,
        types,
        structs=None,
        deprecated=False,
        deprecated_reason="",
    ):
        super().__init__()
        self.types = types
        self.name = name
        self.fullname = name
        self.documentation = documentation
        self.deprecated = deprecated
        self.deprecated_reason = deprecated_reason
        self.cref = "T:%s" % name

        members = []
        cprocedures = defaultdict(dict)
        properties = defaultdict(dict)

        for pname, info in procedures.items():
            del info["id"]

            if "game_scenes" in info:
                info["game_scenes"] = ", ".join(
                    x.replace("_", " ").title() for x in info["game_scenes"]
                )
            else:
                info["game_scenes"] = "All"

            if Attributes.is_a_procedure(pname):
                members.append(Procedure(name, pname, types=types, **info))

            elif Attributes.is_a_property_accessor(pname):
                propname = Attributes.get_property_name(pname)
                procedure = Procedure(name, pname, types=types, **info)
                if Attributes.is_a_property_getter(pname):
                    properties[propname]["getter"] = procedure
                else:
                    properties[propname]["setter"] = procedure

            elif Attributes.is_a_class_member(pname):
                cname = Attributes.get_class_name(pname)
                cprocedures[cname][pname] = info

        for propname, prop in properties.items():
            members.append(Property(name, propname, **prop))

        self.classes = {
            cname: Class(
                name, cname, cprocedures[cname], sort=sort, types=types, **cinfo
            )
            for (cname, cinfo) in classes.items()
        }
        self.enumerations = {
            ename: Enumeration(name, ename, sort=sort, **einfo)
            for (ename, einfo) in enumerations.items()
        }
        self.structs = {
            sname: Struct(name, sname, types=types, **sinfo)
            for (sname, sinfo) in (structs or {}).items()
        }
        self.exceptions = {
            ename: ExceptionNode(name, ename, **einfo)
            for (ename, einfo) in exceptions.items()
        }

        self.members = OrderedDict(
            (member.name, member) for member in sorted(members, key=sort)
        )

    def remove(self, member_name):
        if member_name in self.classes:
            del self.classes[member_name]
        if member_name in self.enumerations:
            del self.enumerations[member_name]
        if member_name in self.structs:
            del self.structs[member_name]
        if member_name in self.exceptions:
            del self.exceptions[member_name]
        del self.members[member_name]


class Class(Appendable):
    def __init__(
        self,
        service_name,
        name,
        procedures,
        documentation,
        sort,
        types,
        deprecated=False,
        deprecated_reason="",
    ):
        super().__init__()
        self.types = types
        self.service_name = service_name
        self.name = name
        self.fullname = service_name + "." + name
        self.documentation = documentation
        self.deprecated = deprecated
        self.deprecated_reason = deprecated_reason
        self.cref = "T:%s.%s" % (service_name, name)

        members = []
        properties = defaultdict(dict)

        for pname, pinfo in procedures.items():
            if "id" in pinfo:
                del pinfo["id"]

            if Attributes.is_a_class_method(pname):
                members.append(
                    ClassMethod(service_name, name, pname, types=types, **pinfo)
                )

            elif Attributes.is_a_class_static_method(pname):
                members.append(
                    ClassStaticMethod(service_name, name, pname, types=types, **pinfo)
                )

            elif Attributes.is_a_class_property_accessor(pname):
                propname = Attributes.get_class_member_name(pname)
                proc = Procedure(service_name, pname, types=types, **pinfo)
                if Attributes.is_a_class_property_getter(pname):
                    properties[propname]["getter"] = proc
                else:
                    properties[propname]["setter"] = proc

        for propname, prop in properties.items():
            members.append(ClassProperty(service_name, name, propname, **prop))

        self.members = OrderedDict(
            (member.name, member) for member in sorted(members, key=sort)
        )


class Parameter(Appendable):
    # pylint: disable=redefined-builtin
    def __init__(
        self,
        name,
        type,
        documentation,
        types,
        procedure,
        default_value=None,
        nullable=False,
    ):
        super().__init__()
        self.types = types
        self.name = name
        self.type = as_type(self.types, type)
        self.has_default_value = default_value is not None
        if default_value is not None:
            location = "%s parameter %s" % (procedure, name)
            default_value = decode_default_value(default_value, self.type, location)
        self.default_value = default_value
        self.nullable = nullable
        self.documentation = documentation


class Procedure(Appendable):
    member_type = "procedure"

    def __init__(
        self,
        service_name,
        name,
        parameters,
        documentation,
        types,
        return_type=None,
        return_is_nullable=False,
        game_scenes=None,
        deprecated=False,
        deprecated_reason="",
    ):
        super().__init__()
        self.types = types
        self.service_name = service_name
        self.name = name
        self.fullname = service_name + "." + name
        if return_type is not None:
            self.return_type = as_type(self.types, return_type)
        else:
            self.return_type = None
        self.return_is_nullable = return_is_nullable
        self.parameters = [
            Parameter(
                documentation=documentation,
                types=types,
                procedure=self.fullname,
                **info
            )
            for info in parameters
        ]
        self.game_scenes = game_scenes
        self.documentation = documentation
        self.deprecated = deprecated
        self.deprecated_reason = deprecated_reason
        self.cref = "M:%s.%s" % (service_name, name)


class Property(Appendable):
    member_type = "property"

    def __init__(self, service_name, name, getter=None, setter=None):
        super().__init__()
        self.service_name = service_name
        self.name = name
        self.fullname = service_name + "." + name
        if getter is not None:
            self.type = getter.return_type
            self.game_scenes = getter.game_scenes
            self.documentation = getter.documentation
            self.deprecated = getter.deprecated
            self.deprecated_reason = getter.deprecated_reason
        else:
            self.type = setter.parameters[0].type
            self.game_scenes = setter.game_scenes
            self.documentation = setter.documentation
            self.deprecated = setter.deprecated
            self.deprecated_reason = setter.deprecated_reason
        self.getter = getter
        self.setter = setter
        self.cref = "M:%s.%s" % (service_name, name)


class ClassMethod(Appendable):
    # pylint: disable=too-many-instance-attributes,too-many-arguments
    member_type = "class_method"

    def __init__(
        self,
        service_name,
        class_name,
        name,
        parameters,
        documentation,
        types,
        return_type=None,
        return_is_nullable=False,
        game_scenes=None,
        deprecated=False,
        deprecated_reason="",
    ):
        super().__init__()
        self.types = types
        name = Attributes.get_class_member_name(name)
        self.service_name = service_name
        self.class_name = class_name
        self.name = name
        self.fullname = service_name + "." + class_name + "." + name
        if return_type is not None:
            self.return_type = as_type(self.types, return_type)
        else:
            self.return_type = None
        self.return_is_nullable = return_is_nullable
        self.parameters = [
            Parameter(
                documentation=documentation,
                types=types,
                procedure=self.fullname,
                **info
            )
            for info in parameters
        ]
        self.game_scenes = game_scenes
        self.documentation = documentation
        self.deprecated = deprecated
        self.deprecated_reason = deprecated_reason
        self.cref = "M:%s.%s.%s" % (service_name, class_name, name)


class ClassStaticMethod(Appendable):
    # pylint: disable=too-many-instance-attributes,too-many-arguments
    member_type = "class_static_method"

    def __init__(
        self,
        service_name,
        class_name,
        name,
        parameters,
        documentation,
        types,
        return_type=None,
        return_is_nullable=False,
        game_scenes=None,
        deprecated=False,
        deprecated_reason="",
    ):
        super().__init__()
        self.types = types
        name = Attributes.get_class_member_name(name)
        self.service_name = service_name
        self.class_name = class_name
        self.name = name
        self.fullname = service_name + "." + class_name + "." + name
        if return_type is not None:
            self.return_type = as_type(self.types, return_type)
        else:
            self.return_type = None
        self.return_is_nullable = return_is_nullable
        self.parameters = [
            Parameter(
                documentation=documentation,
                types=types,
                procedure=self.fullname,
                **info
            )
            for info in parameters
        ]
        self.game_scenes = game_scenes
        self.documentation = documentation
        self.deprecated = deprecated
        self.deprecated_reason = deprecated_reason
        self.cref = "M:%s.%s.%s" % (service_name, class_name, name)


class ClassProperty(Appendable):
    member_type = "class_property"

    def __init__(self, service_name, class_name, name, getter=None, setter=None):
        super().__init__()
        self.service_name = service_name
        self.class_name = class_name
        if getter is not None:
            self.type = getter.return_type
            self.game_scenes = getter.game_scenes
            self.documentation = getter.documentation
            self.deprecated = getter.deprecated
            self.deprecated_reason = getter.deprecated_reason
        else:
            self.type = setter.parameters[1].type
            self.game_scenes = setter.game_scenes
            self.documentation = setter.documentation
            self.deprecated = setter.deprecated
            self.deprecated_reason = setter.deprecated_reason
        self.name = name
        self.fullname = service_name + "." + class_name + "." + name
        self.getter = getter
        self.setter = setter
        self.cref = "M:%s.%s.%s" % (service_name, class_name, name)


class Enumeration(Appendable):
    def __init__(
        self,
        service_name,
        name,
        values,
        documentation,
        sort,
        deprecated=False,
        deprecated_reason="",
    ):
        super().__init__()
        self.service_name = service_name
        self.name = name
        self.fullname = service_name + "." + name
        values = (EnumerationValue(service_name, name, **value) for value in values)
        self.values = OrderedDict((v.name, v) for v in sorted(values, key=sort))
        self.documentation = documentation
        self.deprecated = deprecated
        self.deprecated_reason = deprecated_reason
        self.cref = "T:%s.%s" % (service_name, name)


class EnumerationValue(Appendable):
    def __init__(
        self,
        service_name,
        enum_name,
        name,
        value,
        documentation,
        deprecated=False,
        deprecated_reason="",
    ):
        super().__init__()
        self.service_name = service_name
        self.enum_name = enum_name
        self.name = name
        self.fullname = service_name + "." + enum_name + "." + name
        self.value = value
        self.documentation = documentation
        self.deprecated = deprecated
        self.deprecated_reason = deprecated_reason
        self.cref = "M:%s.%s.%s" % (service_name, enum_name, name)


class Struct(Appendable):
    def __init__(
        self,
        service_name,
        name,
        fields,
        documentation,
        types,
        deprecated=False,
        deprecated_reason="",
    ):
        super().__init__()
        self.types = types
        self.service_name = service_name
        self.name = name
        self.fullname = service_name + "." + name
        # The fields keep the order the structure declares them in, which is the order their
        # values are encoded in, rather than being sorted like the members of a class
        self.fields = OrderedDict(
            (field["name"], StructField(service_name, name, types=types, **field))
            for field in fields
        )
        self.documentation = documentation
        self.deprecated = deprecated
        self.deprecated_reason = deprecated_reason
        self.cref = "T:%s.%s" % (service_name, name)


class StructField(Appendable):
    # pylint: disable=redefined-builtin
    def __init__(
        self,
        service_name,
        struct_name,
        name,
        type,
        documentation,
        types,
        deprecated=False,
        deprecated_reason="",
    ):
        super().__init__()
        self.types = types
        self.service_name = service_name
        self.struct_name = struct_name
        self.name = name
        self.fullname = service_name + "." + struct_name + "." + name
        self.type = as_type(self.types, type)
        self.documentation = documentation
        self.deprecated = deprecated
        self.deprecated_reason = deprecated_reason
        self.cref = "M:%s.%s.%s" % (service_name, struct_name, name)


class ExceptionNode(Appendable):
    def __init__(
        self, service_name, name, documentation, deprecated=False, deprecated_reason=""
    ):
        super().__init__()
        self.service_name = service_name
        self.name = name
        self.fullname = service_name + "." + name
        self.documentation = documentation
        self.deprecated = deprecated
        self.deprecated_reason = deprecated_reason
        self.cref = "T:%s.%s" % (service_name, name)
