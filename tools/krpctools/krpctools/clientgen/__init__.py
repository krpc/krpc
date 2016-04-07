import argparse
import json
import os
import sys
import tempfile
from pkg_resources import Requirement, resource_filename, resource_string
from .csharp import CsharpGenerator
from .cpp import CppGenerator
from .java import JavaGenerator
from ..version import __version__
from ..servicedefs import servicedefs

def main():
    prog = 'krpc-clientgen'
    parser = argparse.ArgumentParser(prog=prog, description='Generate client source code for kRPC services.')
    parser.add_argument('-v', '--version', action='version', version='%s version %s' % (prog, __version__))
    parser.add_argument('language', choices=('csharp', 'cpp', 'java'), help='Language to generate')
    parser.add_argument('service', help='Name of service to generate')
    parser.add_argument('input', nargs='+', help='Path to service definition JSON file or assembly DLL(s)')
    parser.add_argument('-o', '--output', help='Path to write source code to. If not specified, writes source code to standard output.')
    parser.add_argument('--ksp', help='Path to Kerbal Space Program directory. Required when reading from an assembly DLL(s)')
    parser.add_argument('--output-defs', help='When generting client code from a DLL, output the service definitions to the given JSON file')
    args = parser.parse_args()

    try:

        # Check and expand input paths
        inputs = []
        for path in args.input:
            path = os.path.abspath(os.path.expanduser(os.path.expandvars(path)))
            if not os.path.exists(path):
                raise RuntimeError ('Input \'%s\' not found.' % path)
            inputs.append(path)

        # Get service defs

        if len(inputs) == 1 and inputs[0].endswith('.json'):
            # From JSON file
            with open(inputs[0], 'r') as f:
                defs = json.load(f)
            if args.output_defs:
                sys.stderr.write('Warning: Ignoring --output-defs as the definitions have been loaded from an existing JSON file.\n')

        elif all(path.endswith('.dll') for path in inputs):
            # From assembly DLLs
            if not args.ksp:
                raise RuntimeError ('KSP directory not set. You must pass --ksp when generating code from an assembly DLL.')
            defs = servicedefs(args.ksp, args.service, inputs)
            if args.output_defs:
                with open(args.output_defs, 'w') as f:
                    f.write(defs)
            defs = json.loads(defs)

        else:
            # No valid inputs found
            raise RuntimeError('Failed to read service definitions from \'%s\'. Expected a single JSON file, or one or more assembly DLLs.' % '\,\''.join(inputs))

        # Check loaded definitions
        if len(defs.keys()) == 0:
            raise RuntimeError('No services found in input.')
        if args.service not in defs.keys():
            raise RuntimeError('Service \'%s\' not found in input.' % args.service)

        # Generate code
        if args.language == 'csharp':
            generator = CsharpGenerator
        elif args.language == 'cpp':
            generator = CppGenerator
        else:
            generator = JavaGenerator
        macro_template = resource_string(__name__, args.language+'.tmpl').decode('utf-8')
        g = generator(macro_template, args.service, defs[args.service])
        if args.output:
            g.generate_file(args.output)
        else:
            print(g.generate())

    except RuntimeError, e:
        sys.stderr.write('Error: %s\n' % str(e))
        return 1

if __name__ == '__main__':
    main()
