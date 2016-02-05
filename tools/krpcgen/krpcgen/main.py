import os
import argparse
import json
import subprocess
import tempfile
import shutil
from pkg_resources import Requirement, resource_filename, resource_string
import krpcgen
from krpcgen.cpp import CppGenerator
from krpcgen.csharp import CsharpGenerator

def main():
    version = krpcgen.__version__
    parser = argparse.ArgumentParser(description='Generate source code for kRPC services')
    parser.add_argument('-v,--version', action='version', version='krpcgen version %s' % version)
    parser.add_argument('language', choices=('cpp', 'csharp'), help='Language to generate')
    parser.add_argument('service', help='Name of service to generate')
    parser.add_argument('input', help='Path to service definition JSON file or assembly DLL')
    parser.add_argument('output', help='Path to output source file to')
    parser.add_argument('--ksp', help='Path to Kerbal Space Program directory -- required when reading from a DLL')
    parser.add_argument('--output-defs', help='When generting client code from a DLL, output the service definitions to the given JSON file')
    parser.add_argument('--mono', action='store_true', help='Run service definitions generator using mono') #TODO: remove hack
    args = parser.parse_args()

    if not os.path.exists(args.input):
        print('Input not found')
        return 1

    defs = {}
    if args.input.endswith('.json'):
        with open(args.input, 'r') as f:
            defs.update(json.load(f))
    elif args.input.endswith('.dll'):
        defs = generate_defs(args)
        if not args.ksp:
            print('KSP directory not set. You must pass --ksp when generating code from an assembly DLL.')
        if not os.path.exists(args.ksp):
            print('KSP directory does not exist. Check the path passed to --ksp')
    else:
        print('Failed to read service definitions from \'%s\'. Not a JSON file or assembly DLL.' % args.input)
    if len(defs.keys()) == 0:
        print('No services found in services definition files')
        return 1
    if args.service not in defs.keys():
        print('Service \'%s\' not found' % args.service)
        return 1

    if args.language == 'cpp':
        generator = CppGenerator
    else:
        generator = CsharpGenerator

    macro_template = resource_string(__name__, args.language+'.tmpl').decode('utf-8')

    generator(macro_template, args.service, defs[args.service]).generate_file(args.output)

def generate_defs(args):
    assembly = os.path.abspath(args.input) #TODO: process path more intelligently (e.g. expand ~)
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
    subprocess.check_call([bindir+'/ServiceDefinitions.exe', args.service, tmpout, assembly])

    if args.output_defs:
        shutil.copy(tmpout, args.output_defs)
    with open(tmpout, 'r') as f:
        return json.load(f)

if __name__ == '__main__':
    main()
