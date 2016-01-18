#!/usr/bin/env python

import argparse
import glob
import os
import codecs
import json
import jinja2
from krpc.types import Types
from lib.utils import snakecase, indent, singleline, lookup_cref
from lib.python import PythonDomain
from lib.lua import LuaDomain
from lib.cpp import CppDomain
from lib.nodes import Service
from lib.docparser import DocumentationParser
from lib.extensions import AppendExtension

def process_file(args, domain, services, path):

    loader = jinja2.FileSystemLoader(searchpath='./')
    template_env = jinja2.Environment(
        loader=loader,
        trim_blocks=True,
        lstrip_blocks=True,
        undefined=jinja2.StrictUndefined,
        extensions=[AppendExtension]
    )

    def hasdoc_fn(xml, selector='./summary'):
        return DocumentationParser(domain, services, xml).has(selector)

    context = {
        'language': args.language,
        'domain': domain,
        'services': services,
        'hasdoc': hasdoc_fn
    }

    def return_type_filter(typ):
        return domain.return_type(typ)

    def parameter_type_filter(typ):
        return domain.parameter_type(typ)

    def type_description_filter(typ):
        return domain.type_description(typ)

    def parsedoc_filter(xml, selector='./summary'):
        return DocumentationParser(domain, services, xml).parse(selector)

    def parsesee_filter(cref):
        obj = lookup_cref(cref, services)
        return domain.see(obj)

    def parsecode_filter(value):
        return domain.code(value)

    template_env.filters['snakecase'] = snakecase
    template_env.filters['indent'] = indent
    template_env.filters['singleline'] = singleline
    template_env.filters['parameter_type'] = parameter_type_filter
    template_env.filters['return_type'] = return_type_filter
    template_env.filters['type_description'] = type_description_filter
    template_env.filters['parsedoc'] = parsedoc_filter
    template_env.filters['parsesee'] = parsesee_filter
    template_env.filters['parsecode'] = parsecode_filter

    template = template_env.get_template(path)
    content = template.render(context)
    return content.rstrip()+'\n'

def main():
    parser = argparse.ArgumentParser(description='Generate API documentation from service definitions')
    parser.add_argument('language', choices = ['python', 'lua', 'cpp'],
                        help='Language to compile')
    parser.add_argument('source', action='store',
                        help='Path to source file')
    parser.add_argument('destination', action='store',
                        help='Path to destination file')
    parser.add_argument('definitions', nargs='*', default=[],
                        help='Paths to service definition files')
    parser.add_argument('--no-warnings', action='store_true', default=False,
                        help='Ignore warnings')
    parser.add_argument('--force', action='store_true', default=False,
                        help='Overwrite existing files, even when nothing\'s changed')
    parser.add_argument('--order-file', action='store', default='order.txt',
                        help='Path to order definition file')
    parser.add_argument('--python-macros', action='store', default='lib/python.tmpl',
                        help='Path to Python macros template file')
    parser.add_argument('--cpp-macros', action='store', default='lib/cpp.tmpl',
                        help='Path to C++ macros template file')
    parser.add_argument('--lua-macros', action='store', default='lib/python.tmpl',
                        help='Path to Lua macros template file')
    args = parser.parse_args()

    if args.language == 'python':
        domain = PythonDomain(args)
    elif args.language == 'cpp':
        domain = CppDomain(args)
    else: # lua
        domain = LuaDomain(args)

    if not os.path.exists(args.order_file):
        raise RuntimeError('Ordering file \'%s\' does not exist' % args.order_file)
    with open(args.order_file, 'r') as f:
        ordering = [x.strip() for x in f.readlines()]

    services_info = {}
    for definition in args.definitions:
        for path in glob.glob(definition):
            with open(path, 'r') as f:
                services_info.update(json.load(f))

    if services_info == {}:
        print 'No services found in services definition files'
        exit(1)

    services = [Service(name, **info) for name,info in services_info.items()]
    for service in services:
        service.sort(ordering)
    services = dict([(service.name,service) for service in services])

    content = process_file(args, domain, services, args.source)
    if not os.path.exists(os.path.dirname(args.destination)):
        os.makedirs(os.path.dirname(args.destination))
    with codecs.open(args.destination, 'w', encoding='utf8') as f:
        f.write(content)

if __name__ == '__main__':
    main()
