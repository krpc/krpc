import re

_CAMEL_CASE_REGEX = re.compile(r'([a-z]+|[A-Z][^A-Z]*)')

def lower_camel_case(string):
    """ Convert from CamelCase to lowerCamelCase """
    parts = re.findall(_CAMEL_CASE_REGEX, string)
    parts[0] = parts[0].lower()
    return ''.join(parts)

def upper_camel_case(string):
    """ Convert from lowerCamelCase to CamelCase """
    return string[0].upper() + string[1:]

def indent(string, width=3):
    """ Indent the lines in the given string with width spaces """
    lines = string.split('\n')
    for i, line in enumerate(lines):
        if len(line.strip()) > 0:
            lines[i] = (' '*width) + line
    return '\n'.join(lines).strip('\n')

def single_line(string):
    """ Convert the given string into a single line """
    return ' '.join(line.strip() for line in string.split('\n'))
