from krpc.utils import snake_case

from ..lang.http import EXCLUDED_CLASSES, HttpLanguage, kebab_case
from .domain import Domain
from .nodes import (
    Class,
    ClassMethod,
    ClassProperty,
    ClassStaticMethod,
    Enumeration,
    EnumerationValue,
    ExceptionNode,
    Procedure,
    Property,
    Service,
    Struct,
    StructField,
)


class HttpDomain(Domain[HttpLanguage]):
    """The HTTP server's paths, documented with sphinxcontrib-httpdomain. A class, enumeration,
    structure or exception has no directive in that domain, so it is a label and the text
    below it."""

    name = "http"
    prettyname = "HTTP"
    sphinxname = "http"
    highlight = "json"
    # The API pages carry no examples for HTTP
    codeext = None
    language = HttpLanguage()

    def currentmodule(self, name):
        super().currentmodule(name)
        return ""

    @staticmethod
    def class_supported(node):
        """Whether the members of a class can have paths"""
        return (node.service_name, node.name) not in EXCLUDED_CLASSES

    def supported(self, member):
        """Whether the member has a path, which it does if JSON can carry its values"""
        if isinstance(member, (Property, ClassProperty)):
            return self.language.supported(member.type)
        if member.return_type is not None and not self.language.supported(
            member.return_type
        ):
            return False
        return all(self.language.supported(p.type) for p in member.parameters)

    @staticmethod
    def path(member):
        """The path of a member, with (id) in place of the object of a class member"""
        service = "/" + kebab_case(member.service_name)
        if isinstance(member, (Procedure, Property)):
            return "%s/%s" % (service, kebab_case(member.name))
        if isinstance(member, ClassStaticMethod):
            return "%s/%s/%s" % (
                service,
                kebab_case(member.class_name),
                kebab_case(member.name),
            )
        return "%s/%s/(id)/%s" % (
            service,
            kebab_case(member.class_name),
            kebab_case(member.name),
        )

    @staticmethod
    def label(obj):
        return "http-%s" % obj.fullname

    def see(self, obj):
        if isinstance(obj, Class) and not self.class_supported(obj):
            return "``%s``" % self.shorten_ref(obj.fullname)
        if isinstance(obj, (Service, Class, Enumeration, Struct, ExceptionNode)):
            return ":ref:`%s <%s>`" % (self.shorten_ref(obj.fullname), self.label(obj))
        if isinstance(obj, (Procedure, ClassMethod, ClassStaticMethod)):
            if not self.supported(obj):
                return "``%s``" % self.shorten_ref(obj.fullname)
            return ":http:post:`%s`" % self.path(obj)
        if isinstance(obj, (Property, ClassProperty)):
            if not self.supported(obj):
                return "``%s``" % self.shorten_ref(obj.fullname)
            if obj.getter is None:
                return ":http:put:`%s`" % self.path(obj)
            return ":http:get:`%s`" % self.path(obj)
        if isinstance(obj, EnumerationValue):
            return '``"%s"``' % snake_case(obj.name)
        if isinstance(obj, StructField):
            return "``%s``" % snake_case(obj.name)
        raise RuntimeError(str(obj))

    def raised_as(self, exception):
        return (
            'Returned as an error whose ``service`` and ``name`` are ``"%s"`` and ``"%s"``.'
            % (exception.service_name, exception.name)
        )

    @staticmethod
    def paramref(name):
        return Domain.paramref(snake_case(name))
