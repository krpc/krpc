import argparse
import glob
import os
import codecs
import json
import jinja2
from pkg_resources import resource_filename
from krpc.types import Types
from .utils import snakecase, indent, singleline, lookup_cref
from .cpp import CppDomain
from .csharp import CsharpDomain
from .lua import LuaDomain
from .python import PythonDomain
from .java import JavaDomain
from .nodes import Service
from .docparser import DocumentationParser
from .extensions import AppendExtension
from ..version import __version__

def main():
    prog = 'krpc-docgen'
    parser = argparse.ArgumentParser(prog=prog, description='Generate API documentation for a kRPC service')
    parser.add_argument('-v', '--version', action='version', version='%s version %s' % (prog, __version__))
    parser.add_argument('language', choices=('cpp', 'csharp', 'lua', 'python', 'java'), help='Language to generate')
    parser.add_argument('source', action='store', help='Path to source reStructuredText file')
    parser.add_argument('order_file', action='store', default='order.txt', help='Path to ordering file')
    parser.add_argument('output', action='store', help='Path to write output to')
    parser.add_argument('definitions', nargs='*', default=[], help='Paths to service definition files')
    parser.add_argument('--no-warnings', action='store_true', default=False, help='Suppress warnings')
    parser.add_argument('--force', action='store_true', default=False, help='Always overwrite existing files')
    parser.add_argument('--macros', action='store', default=None, help='Path to macros template file')
    args = parser.parse_args()

    macros = args.macros
    if macros == None:
        macros = resource_filename(__name__, '%s.tmpl' % args.language).decode('utf-8')

    if args.language == 'cpp':
        domain = CppDomain(macros)
    elif args.language == 'csharp':
        domain = CsharpDomain(macros)
    elif args.language == 'lua':
        domain = LuaDomain(macros)
    elif args.language == 'python':
        domain = PythonDomain(macros)
    else: # java
        domain = JavaDomain(macros)

    if not os.path.exists(args.order_file):
        raise RuntimeError('Ordering file \'%s\' does not exist' % args.order_file)
    with open(args.order_file, 'r') as f:
        ordering = [x.strip() for x in f.readlines()]

    services_info = {}
    for path in args.definitions:
        with open(path, 'r') as f:
            services_info.update(json.load(f))

    if services_info == {}:
        print 'No services found in services definition files'
        exit(1)

    sort_failed = []

    def sort(member):
        if member.fullname not in ordering:
            sort_failed.append(member.fullname)
            return 0
        else:
            return ordering.index(member.fullname)

    services = [Service(name, sort=sort, **info) for name,info in services_info.iteritems()]
    services = {service.name: service for service in services}

    if len(sort_failed) > 0:
        raise RuntimeError ('Don\'t know how to order:\n'+'\n'.join(sort_failed))

    content = process_file(args, domain, services, args.source)
    output = os.path.abspath(os.path.expanduser(os.path.expandvars(args.output)))
    if not os.path.exists(os.path.dirname(output)):
        os.makedirs(os.path.dirname(output))
    with codecs.open(output, 'w', encoding='utf8') as f:
        f.write(content)

def process_file(args, domain, services, path):

    loader = jinja2.FileSystemLoader(searchpath=['./','/'])
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

if __name__ == '__main__':
    main()
