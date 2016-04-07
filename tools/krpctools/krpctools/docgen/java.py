from .domain import Domain
from .nodes import *
from ..utils import lower_camel_case
from krpc.utils import snake_case
from krpc.types import ValueType, MessageType, ClassType, EnumType, ListType, DictionaryType, SetType, TupleType

class JavaDomain(Domain):
    name = 'java'
    prettyname = 'Java'
    sphinxname = 'java'
    codeext = 'java'

    type_map = {
        'int32': 'int',
        'int64': 'long',
        'uint32': 'int',
        'uint64': 'long',
        'bytes': 'byte[]',
        'string': 'String',
        'float': 'float',
        'double': 'double',
        'bool': 'boolean'
    }

    boxed_type_map = {
        'int32': 'Integer',
        'int64': 'Long',
        'uint32': 'Integer',
        'uint64': 'Long',
        'bytes': 'Byte[]',
        'string': 'String',
        'float': 'Single',
        'double': 'Double',
        'bool': 'Boolean'
    }

    tuple_types = [
        'Unit', 'Pair', 'Triplet', 'Quartet', 'Quintet',
        'Sextet', 'Septet', 'Octet', 'Ennead', 'Decade'
    ]

    def __init__(self, macros):
        super(JavaDomain, self).__init__(macros)

    def type(self, typ, generic=False):
        if typ == None:
            return 'void'
        elif not generic and isinstance(typ, ValueType):
            return self.type_map[typ.protobuf_type]
        elif generic and isinstance(typ, ValueType):
            return self.boxed_type_map[typ.protobuf_type]
        elif isinstance(typ, MessageType):
            return 'krpc.schema.%s' % typ.protobuf_type
        elif isinstance(typ, ClassType):
            return self.shorten_ref(typ.protobuf_type[6:-1])
        elif isinstance(typ, EnumType):
            return self.shorten_ref(typ.protobuf_type[5:-1])
        elif isinstance(typ, ListType):
            return 'java.util.List<%s>' % self.type(typ.value_type, True)
        elif isinstance(typ, DictionaryType):
            return 'java.util.Map<%s,%s>' % (self.type(typ.key_type, True), self.type(typ.value_type, True))
        elif isinstance(typ, SetType):
            return 'java.util.Set<%s>' % self.type(typ.value_type, True)
        elif isinstance(typ, TupleType):
            name = self.tuple_types[len(typ.value_types)-1]
            return 'org.javatuples.%s<%s>' % (name, ','.join(self.type(typ, True) for typ in typ.value_types))
        else:
            raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def type_description(self, typ):
        if isinstance(typ, ValueType):
            return self.type(typ)
        elif isinstance(typ, MessageType):
            return ':type:`krpc.schema.%s`' % typ.protobuf_type
        elif isinstance(typ, ClassType):
            return ':type:`%s`' % self.type(typ)
        elif isinstance(typ, EnumType):
            return ':class:`%s`' % self.type(typ)
        elif isinstance(typ, ListType):
            return ':class:`java.util.List<%s>`' % self.type(typ.value_type, True)
        elif isinstance(typ, DictionaryType):
            return ':class:`java.util.Map<%s,%s>`' % (self.type(typ.key_type, True), self.type(typ.value_type, True))
        elif isinstance(typ, SetType):
            return ':class:`java.util.Set<%s>`' % self.type(typ.value_type, True)
        elif isinstance(typ, TupleType):
            name = self.tuple_types[len(typ.value_types)-1]
            return ':class:`org.javatuples.%s<%s>`' % (name, ','.join(self.type(typ, True) for typ in typ.value_types))
        else:
            raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def ref(self, obj):
        name = obj.fullname
        if isinstance(obj, Procedure) or isinstance(obj, ClassMethod) or isinstance(obj, ClassStaticMethod):
            parameters = [self.type(p.type) for p in obj.parameters]
            if isinstance(obj, ClassMethod):
                parameters = parameters[1:]
            name = name.split('.')
            name[-1] = lower_camel_case(name[-1])+'('+', '.join(parameters)+')'
            name = '.'.join(name)
        elif isinstance(obj, Property) or isinstance(obj, ClassProperty):
            name = name.split('.')
            name[-1] = 'get'+name[-1]+'()'
            name = '.'.join(name)
        elif isinstance(obj, EnumerationValue):
            name = name.split('.')
            name[-1] = snake_case(name[-1]).upper()
            name = '.'.join(name)
        return self.shorten_ref(name)

    def shorten_ref(self, name):
        # TODO: Drop service name from all members.
        # TODO: This will cause issues if there a a name clash is introduced between services.
        _,_,name = name.partition('.')
        return name

    def see(self, obj):
        if isinstance(obj, Procedure) or isinstance(obj, ClassMethod) or isinstance(obj, ClassStaticMethod) or \
             isinstance(obj, Property) or isinstance(obj, ClassProperty) or isinstance(obj, EnumerationValue):
            prefix = 'meth'
        elif isinstance(obj, Class) or isinstance(obj, Enumeration):
            prefix = 'type'
        else:
            raise RuntimeError(str(obj))
        return ':%s:`%s`' % (prefix, self.ref(obj))
