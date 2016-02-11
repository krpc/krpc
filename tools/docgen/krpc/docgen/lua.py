from krpc.docgen.python import PythonDomain
from krpc.docgen.utils import snakecase
from krpc.types import ValueType, ClassType, EnumType, ListType, DictionaryType, SetType, TupleType

class LuaDomain(PythonDomain):

    name = 'lua'
    prettyname = 'Lua'
    sphinxname = 'lua'
    codeext = 'lua'

    type_map = {
        'double': 'number',
        'float': 'number',
        'int32': 'number',
        'int64': 'number',
        'uint32': 'number',
        'uint64': 'number',
        'bool': 'boolean',
        'string': 'string',
        'bytes': 'string'
    }

    value_map = {
        'null': 'nil',
        'true': 'True',
        'false': 'False'
    }

    def __init__(self, macros):
        self._currentmodule = None
        self.macros = macros

    def type(self, typ):
        if isinstance(typ, ValueType):
            return self.type_map[typ.protobuf_type]
        elif isinstance(typ, ListType):
            return 'List'
        elif isinstance(typ, DictionaryType):
            return 'Map'
        elif isinstance(typ, SetType):
            raise RuntimeError('Not supported')
        elif isinstance(typ, TupleType):
            return 'Tuple'
        else:
            super(LuaDomain, self).type(typ)
