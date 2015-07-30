from domain import DefaultDomain

class LuaAPI(DefaultDomain):

    name = 'lua'

    type_map = {
        'double': 'number',
        'float': 'number',
        'int32': 'number',
        'int64': 'number',
        'uint32': 'number',
        'uint64': 'number',
        'bool': 'boolean',
        'string': 'string',
        'bytes': 'string',
        'KRPC.Tuple': 'Tuple',
        'KRPC.Dictionary': 'Map',
        'KRPC.List': 'List',
        'KRPC.Status': 'KRPC.Status',
        'KRPC.Services': 'KRPC.Services'
    }

    value_map = {
        'null': 'nil',
        'true': 'True',
        'false': 'False'
    }
