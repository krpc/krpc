#!/usr/bin/env python

# Check API docs against the service definition

import sys
import os
import re
import yaml
import copy

src = sys.argv[1]
service_definition = sys.argv[2]

if not os.path.exists(service_definition):
    print 'WARNING: %s does not exist, skipping' % service_definition
    exit(0)

services = None
with open(service_definition, 'r') as f:
    services = yaml.load(f)

not_documented = copy.deepcopy(services['SpaceCenter'])

def check_parameters(name, params):
    num_params = 0
    if 'parameters' in not_documented[name]:
        num_params = len(not_documented[name]['parameters'])
    if len(params) != num_params:
        raise RuntimeError('%s wrong number of parameters; expected %d got %d' % (name, num_params, len(params)))
    if num_params > 0:
        for p,x in sorted(not_documented[name]['parameters'].items(), key=lambda (p,x): x['position']):
            i = x['position']
            if p != params[i]:
                raise RuntimeError('%s wrong parameter name; expected %s got %s' % (name, p, params[i]))

def documented_procedure(name, params):
    check_parameters(name, params)
    del not_documented[name]

def documented_class(name):
    pass

def documented_method(name, params):
    try:
        cls,meth = name.split('.')
    except ValueError:
        raise RuntimeError('Invalid class method name %s' % name)
    proc = cls+'_'+meth
    if proc not in not_documented:
        raise RuntimeError('%s is documented, but does not exist' % name)
    if any(x.startswith('Class.Method') for x in not_documented[proc]['attributes']):
        params = ['this'] + params
    check_parameters(proc, params)
    del not_documented[proc]

def documented_property(name):
    get_proc = 'get_'+name
    set_proc = 'set_'+name
    if get_proc in not_documented:
        del not_documented[get_proc]
    if set_proc in not_documented:
        del not_documented[set_proc]

def documented_class_property(name):
    try:
        cls,prop = name.split('.')
    except ValueError:
        raise RuntimeError('Invalid class property name %s' % name)
    get_proc = cls+'_get_'+prop
    set_proc = cls+'_set_'+prop
    if get_proc in not_documented:
        del not_documented[get_proc]
    if set_proc in not_documented:
        del not_documented[set_proc]

in_class = None

def process_directive(line):
    global in_class
    m = re.match('^(\s*)\.\. ([a-z]+):: (.+)$', line)
    if m is not None:
        indent = m.group(1)
        typ = m.group(2)
        signature = m.group(3)
        if typ == 'class':
            if signature != 'SpaceCenter':
                in_class = signature
                documented_class(in_class)
            else:
                in_class = None
        if typ == 'attribute' or typ == 'data':
            if in_class is None:
                documented_property(signature)
            else:
                documented_class_property(in_class+'.'+signature)
        if typ == 'method':
            m = re.match('^(.+) \((.*)\)$', signature)
            name = m.group(1)
            params = []
            for param in filter(lambda x: x != '', [p.strip() for p in m.group(2).split(',')]):
                if param.startswith('['):
                    param = param.split(' ')[0][1:]
                params.append(param)
            if in_class is None:
                documented_procedure(name, params)
            else:
                documented_method(in_class+'.'+name, params)

def process_file(path):
    with open(path, 'r') as f:
        for lineno,line in enumerate(f.readlines()):
            try:
                process_directive(line)
            except Exception, e:
                print 'Error on line', lineno, 'in', path
                print line
                print e
                raise
                exit(1)

for dirname,dirnames,filenames in os.walk(src):
    for filename in filenames:
        src_path = os.path.join(dirname, filename)
        try:
            process_file(src_path)
        except IOError:
            continue

    for k in not_documented.keys():
        raise RuntimeError('%s is not documented' % k)

print 'OK'
