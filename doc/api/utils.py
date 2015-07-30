import re

_regex_multi_uppercase = re.compile(r'([A-Z]+)([A-Z][a-z0-9])')
_regex_single_uppercase = re.compile(r'([a-z0-9])([A-Z])')
_regex_underscores = re.compile(r'(.)_')

def snake_case(name):
    if '.' in name:
        cls,name = name.split('.')
        return cls+'.'+snake_case(name)
    else:
        result = re.sub(_regex_underscores, r'\1__', name)
        result = re.sub(_regex_single_uppercase, r'\1_\2', result)
        return re.sub(_regex_multi_uppercase, r'\1_\2', result).lower()

def indent(lines, level):
    result = []
    for line in lines:
        if line:
            result.append((' '*level)+line)
        else:
            result.append(line)
    return result
