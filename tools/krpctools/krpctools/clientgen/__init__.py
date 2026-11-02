import argparse
import builtins
import importlib
import json
import os
import sys
import warnings
from importlib.resources import files

from ..definitions import Definitions
from ..servicedefs import servicedefs
from ..version import __version__
from .cnano import CnanoGenerator
from .cpp import CppGenerator
from .csharp import CsharpGenerator
from .dispatch import (
    CnanoDispatchGenerator,
    CppDispatchGenerator,
    CsharpDispatchGenerator,
    JavaDispatchGenerator,
)
from .docparser import DocumentationMarkupWarning
from .java import JavaGenerator
from .python import PythonGenerator

_default_show_warning = warnings.showwarning


def _show_warning(message, category, filename, lineno, file=None, line=None):
    if issubclass(category, DocumentationMarkupWarning):
        sys.stderr.write("Warning: %s\n" % message)
    else:
        _default_show_warning(message, category, filename, lineno, file, line)


GENERATORS = {
    "csharp": CsharpGenerator,
    "cpp": CppGenerator,
    "java": JavaGenerator,
    "cnano": CnanoGenerator,
    "python": PythonGenerator,
    # The dispatch of a conformance testee
    "csharp-dispatch": CsharpDispatchGenerator,
    "cpp-dispatch": CppDispatchGenerator,
    "java-dispatch": JavaDispatchGenerator,
    "cnano-dispatch": CnanoDispatchGenerator,
}


def _reference_dirs(args):
    """The directories to load the assemblies a service references from. --ksp names
    those of a Kerbal Space Program install."""
    reference_dirs = list(args.reference_dir)
    if args.ksp:
        for data in ("KSP_Data", "KSP_x64_Data"):
            path = os.path.join(args.ksp, data, "Managed")
            if os.path.isdir(path):
                reference_dirs.append(path)
                break
        else:
            raise RuntimeError("No managed assemblies found in '%s'." % args.ksp)
    return reference_dirs


def main():
    prog = "krpc-clientgen"
    # The dispatch generators are for the conformance testees in the kRPC repository
    languages = ", ".join(sorted(x for x in GENERATORS if not x.endswith("-dispatch")))
    parser = argparse.ArgumentParser(
        prog=prog, description="Generate client source code for kRPC services."
    )
    parser.add_argument(
        "-v",
        "--version",
        action="version",
        version="%s version v%s" % (prog, __version__),
    )
    parser.add_argument(
        "language", help="Language to generate (%s) or path to generator" % languages
    )
    parser.add_argument("service", help="Name of service to generate")
    parser.add_argument(
        "input",
        nargs="+",
        help="Path to service definition JSON file(s) or assembly DLL(s). "
        + "JSON files can give the definitions of the services whose types a DLL's "
        + "service uses.",
    )
    parser.add_argument(
        "-o",
        "--output",
        help="Path to write source code to. "
        + "If not specified, writes source code to standard output.",
    )
    parser.add_argument(
        "--reference-dir",
        action="append",
        default=[],
        metavar="DIR",
        help="When reading from an assembly DLL, a directory to load the assemblies "
        + "the service references from, such as the game's managed assemblies. "
        + "Can be repeated.",
    )
    parser.add_argument(
        "--ksp",
        help="Deprecated: pass the KSP_Data/Managed directory of a Kerbal Space Program "
        + "install with --reference-dir.",
    )
    parser.add_argument(
        "--include",
        action="append",
        default=[],
        metavar="SERVICE=PATH",
        help="Include the generated code of another service, whose types the service "
        + "uses, from the given path. Required by the C++ and C nano generators for "
        + "each such service.",
    )
    parser.add_argument(
        "--output-defs",
        help="When generating client code from a DLL, "
        + "output the service definitions to the given JSON file",
    )
    args = parser.parse_args()
    warnings.showwarning = _show_warning
    if args.ksp:
        sys.stderr.write(
            "Warning: --ksp is deprecated, pass --reference-dir instead.\n"
        )

    try:
        includes = {}
        for include in args.include:
            service, sep, path = include.partition("=")
            if not sep or not service or not path:
                raise RuntimeError(
                    "Invalid include '%s'. Expected SERVICE=PATH." % include
                )
            includes[service] = path

        # Check and expand input paths
        inputs = []
        for path in args.input:
            path = os.path.abspath(os.path.expanduser(os.path.expandvars(path)))
            if not os.path.exists(path):
                raise RuntimeError("Input '%s' not found." % path)
            inputs.append(path)

        # Get service defs
        others = [path for path in inputs if not path.endswith((".json", ".dll"))]
        if others:
            raise RuntimeError(
                "Failed to read service definitions from '%s'. "
                "Expected JSON files or assembly DLLs." % "','".join(others)
            )
        dlls = [path for path in inputs if path.endswith(".dll")]
        defs = {}
        # From JSON files, holding the service or the services its types come from
        for path in inputs:
            if path.endswith(".json"):
                with builtins.open(path, encoding="utf-8") as fp:
                    defs.update(json.load(fp))
        if dlls:
            # From assembly DLLs
            dll_defs = servicedefs(args.service, dlls, _reference_dirs(args))
            if args.output_defs:
                with builtins.open(args.output_defs, "w", encoding="utf-8") as fp:
                    fp.write(dll_defs)
            defs.update(json.loads(dll_defs))
        elif args.output_defs:
            sys.stderr.write(
                "Warning: Ignoring --output-defs as the definitions "
                + "have been loaded from an existing JSON file.\n"
            )

        # Check loaded definitions
        if not defs.keys():
            raise RuntimeError("No services found in input.")
        if args.service not in defs.keys():
            raise RuntimeError("Service '%s' not found in input." % args.service)

        # Get generator and template
        if args.language in GENERATORS:
            # Built-in generator and template
            generator = GENERATORS[args.language]
            macro_template = (
                files(__name__)
                .joinpath(args.language + ".tmpl")
                .read_text(encoding="utf-8")
            )
        else:
            # Generator defined in a python module
            generator, macro_template = load_generator(args.language)

        # Run the generator, over all of the definitions that were loaded: the requested
        # service is the one generated, but the types it refers to may be defined by any of them
        g = generator(macro_template, args.service, Definitions(defs), includes)
        if args.output:
            g.generate_file(args.output)
        else:
            print(g.generate())

    except RuntimeError as ex:
        sys.stderr.write("Error: %s\n" % str(ex))
        return 1

    return 0


def load_generator(path):
    path = os.path.abspath(os.path.expanduser(os.path.expandvars(path)))
    dirpath = os.path.dirname(path)
    modulepath = os.path.basename(path).rstrip(".py")
    sys.path.append(dirpath)
    module = importlib.import_module(modulepath)
    generator = module.generator
    with builtins.open(module.tmpl, encoding="utf-8") as fp:
        macro_template = "".join(fp.readlines())
    return generator, macro_template


if __name__ == "__main__":
    sys.exit(main())
