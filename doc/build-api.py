#!/usr/bin/env python

# Simple script to build python API docs from the generic API docs

import sys
import os
import re

src = sys.argv[1]
dst = sys.argv[2]

_regex_multi_uppercase = re.compile(r'([A-Z]+)([A-Z][a-z0-9])')
_regex_single_uppercase = re.compile(r'([a-z0-9])([A-Z])')
_regex_underscores = re.compile(r'(.)_')

def snake_case(camel_case):
    result = re.sub(_regex_underscores, r'\1__', camel_case)
    result = re.sub(_regex_single_uppercase, r'\1_\2', result)
    return re.sub(_regex_multi_uppercase, r'\1_\2', result).lower()

def snake_case_name(name):
    if '.' in name:
        cls,name = name.split('.')
        return cls+'.'+snake_case(name)
    else:
        return snake_case(name)

def parse_file(path):
    with open(path, 'r') as f:
        lines = []
        for line in f.readlines():
            m = re.match('^\.\. ([a-z]+):: ([A-Za-z\.]+)(.*)$', line)
            if m is not None:
                typ = m.group(1)
                name = m.group(2)
                rest = m.group(3)
                if typ in ['method', 'attribute']:
                    name = snake_case_name(name)
                    line = '.. %s:: %s%s' % (typ, name, rest)
            inlines = [':meth:', ':attr:']
            for inline in inlines:
                if inline in line:
                    def repl(m):
                        return inline+'`'+snake_case_name(m.group(1))+'`'
                    line = re.sub(inline+'`([^`]+)`', repl, line)
            lines.append(line.rstrip())
        return '\n'.join(lines)+'\n'

for dirname,dirnames,filenames in os.walk(src):
    for filename in filenames:
        src_path = os.path.join(dirname, filename)
        dst_path = os.path.join(dst, src_path[len(src)+1:])
        print src_path+' -> '+dst_path
        assert not os.path.exists(dst_path)
        content = parse_file(src_path)
        if not os.path.exists(os.path.dirname(dst_path)):
            os.makedirs(os.path.dirname(dst_path))
        with open(dst_path, 'w') as f:
            f.write(content)
