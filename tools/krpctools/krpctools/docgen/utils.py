import re

_regex_multi_uppercase = re.compile(r'([A-Z]+)([A-Z][a-z0-9])')
_regex_single_uppercase = re.compile(r'([a-z0-9])([A-Z])')
_regex_underscores = re.compile(r'(.)_')
_camel_case_regex = re.compile(r'([a-z]+|[A-Z][^A-Z]*)')

def snakecase(camel_case):
    """ Convert camel case to snake case, e.g. GetServices -> get_services """
    result = re.sub(_regex_underscores, r'\1__', camel_case)
    result = re.sub(_regex_single_uppercase, r'\1_\2', result)
    return re.sub(_regex_multi_uppercase, r'\1_\2', result).lower()

def lower_camelcase(camel_case):
    parts = re.findall(_camel_case_regex, camel_case)
    parts[0] = parts[0].lower()
    return ''.join(parts)

def indent(s, width=3):
    lines = s.split('\n')
    for i in range(len(lines)):
        if len(lines[i].strip()) > 0:
            lines[i] = (' '*width) + lines[i]
    return '\n'.join(lines).strip('\n')

def singleline(s):
    return ' '.join(line.strip() for line in s.split('\n'))

_services_lookup = None

def lookup_cref(cref, services):
    global _services_lookup
    if _services_lookup is None:
        objs = []
        for service in services.values():
            objs.append(service)
            objs.extend(service.members.values())
            objs.extend(service.classes.values())
            objs.extend(service.enumerations.values())
            for cls in service.classes.values():
                objs.extend(cls.members.values())
            for enumeration in service.enumerations.values():
                objs.extend(enumeration.values.values())
                for value in enumeration.values.values():
                    objs.append(value)
        _services_lookup = dict([(x.cref, x) for x in objs])
    return _services_lookup[cref]
