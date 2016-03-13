import re

_camel_case_regex = re.compile(r'([a-z]+|[A-Z][^A-Z]*)')

def lower_camel_case(camel_case):
    """ Convert from CamelCase to lowerCamelCase """
    parts = re.findall(_camel_case_regex, camel_case)
    parts[0] = parts[0].lower()
    return ''.join(parts)

def upper_camel_case(lower_camel_case):
    """ Convert from lowerCamelCase to CamelCase """
    return lower_camel_case[0].upper() + lower_camel_case[1:]

def indent(s, width=3):
    """ Indent the lines in the given string with width spaces """
    lines = s.split('\n')
    for i in range(len(lines)):
        if len(lines[i].strip()) > 0:
            lines[i] = (' '*width) + lines[i]
    return '\n'.join(lines).strip('\n')

def single_line(s):
    """ Convert the given string into a single line """
    return ' '.join(line.strip() for line in s.split('\n'))
