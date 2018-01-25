from krpc.types import \
    ValueType, ClassType, EnumerationType, MessageType, \
    TupleType, ListType, SetType, DictionaryType
from krpc.utils import snake_case
from .domain import Domain
from .nodes import \
    Procedure, Property, Class, ClassMethod, ClassStaticMethod, \
    ClassProperty, Enumeration, EnumerationValue
from ..utils import lower_camel_case
from ..lang.java import JavaLanguage


class JavaDomain(Domain):
    name = 'java'
    prettyname = 'Java'
    sphinxname = 'java'
    highlight = 'java'
    codeext = 'java'
    language = JavaLanguage()

    def currentmodule(self, name):
        super(JavaDomain, self).currentmodule(name)
        return '.. package:: krpc.client.services.%s' % name

    def method_name(self, name):
        if lower_camel_case(name) in self.language.keywords:
            return '%s_' % name
        return name

    def type(self, typ):
        return self._type(typ)

    def _type(self, typ, generic=False):
        if typ is None:
            return 'void'
        elif not generic and isinstance(typ, ValueType):
            return self.language.type_map[typ.protobuf_type.code]
        elif generic and isinstance(typ, ValueType):
            return self.language.type_map_classes[typ.protobuf_type.code]
        elif isinstance(typ, MessageType):
            return 'krpc.schema.KRPC.%s' % typ.python_type.__name__
        elif isinstance(typ, (ClassType, EnumerationType)):
            return self.shorten_ref(
                '%s.%s' % (typ.protobuf_type.service, typ.protobuf_type.name))
        elif isinstance(typ, ListType):
            return 'java.util.List<%s>' % self._type(typ.value_type, True)
        elif isinstance(typ, DictionaryType):
            return 'java.util.Map<%s,%s>' % \
                (self._type(typ.key_type, True),
                 self._type(typ.value_type, True))
        elif isinstance(typ, SetType):
            return 'java.util.Set<%s>' % self._type(typ.value_type, True)
        elif isinstance(typ, TupleType):
            name = self.language.tuple_types[len(typ.value_types)-1]
            return 'org.javatuples.%s<%s>' % \
                (name, ','.join(self._type(typ, True)
                                for typ in typ.value_types))
        else:
            raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def type_description(self, typ):
        if isinstance(typ, ValueType):
            return self._type(typ)
        elif isinstance(typ, MessageType):
            return ':type:`%s`' % self._type(typ)
        elif isinstance(typ, ClassType):
            return ':type:`%s`' % self.type(typ)
        elif isinstance(typ, EnumerationType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, ListType):
            return ':class:`java.util.List<%s>`' % \
                self.type(typ.value_type, True)
        elif isinstance(typ, DictionaryType):
            return ':class:`java.util.Map<%s,%s>`' % \
                (self.type(typ.key_type, True),
                 self.type(typ.value_type, True))
        elif isinstance(typ, SetType):
            return ':class:`java.util.Set<%s>`' % \
                self.type(typ.value_type, True)
        elif isinstance(typ, TupleType):
            name = self.tuple_types[len(typ.value_types)-1]
            return ':class:`org.javatuples.%s<%s>`' % \
                (name, ','.join(self.type(typ, True)
                                for typ in typ.value_types))
        else:
            raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def ref(self, obj):
        name = obj.fullname
        if isinstance(obj, (Procedure, ClassMethod, ClassStaticMethod)):
            parameters = [self.type(p.type) for p in obj.parameters]
            if isinstance(obj, ClassMethod):
                parameters = parameters[1:]
            name = name.split('.')
            name[-1] = lower_camel_case(name[-1])+'('+', '.join(parameters)+')'
            name = '.'.join(name)
        elif isinstance(obj, (Property, ClassProperty)):
            name = name.split('.')
            name[-1] = 'get'+name[-1]+'()'
            name = '.'.join(name)
        elif isinstance(obj, EnumerationValue):
            name = name.split('.')
            name[-1] = snake_case(name[-1]).upper()
            name = '.'.join(name)
        return self.shorten_ref(name)

    def see(self, obj):
        if any(isinstance(obj, cls) for cls in
               (Procedure, ClassMethod, ClassStaticMethod, Property,
                ClassProperty, EnumerationValue)):
            prefix = 'meth'
        elif any(isinstance(obj, cls) for cls in (Class, Enumeration)):
            prefix = 'type'
        else:
            raise RuntimeError(str(obj))
        return ':%s:`%s`' % (prefix, self.ref(obj))
