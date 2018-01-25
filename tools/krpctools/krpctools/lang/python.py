from krpc.types import \
    ValueType, ClassType, EnumerationType, MessageType, \
    TupleType, ListType, SetType, DictionaryType
from .language import Language


class PythonLanguage(Language):

    value_map = {
        'null': 'None',
        'true': 'True',
        'false': 'False'
    }

    def parse_type(self, typ):
        if isinstance(typ, ValueType):
            return typ.python_type.__name__
        elif isinstance(typ, MessageType):
            return 'krpc.schema.KRPC.%s' % typ.python_type.__name__
        elif isinstance(typ, (ClassType, EnumerationType)):
            return self.shorten_ref(
                '%s.%s' % (typ.protobuf_type.service,
                           typ.protobuf_type.name))
        elif isinstance(typ, ListType):
            return 'list'
        elif isinstance(typ, DictionaryType):
            return 'dict'
        elif isinstance(typ, SetType):
            return 'set'
        elif isinstance(typ, TupleType):
            return 'tuple'
        else:
            raise RuntimeError('Unknown type \'%s\'' % str(typ))

    def shorten_ref(self, name):
        name = name.split('.')
        if name[0] == self.module:
            del name[0]
        return '.'.join(name)
