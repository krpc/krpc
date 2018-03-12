from krpc.schema.KRPC_pb2 import Type
from krpc.types import \
    ValueType, ClassType, EnumerationType, MessageType, \
    TupleType, ListType, SetType, DictionaryType
from krpc.utils import snake_case
from ..utils import lower_camel_case
from .language import Language


class JavaLanguage(Language):

    keywords = set([
        'abstract', 'continue', 'for', 'new', 'switch', 'assert', 'default',
        'goto', 'package', 'synchronized', 'boolean', 'do', 'if', 'private',
        'this', 'break', 'double', 'implements', 'protected', 'throw', 'byte',
        'else', 'import', 'public', 'throws', 'case', 'enum', 'instanceof',
        'return', 'transient', 'catch', 'extends', 'int', 'short', 'try',
        'char', 'final', 'interface', 'static', 'void', 'class', 'finally',
        'long', 'strictfp', 'volatile', 'const', 'float', 'native', 'super',
        'while', 'wait'
    ])

    tuple_types = [
        'Unit', 'Pair', 'Triplet', 'Quartet', 'Quintet', 'Sextet', 'Septet',
        'Octet', 'Ennead', 'Decade'
    ]

    type_map = {
        Type.DOUBLE: 'double',
        Type.FLOAT: 'float',
        Type.SINT32: 'int',
        Type.SINT64: 'long',
        Type.UINT32: 'int',
        Type.UINT64: 'long',
        Type.BOOL: 'boolean',
        Type.STRING: 'String',
        Type.BYTES: 'byte[]'
    }

    type_map_classes = {
        Type.DOUBLE: 'Double',
        Type.FLOAT: 'Float',
        Type.SINT32: 'Integer',
        Type.SINT64: 'Long',
        Type.UINT32: 'Integer',
        Type.UINT64: 'Long',
        Type.BOOL: 'Boolean',
        Type.STRING: 'String',
        Type.BYTES: 'byte[]'
    }

    @classmethod
    def get_tuple_class_name(cls, value_types):
        return cls.tuple_types[len(value_types)-1]

    def parse_name(self, name):
        return super(JavaLanguage, self).parse_name(lower_camel_case(name))

    @staticmethod
    def parse_const_name(name):
        return snake_case(name).upper()

    def parse_type(self, typ):
        return self._parse_type(typ)

    def _parse_type(self, typ, in_collection=False):
        if typ is None:
            if in_collection:
                raise RuntimeError('void type not allowed in collection type')
            return 'void'
        elif not in_collection and isinstance(typ, ValueType):
            return self.type_map[typ.protobuf_type.code]
        elif isinstance(typ, ValueType):
            return self.type_map_classes[typ.protobuf_type.code]
        elif (isinstance(typ, MessageType) and
              typ.protobuf_type.code == Type.EVENT):
            return 'krpc.client.Event'
        elif isinstance(typ, MessageType):
            return 'krpc.schema.KRPC.%s' % typ.python_type.__name__
        elif isinstance(typ, (ClassType, EnumerationType)):
            return 'krpc.client.services.%s.%s' % \
                (typ.protobuf_type.service, typ.protobuf_type.name)
        elif isinstance(typ, TupleType):
            name = self.get_tuple_class_name(typ.value_types)
            return 'org.javatuples.'+name+'<%s>' % \
                (','.join(self._parse_type(t, True) for t in typ.value_types))
        elif isinstance(typ, ListType):
            return 'java.util.List<%s>' % \
                self._parse_type(typ.value_type, True)
        elif isinstance(typ, SetType):
            return 'java.util.Set<%s>' % \
                self._parse_type(typ.value_type, True)
        elif isinstance(typ, DictionaryType):
            return 'java.util.Map<%s,%s>' % \
                (self._parse_type(typ.key_type, True),
                 self._parse_type(typ.value_type, True))
        raise RuntimeError('Unknown type \'%s\'' % str(typ))
