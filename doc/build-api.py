#!/usr/bin/env python

# Build Python API docs from generic API docs

import sys
import os
import re

src = sys.argv[1]
dst = sys.argv[2]

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

def convert_type(name):
    typs = {
       'double': 'float',
       'int32': 'int',
       'Dictionary': 'dict',
       'List': 'list'
    }
    if name in typs:
        return typs[name]
    else:
        return name

def process_directive(line):
    global in_class
    m = re.match('^(\s*)\.\. ([a-z]+):: (.+)$', line)
    if m is not None:
        indent = m.group(1)
        typ = m.group(2)
        signature = m.group(3)
        if typ == 'attribute' or typ == 'data':
            line = '%s.. %s:: %s' % (indent, typ, snake_case(signature))
        if typ == 'method':
            m = re.match('^(.+) \((.*)\)$', signature)
            name = m.group(1)
            params = m.group(2)
            name = snake_case(name)
            params = params.split(',')
            optional = False
            for i in range(len(params)):
                param = params[i].strip()
                if '=' in param:
                    optional = True
                    param,default = param.split('=')
                    param = param.strip()
                    default = default.strip()
                    param = snake_case(param)+'='+snake_case(default)
                    if not param.startswith('[') or not param.endswith(']'):
                        raise RuntimeError('Optional parameter not enclosed in [ ... ]')
                else:
                    param = snake_case(param)
                params[i] = param
            line = '%s.. %s:: %s (%s)' % (indent, typ, name, ', '.join(params))
    return line

def process_inline(line):
    for inline in [':meth:', ':attr:']:
       if inline in line:
            def repl(m):
                return inline+'`'+snake_case(m.group(1))+'`'
            line = re.sub(inline+'`([^`]+)`', repl, line)
    return line

def process_inline_types_and_values(line):
    replacements = {
        '``null``': '``None``',
        '``true``': '``True``',
        '``false``': '``False``',
        '``string``': '``str``',
        '``double``': '``float``',
        '``int32``': '``int``',
        ':class:`Dictionary`': '``dict``',
        ':class:`List`': '``list``'
    }
    for x,y in replacements.items():
        line = line.replace(x, y)
    return line

def process_parameters(line):
    def repl(m):
        return ':param '+convert_type(m.group(1))+' '+snake_case(m.group(2))+':'
    return re.sub(':param ([^ ]+) (.+):', repl, line)

def process_inline_parameters(line):
    def repl(m):
        return m.group(1)+'*'+snake_case(m.group(2))+'*'+m.group(3)
    return re.sub('([^\*])\*([^\*]+)\*([^\*])', repl, line)

def process_file(path):
    with open(path, 'r') as f:
        lines = []
        for lineno,line in enumerate(f.readlines()):
            try:
                line = process_directive(line)
                line = process_inline(line)
                line = process_parameters(line)
                line = process_inline_parameters(line)
                line = process_inline_types_and_values(line)
                lines.append(line.rstrip())
            except Exception, e:
                print 'Error on line', lineno, 'in', path
                print line
                print e
                exit(1)
        return '\n'.join(lines)+'\n'

for dirname,dirnames,filenames in os.walk(src):
    for filename in filenames:
        src_path = os.path.join(dirname, filename)
        dst_path = os.path.join(dst, src_path[len(src)+1:])
        try:
            content = process_file(src_path)
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
