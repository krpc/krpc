import argparse
import os
import subprocess
import sys
import tempfile

from ..dotnet import command, servicedefinitions_dir
from ..version import __version__


def main():
    prog = "krpc-servicedefs"
    parser = argparse.ArgumentParser(
        prog=prog, description="Generate a service definition file for a kRPC service."
    )
    parser.add_argument(
        "-v",
        "--version",
        action="version",
        version="%s version v%s" % (prog, __version__),
    )
    parser.add_argument("service", help="Name of service")
    parser.add_argument(
        "assemblies",
        nargs="+",
        help="Paths to assemblies containing the service and "
        + "any dependencies required to load it",
    )
    parser.add_argument(
        "-o",
        "--output",
        help="Path to write output to. If not specified, "
        + "output is written to standard output.",
    )
    parser.add_argument(
        "--reference-dir",
        action="append",
        default=[],
        metavar="DIR",
        help="Directory to load the assemblies the service references from, such as "
        + "the game's managed assemblies. Can be repeated.",
    )
    args = parser.parse_args()

    try:
        defs = servicedefs(args.service, args.assemblies, args.reference_dir)
    except RuntimeError as ex:
        sys.stderr.write("Error: %s\n" % str(ex))
        return 1

    if args.output:
        with open(args.output, "w", encoding="utf-8") as fp:
            fp.write(defs)
    else:
        print(defs)

    return 0


def servicedefs(service, assemblies, reference_dirs=()):
    """Generate service definitions from assembly DLLs using the
    ServiceDefinitions tool (requires the .NET 10 runtime). The tool also
    loads referenced assemblies from the directories of the given assemblies."""

    for directory in reference_dirs:
        if not os.path.isdir(directory):
            raise RuntimeError("Reference directory '%s' does not exist." % directory)

    tool = os.path.join(servicedefinitions_dir(), "ServiceDefinitions.dll")
    with tempfile.TemporaryDirectory(prefix="krpc-servicedefs-") as directory:
        output = os.path.join(directory, "out.json")
        args = ["--output=" + output]
        args += ["--reference-dir=" + os.path.abspath(x) for x in reference_dirs]
        args += [service] + [os.path.abspath(x) for x in assemblies]
        try:
            subprocess.check_output(command(tool, args), stderr=subprocess.STDOUT)
        except subprocess.CalledProcessError as ex:
            raise RuntimeError(ex.output.decode("utf-8", "replace")) from ex
        with open(output, encoding="utf-8") as fp:
            return fp.read()


if __name__ == "__main__":
    sys.exit(main())
