import argparse
import builtins
import importlib
import json
import os
import sys
from importlib.resources import files

from ..definitions import Definitions
from ..servicedefs import servicedefs
from ..version import __version__
from .cnano import CnanoGenerator
from .cpp import CppGenerator
from .csharp import CsharpGenerator
from .java import JavaGenerator
from .python import PythonGenerator

GENERATORS = {
    "csharp": CsharpGenerator,
    "cpp": CppGenerator,
    "java": JavaGenerator,
    "cnano": CnanoGenerator,
    "python": PythonGenerator,
}


def main():
    prog = "krpc-clientgen"
    languages = ", ".join(sorted(GENERATORS.keys()))
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
        help="Path to service definition JSON file or assembly DLL(s)",
    )
    parser.add_argument(
        "-o",
        "--output",
        help="Path to write source code to. "
        + "If not specified, writes source code to standard output.",
    )
    parser.add_argument(
        "--ksp",
        help="Path to Kerbal Space Program directory. "
        + "Required when reading from an assembly DLL(s)",
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
        if len(inputs) == 1 and inputs[0].endswith(".json"):
            # From JSON file
            with builtins.open(inputs[0], encoding="utf-8") as fp:
                defs = json.load(fp)
            if args.output_defs:
                sys.stderr.write(
                    "Warning: Ignoring --output-defs as the definitions "
                    + "have been loaded from an existing JSON file.\n"
                )

        elif all(path.endswith(".dll") for path in inputs):
            # From assembly DLLs
            if not args.ksp:
                raise RuntimeError(
                    "KSP directory not set. You must pass --ksp when "
                    + "generating code from an assembly DLL."
                )
            defs = servicedefs(args.ksp, args.service, inputs)
            if args.output_defs:
                with builtins.open(args.output_defs, "w", encoding="utf-8") as fp:
                    fp.write(defs)
            defs = json.loads(defs)

        else:
            # No valid inputs found
            raise RuntimeError(
                (
                    "Failed to read service definitions from '%s'. "
                    + "Expected a single JSON file, or one or more "
                    + "assembly DLLs."
                )
                % "','".join(inputs)
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
    main()
