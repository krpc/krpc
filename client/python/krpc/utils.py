import re

_regex_multi_uppercase = re.compile(r'([A-Z]+)([A-Z][a-z0-9])')
_regex_single_uppercase = re.compile(r'([a-z0-9])([A-Z])')
_regex_underscores = re.compile(r'(.)_')

def snake_case(camel_case):
    """ Convert camel case to snake case, e.g. GetServices -> get_services """
    result = re.sub(_regex_underscores, r'\1__', camel_case)
    result = re.sub(_regex_single_uppercase, r'\1_\2', result)
    return re.sub(_regex_multi_uppercase, r'\1_\2', result).lower()
