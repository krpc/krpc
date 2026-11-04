import argparse
import builtins
import json
import os
import sys
from importlib.resources import files
from typing import cast

import jinja2

from krpc.utils import snake_case

from ..definitions import Definitions
from ..utils import indent, lower_camel_case, single_line
from ..version import __version__
from .cnano import CnanoDomain
from .cpp import CppDomain
from .csharp import CsharpDomain
from .docgen import DocumentationGenerator
from .extensions import AppendExtension
from .java import JavaDomain
from .lua import LuaDomain
from .nodes import Service
from .python import PythonDomain
from .utils import lookup_cref


def main():
    prog = "krpc-docgen"
    parser = argparse.ArgumentParser(
        prog=prog,
        description="Generate API documentation for a kRPC service",
        usage=(
            "%(prog)s [options] LANGUAGE ORDER_FILE [DEFINITIONS ...] "
            "--page SOURCE OUTPUT [--page SOURCE OUTPUT ...]"
        ),
        epilog="Arguments are read from a file, one per line, with @FILE.",
        fromfile_prefix_chars="@",
    )
    parser.add_argument(
        "-v",
        "--version",
        action="version",
        version="%s version v%s" % (prog, __version__),
    )
    parser.add_argument(
        "language",
        choices=sorted(DOMAINS),
        help="Language to generate",
    )
    parser.add_argument(
        "order_file", metavar="ORDER_FILE", help="Path to the ordering file"
    )
    parser.add_argument(
        "definitions",
        nargs="*",
        metavar="DEFINITIONS",
        help="Paths to the service definition files",
    )
    parser.add_argument(
        "--page",
        nargs=2,
        action="append",
        required=True,
        metavar=("SOURCE", "OUTPUT"),
        help="Path to a template, and the path to write its page to. Can be repeated.",
    )
    parser.add_argument(
        "--no-warnings", action="store_true", default=False, help="Suppress warnings"
    )
    parser.add_argument(
        "--force",
        action="store_true",
        default=False,
        help="Always overwrite existing files",
    )
    parser.add_argument(
        "--documented",
        action="store",
        help="Path to output a list of the members the pages document",
    )
    args = parser.parse_intermixed_args()

    try:
        services = build_services(
            load_definitions(args.definitions), load_ordering(args.order_file)
        )
        documented = set()
        for source, output in args.page:
            content, page_documented = generate(args.language, services, source)
            write_file(output, content)
            documented.update(page_documented)
    except RuntimeError as ex:
        sys.stderr.write("Error: %s\n" % str(ex))
        return 1
    if args.documented:
        write_file(args.documented, "\n".join(sorted(documented)) + "\n")
    return 0


DOMAINS = {
    "cnano": CnanoDomain,
    "cpp": CppDomain,
    "csharp": CsharpDomain,
    "java": JavaDomain,
    "lua": LuaDomain,
    "python": PythonDomain,
}


def load_definitions(paths):
    """The service definitions in the given files, merged into one dictionary"""
    services_info = {}
    for path in paths:
        with builtins.open(path, encoding="utf-8") as fp:
            services_info.update(json.load(fp))
    if not services_info:
        print("No services found in services definition files")
        sys.exit(1)
    return services_info


def load_ordering(path):
    """The position of each member in the ordering file, by full name"""
    if not os.path.exists(path):
        raise RuntimeError("Ordering file '%s' does not exist" % path)
    with builtins.open(path, encoding="utf-8") as fp:
        return {x.strip(): i for i, x in enumerate(fp.readlines())}


def build_services(services_info, ordering):
    """The documentation nodes for the services"""
    sort_failed = []

    def sort(member):
        key = ordering.get(member.fullname)
        if key is None:
            sort_failed.append(member.fullname)
            return 0
        return key

    for info in services_info.values():
        del info["id"]

    # Register everything the services define before building any documentation node, as a node
    # for a procedure's default value is built from the type the value decodes through
    definitions = Definitions(services_info)

    services = {
        name: Service(name, sort=sort, types=definitions.types, **info)
        for name, info in definitions.services.items()
    }

    if sort_failed:
        raise RuntimeError("Don't know how to order:\n" + "\n".join(sort_failed))
    return services


def generate(language, services, source):
    """The documentation for a template, and the members it documents"""
    domain = DOMAINS[language](str(files(__name__).joinpath("%s.tmpl" % language)))
    return process_file(domain, services, source)


def write_file(path, content):
    """Write the content to the path, creating its directory if needed"""
    path = os.path.abspath(os.path.expanduser(os.path.expandvars(path)))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with builtins.open(path, "w", encoding="utf-8") as fp:
        fp.write(content)


def process_file(domain, services, path):
    loader = jinja2.FileSystemLoader(
        searchpath=[domain.macros_dir, os.path.abspath(os.path.dirname(path))]
    )
    template_env = jinja2.Environment(
        loader=loader,
        trim_blocks=True,
        lstrip_blocks=True,
        undefined=jinja2.StrictUndefined,
        extensions=[AppendExtension],
    )

    def hasdoc(xml, selector="./summary"):
        return DocumentationGenerator(domain, services, xml).has(selector)

    def gendoc(xml, selector="./summary"):
        return DocumentationGenerator(domain, services, xml).generate(selector)

    def gendesc(text):
        # Render a description fragment (e.g. a deprecation reason), which may
        # contain the same markup as documentation strings
        xml = "<doc><summary>%s</summary></doc>" % text
        return DocumentationGenerator(domain, services, xml).generate("./summary")

    def see(cref):
        obj = lookup_cref(cref, services)
        return domain.see(obj)

    documented = set()

    def mark_documented(x):
        documented.add(x.fullname)
        return ""

    context = {
        "language": domain.name,
        "domain": domain,
        "services": services,
        "hasdoc": hasdoc,
        "gendoc": gendoc,
        "gendesc": gendesc,
        "see": see,
        "mark_documented": mark_documented,
    }

    template_env.filters["snakecase"] = snake_case
    template_env.filters["lower_camelcase"] = lower_camel_case
    template_env.filters["indent"] = indent
    template_env.filters["singleline"] = single_line

    template = template_env.get_template(os.path.basename(path))
    # A template appends to the nodes it renders, and the nodes are shared between templates
    try:
        content = template.render(context)
    finally:
        extension = template_env.extensions[AppendExtension.identifier]
        for node in cast(AppendExtension, extension).appended_nodes:
            node.clear_appended()

    return (content.rstrip() + "\n", sorted(documented))


if __name__ == "__main__":
    sys.exit(main())
