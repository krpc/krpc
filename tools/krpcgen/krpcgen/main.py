import argparse
import json
import os
import shutil
import subprocess
import sys
import tempfile
from pkg_resources import Requirement, resource_filename, resource_string
import krpcgen
from krpcgen.cpp import CppGenerator
from krpcgen.csharp import CsharpGenerator

def main():
    version = krpcgen.__version__
    parser = argparse.ArgumentParser(prog='krpcgen', description='Generate client source code for kRPC services.')
    parser.add_argument('-v', '--version', action='version', version='krpcgen version %s' % version)
    parser.add_argument('language', choices=('cpp', 'csharp'), help='Language to generate')
    parser.add_argument('service', help='Name of service to generate')
    parser.add_argument('input', nargs='+', help='Path to service definition JSON file or assembly DLL(s)')
    parser.add_argument('-o', '--output', help='Path to write source code to. If not specified, writes source code to standard output.')
    parser.add_argument('--ksp', help='Path to Kerbal Space Program directory. Required when reading from an assembly DLL(s)')
    parser.add_argument('--output-defs', help='When generting client code from a DLL, output the service definitions to the given JSON file')
    args = parser.parse_args()

    # Check and expand input paths
    inputs = []
    for path in args.input:
        path = os.path.abspath(os.path.expanduser(os.path.expandvars(path)))
        if not os.path.exists(path):
            sys.stderr.write('Input \'%s\' not found\n' % path)
            return 1
        inputs.append(path)

    defs = {}
    if len(inputs) == 1 and inputs[0].endswith('.json'):
        # Load definitions from .json input
        with open(inputs[0], 'r') as f:
            defs.update(json.load(f))
    elif all(path.endswith('.dll') for path in inputs):
        # Get definitions from .dll input(s)
        try:
            defs = generate_defs(args, inputs)
        except RuntimeError, e:
            sys.stderr.write(str(e)+'\n')
            return 1
        if args.output_defs:
            with open(args.output_defs, 'w') as f:
                json.dump(defs, f)
    else:
        # No valid inputs found
        sys.stderr.write('Failed to read service definitions from \'%s\'. Expected a single JSON file, or one or more assembly DLLs.\n' % '\,\''.join(inputs))
        return 1

    # Check loaded definitions
    if len(defs.keys()) == 0:
        sys.stderr.write('No services found in input\n')
        return 1
    if args.service not in defs.keys():
        sys.stderr.write('Service \'%s\' not found in input\n' % args.service)
        return 1

    # Generate code
    if args.language == 'cpp':
        generator = CppGenerator
    else:
        generator = CsharpGenerator
    macro_template = resource_string(__name__, args.language+'.tmpl').decode('utf-8')
    g = generator(macro_template, args.service, defs[args.service])
    if args.output:
        g.generate_file(args.output)
    else:
        print(g.generate())

def generate_defs(args, assemblies):
    """ Generate service definitions from assembly DLLs using ServiceDefinitions.exe """

    if not args.ksp:
        raise RuntimeError ('KSP directory not set. You must pass --ksp when generating code from an assembly DLL.')

    if not os.path.exists(args.ksp):
        raise RuntimeError ('KSP directory does not exist. Check the path passed to --ksp')

    bindir = tempfile.mkdtemp(prefix='krpcgen-') #TODO: delete when done
    tmpout = bindir+'/defs.json'

    # Copy krpcgen binaries to the tmp dir
    binpath = resource_filename(Requirement.parse('krpcgen'),'krpcgen/bin')
    files = os.listdir(binpath)
    for filename in files:
        filename = os.path.join(binpath, filename)
        if (os.path.isfile(filename)):
            shutil.copy(filename, bindir)

    # Copy KSP DLLs to the tmp dir
    ksp_dlls = [
        'Assembly-CSharp.dll',
        'Assembly-CSharp-firstpass.dll',
        'UnityEngine.dll',
        'TDx.TDxInput.dll'
    ]
    for dll in ksp_dlls:
        shutil.copy(args.ksp+'/KSP_Data/Managed/'+dll, bindir)

    # Generate the service definitions
    try:
        subprocess.check_output([bindir+'/ServiceDefinitions.exe', args.service, tmpout] + assemblies)
    except subprocess.CalledProcessError, e:
        raise RuntimeError (e.output)

    if args.output_defs:
        shutil.copy(tmpout, args.output_defs)
    with open(tmpout, 'r') as f:
        return json.load(f)

if __name__ == '__main__':
    main()
