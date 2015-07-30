#!/usr/bin/env python

import json
import krpc
from krpc.types import Types
from krpc.decoder import Decoder

conn = krpc.connect(name='get-api.py')
types = Types()

info = {}
for service in conn.krpc.get_services().services:
    service_info = {
        'procedures': {},
        'classes': {},
        'enumerations': {},
        'documentation': service.documentation
    }
    info[service.name] = service_info
    for procedure in service.procedures:
        procedure_info = {
            'parameters': [],
            'return_type': procedure.return_type,
            'attributes': [str(x) for x in procedure.attributes],
            'documentation': procedure.documentation
        }
        service_info['procedures'][procedure.name] = procedure_info
        for parameter in procedure.parameters:

            default_argument = None
            if parameter.HasField('default_argument'):
                typ = types.as_type(parameter.type)
                default_argument = Decoder.decode(parameter.default_argument, typ)

            parameter_info = {
                'name': parameter.name,
                'type': parameter.type,
                'default_argument': default_argument
            }
            procedure_info['parameters'].append(parameter_info)
    for cls in service.classes:
        cls_info = {
            'documentation': cls.documentation
        }
        service_info['classes'][cls.name] = cls_info
    for enum in service.enumerations:
        enum_info = {
            'values': [
                {'name': value.name, 'value': value.value, 'documentation': value.documentation}
                for value in enum.values],
            'documentation': enum.documentation
        }
        service_info['enumerations'][enum.name] = enum_info

print json.dumps(info, sort_keys=True, indent=4, separators=(',', ': '))
