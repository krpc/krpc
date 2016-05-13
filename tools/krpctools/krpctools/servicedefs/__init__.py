import argparse
import json
import os
import shutil
import subprocess
import sys
import tempfile
from pkg_resources import Requirement, resource_filename
from ..version import __version__

def main():
    prog = 'krpc-servicedefs'
    parser = argparse.ArgumentParser(prog=prog,
                                     description='Generate a service definition file for a kRPC service.')
    parser.add_argument('-v', '--version', action='version', version='%s version %s' % (prog, __version__))
    parser.add_argument('ksp', help='Path to Kerbal Space Program directory')
    parser.add_argument('service', help='Name of service')
    parser.add_argument('assemblies', nargs='+',
                        help='Paths to assemblies containing the service and any dependencies required to load it')
    parser.add_argument('-o', '--output',
                        help='Path to write output to. If not specified, output is written to standard output.')
    args = parser.parse_args()

    try:
        defs = servicedefs(args.ksp, args.service, args.assemblies)
    except RuntimeError, ex:
        sys.stderr.write("Error: %s\n" % str(ex))
        return 1

    if args.output:
        with open(args.output, 'w') as fp:
            fp.write(defs)
    else:
        print defs

def servicedefs(ksp, service, assemblies):
    """ Generate service definitions from assembly DLLs using ServiceDefinitions.exe """

    if not os.path.exists(ksp):
        raise RuntimeError('Kerbal Space Program directory does not exist.')

    bindir = tempfile.mkdtemp(prefix='krpc-servicedefs-') #TODO: delete when done
    tmpout = bindir+'/out.json'

    # Copy binaries to the tmp dir
    binpath = resource_filename(Requirement.parse('krpctools'), 'krpctools/bin')
    files = os.listdir(binpath)
    for filename in files:
        filename = os.path.join(binpath, filename)
        if os.path.isfile(filename):
            shutil.copy(filename, bindir)

    # Copy KSP DLLs to the tmp dir
    ksp_dlls = [
        'Assembly-CSharp.dll',
        'Assembly-CSharp-firstpass.dll',
        'UnityEngine.dll'
    ]
    for dll in ksp_dlls:
        shutil.copy(ksp+'/KSP_Data/Managed/'+dll, bindir)

    # Generate the service definitions
    try:
        subprocess.check_output(
            [bindir+'/ServiceDefinitions.exe', '--output=%s' % tmpout, service] + assemblies,
            stderr=subprocess.STDOUT)
    except subprocess.CalledProcessError, ex:
        raise RuntimeError(ex.output)

    with open(tmpout, 'r') as fp:
        return fp.read()

if __name__ == '__main__':
    main()
