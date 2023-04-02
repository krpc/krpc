import argparse
import json
import os
import shutil
import stat
import subprocess
import sys
import tempfile
from pkg_resources import Requirement, resource_filename
from ..version import __version__


def main():
    prog = 'krpc-servicedefs'
    parser = argparse.ArgumentParser(
        prog=prog,
        description='Generate a service definition file for a kRPC service.')
    parser.add_argument(
        '-v', '--version', action='version',
        version='%s version %s' % (prog, __version__))
    parser.add_argument(
        'ksp', help='Path to Kerbal Space Program directory')
    parser.add_argument(
        'service', help='Name of service')
    parser.add_argument(
        'assemblies', nargs='+',
        help='Paths to assemblies containing the service and ' +
        'any dependencies required to load it')
    parser.add_argument(
        '-o', '--output',
        help='Path to write output to. If not specified, ' +
        'output is written to standard output.')
    args = parser.parse_args()

    try:
        defs = servicedefs(args.ksp, args.service, args.assemblies)
    except RuntimeError as ex:
        sys.stderr.write("Error: %s\n" % str(ex))
        return 1

    if args.output:
        with open(args.output, 'w') as fp:
            fp.write(defs)
    else:
        print(defs)

    return 0


def servicedefs(ksp, service, assemblies):
    """ Generate service definitions from assembly DLLs
        using ServiceDefinitions.exe """
    bindir = tempfile.mkdtemp(prefix='krpc-servicedefs-')
    try:
        return _servicedefs(ksp, service, assemblies, bindir)
    finally:
        shutil.rmtree(bindir)


def _servicedefs(ksp, service, assemblies, bindir):
    if not os.path.exists(ksp):
        raise RuntimeError('Kerbal Space Program directory does not exist.')

    tmpout = bindir+'/out.json'

    # Copy binaries to the tmp dir
    binpath = resource_filename(
        Requirement.parse('krpctools'), 'krpctools/bin')
    files = os.listdir(binpath)
    for filename in files:
        if filename.startswith('ServiceDefinitions'):
            filepath = os.path.join(binpath, filename)
            shutil.unpack_archive(filepath, bindir)

            path = os.path.join(bindir, "ServiceDefinitions")
            st = os.stat(path)
            os.chmod(path, st.st_mode | stat.S_IEXEC)

            os.rename(
                os.path.join(bindir, "service_definitions.runtimeconfig.json"),
                os.path.join(bindir, "ServiceDefinitions.runtimeconfig.json")
            )
            os.rename(
                os.path.join(bindir, "service_definitions.deps.json"),
                os.path.join(bindir, "ServiceDefinitions.deps.json")
            )

    # Find KSP assemblies
    ksp_managed_candidates = [
        'KSP_Data/Managed',
        'KSP_x64_Data/Managed',
        'KSP2_x64_Data/Managed'
    ]
    ksp_managed_path = None
    for ksp_managed in ksp_managed_candidates:
        path = os.path.join(ksp, ksp_managed)
        if os.path.exists(path):
            ksp_managed_path = path
            break
    if ksp_managed_path is None:
        raise RuntimeError("Failed to find DLLs in Kerbal Space Program directory")

    # Copy KSP assemblies to the bin dir, and add to command line call
    for dll in os.listdir(ksp_managed_path):
        if not dll.startswith('System') and dll != 'mscorlib.dll':
            shutil.copy(os.path.join(ksp_managed_path, dll), bindir)
            assemblies.append(dll)

    # Generate the service definitions
    try:
        subprocess.check_output(
            ['./ServiceDefinitions', '--output=%s' % tmpout, service] + assemblies,
            stderr=subprocess.STDOUT,
            cwd=bindir)
    except subprocess.CalledProcessError as ex:
        shutil.rmtree(binpath)
        raise RuntimeError(ex.output) from ex

    with open(tmpout, 'r') as fp:
        return fp.read()


if __name__ == '__main__':
    main()
