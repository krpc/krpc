import re

_REGEX_MULTI_UPPERCASE = re.compile(r'([A-Z]+)([A-Z][a-z0-9])')
_REGEX_SINGLE_UPPERCASE = re.compile(r'([a-z0-9])([A-Z])')
_REGEX_UNDERSCORES = re.compile(r'(.)_')

def snake_case(camel_case):
    """ Convert camel case to snake case, e.g. GetServices -> get_services """
    result = re.sub(_REGEX_UNDERSCORES, r'\1__', camel_case)
    result = re.sub(_REGEX_SINGLE_UPPERCASE, r'\1_\2', result)
    return re.sub(_REGEX_MULTI_UPPERCASE, r'\1_\2', result).lower()
