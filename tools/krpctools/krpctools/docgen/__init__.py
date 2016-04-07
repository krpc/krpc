import argparse
import glob
import os
import codecs
import json
import jinja2
from pkg_resources import resource_filename
from krpc.types import Types
from krpc.utils import snake_case
from ..utils import lower_camel_case, indent, single_line
from .utils import lookup_cref
from .csharp import CsharpDomain
from .cpp import CppDomain
from .java import JavaDomain
from .lua import LuaDomain
from .python import PythonDomain
from .nodes import Service
from .docparser import DocumentationParser
from .extensions import AppendExtension
from ..version import __version__

def main():
    prog = 'krpc-docgen'
    parser = argparse.ArgumentParser(prog=prog, description='Generate API documentation for a kRPC service')
    parser.add_argument('-v', '--version', action='version', version='%s version %s' % (prog, __version__))
    parser.add_argument('language', choices=('csharp', 'cpp', 'java', 'lua', 'python'), help='Language to generate')
    parser.add_argument('source', action='store', help='Path to source reStructuredText file')
    parser.add_argument('order_file', action='store', default='order.txt', help='Path to ordering file')
    parser.add_argument('output', action='store', help='Path to write output to')
    parser.add_argument('definitions', nargs='*', default=[], help='Paths to service definition files')
    parser.add_argument('--no-warnings', action='store_true', default=False, help='Suppress warnings')
    parser.add_argument('--force', action='store_true', default=False, help='Always overwrite existing files')
    parser.add_argument('--documented', action='store', help='Path to output a list of documented members')
    args = parser.parse_args()

    macros = resource_filename(__name__, '%s.tmpl' % args.language).decode('utf-8')

    if args.language == 'csharp':
        domain = CsharpDomain(macros)
    elif args.language == 'cpp':
        domain = CppDomain(macros)
    elif args.language == 'java':
        domain = JavaDomain(macros)
    elif args.language == 'lua':
        domain = LuaDomain(macros)
    else: #python
        domain = PythonDomain(macros)

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

    content,documented = process_file(args, domain, services, args.source)

    output = os.path.abspath(os.path.expanduser(os.path.expandvars(args.output)))
    if not os.path.exists(os.path.dirname(output)):
        os.makedirs(os.path.dirname(output))
    with codecs.open(output, 'w', encoding='utf8') as f:
        f.write(content)

    if args.documented:
        documented_path = os.path.abspath(os.path.expanduser(os.path.expandvars(args.documented)))
        if not os.path.exists(os.path.dirname(documented_path)):
            os.makedirs(os.path.dirname(documented_path))
        with open(documented_path, 'w') as f:
            f.write('\n'.join(documented)+'\n')

def process_file(args, domain, services, path):

    loader = jinja2.FileSystemLoader(searchpath=['./','/'])
    template_env = jinja2.Environment(
        loader=loader,
        trim_blocks=True,
        lstrip_blocks=True,
        undefined=jinja2.StrictUndefined,
        extensions=[AppendExtension]
    )

    def hasdoc(xml, selector='./summary'):
        return DocumentationParser(domain, services, xml).has(selector)

    documented = set()
    def mark_documented(x):
        documented.add(x.fullname)
        return ''

    context = {
        'language': args.language,
        'domain': domain,
        'services': services,
        'hasdoc': hasdoc,
        'mark_documented': mark_documented
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

    template_env.filters['snakecase'] = snake_case
    template_env.filters['lower_camelcase'] = lower_camel_case
    template_env.filters['indent'] = indent
    template_env.filters['singleline'] = single_line
    template_env.filters['parameter_type'] = parameter_type_filter
    template_env.filters['return_type'] = return_type_filter
    template_env.filters['type_description'] = type_description_filter
    template_env.filters['parsedoc'] = parsedoc_filter
    template_env.filters['parsesee'] = parsesee_filter
    template_env.filters['parsecode'] = parsecode_filter

    template = template_env.get_template(path)
    content = template.render(context)

    return (content.rstrip()+'\n', sorted(documented))

if __name__ == '__main__':
    main()
