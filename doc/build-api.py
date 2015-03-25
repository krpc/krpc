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
            m = re.match('^(\s+)\.\. ([a-z]+):: (.+)$', line)
            if m is not None:
                indent = m.group(1)
                typ = m.group(2)
                signature = m.group(3)
                if typ == 'attribute' or typ == 'data':
                    line = '%s.. %s:: %s' % (indent, typ, snake_case_name(signature))
                if typ == 'method':
                    m = re.match('^(.+) \((.*)\)$', signature)
                    name = m.group(1)
                    params = m.group(2)
                    name = snake_case_name(name)
                    params = params.split(',')
                    for i in range(len(params)):
                        param = params[i]
                        if '=' in param:
                            param,default = param.split('=')
                            param = param.strip()
                            default = default.strip()
                            if '.' in default:
                                default = snake_case_name(default)
                            param = snake_case(param)+' = '+default
                        else:
                            param = snake_case(param)
                        params[i] = param
                    line = '%s.. %s:: %s (%s)' % (indent, typ, name, ', '.join(params))
            inlines = [':meth:', ':attr:']
            for inline in inlines:
                if inline in line:
                    def repl(m):
                        return inline+'`'+snake_case_name(m.group(1))+'`'
                    line = re.sub(inline+'`([^`]+)`', repl, line)
            def repl(m):
                return ':param '+m.group(1)+' '+snake_case_name(m.group(2))+':'
            line = re.sub(':param ([^ ]+) (.+):', repl, line)
            def repl2(m):
                return m.group(1)+'*'+snake_case(m.group(2))+'*'+m.group(3)
            line = re.sub('([^\*])\*([^\*]+)\*([^\*])', repl2, line)
            line = line.replace('``null``', '``None``')
            line = line.replace('``true``', '``True``')
            line = line.replace('``false``', '``False``')
            line = line.replace('``string``', '``str``')
            line = line.replace(':class:`Dictionary`', '``dict``')
            line = line.replace(':class:`List`', '``list``')
            lines.append(line.rstrip())
        return '\n'.join(lines)+'\n'

for dirname,dirnames,filenames in os.walk(src):
    for filename in filenames:
        src_path = os.path.join(dirname, filename)
        dst_path = os.path.join(dst, src_path[len(src)+1:])
        try:
            content = parse_file(src_path)
        except IOError:
            continue

        # Skip if already up to date
        if os.path.exists(dst_path):
            try:
                old_content = open(dst_path, 'r').read()
                if content == old_content:
                    continue
            except IOError:
                pass

        # Update
        print src_path+' -> '+dst_path
        if not os.path.exists(os.path.dirname(dst_path)):
            os.makedirs(os.path.dirname(dst_path))
        with open(dst_path, 'w') as f:
            f.write(content)
