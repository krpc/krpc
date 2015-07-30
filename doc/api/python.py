from domain import DefaultDomain

class PythonAPI(DefaultDomain):

    name = 'py'

    type_map = {
        'double': 'float',
        'float': 'float',
        'int32': 'int',
        'int64': 'long',
        'uint32': 'int',
        'uint64': 'long',
        'bool': 'bool',
        'string': 'string',
        'bytes': 'bytes',
        'KRPC.Tuple': 'tuple',
        'KRPC.Dictionary': 'dict',
        'KRPC.List': 'list',
        'KRPC.Status': 'KRPC.Status',
        'KRPC.Services': 'KRPC.Services'
    }

    value_map = {
        'null': 'None',
        'true': 'True',
        'false': 'False'
    }
