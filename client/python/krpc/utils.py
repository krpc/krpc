import re

_REGEX_MULTI_UPPERCASE = re.compile(r'([A-Z]+)([A-Z][a-z0-9])')
_REGEX_SINGLE_UPPERCASE = re.compile(r'([a-z0-9])([A-Z])')
_REGEX_UNDERSCORES = re.compile(r'(.)_')


def snake_case(camel_case):
    """ Convert camel case to snake case, e.g. GetServices -> get_services """
    result = re.sub(_REGEX_UNDERSCORES, r'\1__', camel_case)
    result = re.sub(_REGEX_SINGLE_UPPERCASE, r'\1_\2', result)
    return re.sub(_REGEX_MULTI_UPPERCASE, r'\1_\2', result).lower()

def split_type_string(type_string):
    parts = []
    while type_string is not None:
        part, type_string = _split_type_string(type_string)
        parts.append(part)
    return parts

def _split_type_string(typ):
    """ Given a string, extract a substring up to the first comma. Parses parentheses.
        Multiple calls can be used to separate a string by commas. """
    if typ is None:
        raise ValueError
    result = ''
    level = 0
    for x in typ:
        if level == 0 and x == ',':
            break
        if x == '(':
            level += 1
        if x == ')':
            level -= 1
        result += x
    if level != 0:
        raise ValueError
    if result == typ:
        return result, None
    if typ[len(result)] != ',':
        raise ValueError
    return result, typ[len(result) + 1:]
