#!/usr/bin/env python3

import packaging.version
import urllib.parse
import urllib.request
import hashlib
import re
from collections import OrderedDict


def read_workspace():
    """ Read WORKSPACE file into an ordered dictionary of key/value pairs """
    with open('WORKSPACE', 'r') as f:
        lines = f.readlines()

    header = ''
    loads = []
    workspace = []

    in_entry = False
    in_long_key_value = False
    i = 0
    while i < len(lines):
        line = lines[i]

        def error(msg):
            print(('ERROR on line %d' % i))
            print(msg)
            exit(1)

        if len(header) == 0:
            if line.strip().startswith('workspace'):
                header = line.strip()
            i += 1
            continue

        if line.strip().startswith('load'):
            loads.append(line.strip())
            i += 1
            continue

        if not in_entry:
            if len(line.strip()) == 0:
                i += 1
                continue
            typ = line.strip()
            if not typ.endswith('('):
                error('Expected \'(\' at end of line')
            typ = typ[:-1]
            workspace.append({'type': typ, 'props': OrderedDict()})
            in_entry = True
            i += 1
            continue

        if in_entry and line.strip() == ')':
            in_entry = False
            i += 1
            continue

        if in_entry:
            if '=' not in line:
                error('Expected \'=\'')
            key,_,value = line.partition('=')
            if not value.lstrip().startswith('"""'):
                value = value.strip().rstrip(',')
                i += 1
            else:
                multiline_value = [value.strip()]
                i += 1
                while not lines[i].rstrip().rstrip(',').rstrip().endswith('"""'):
                    multiline_value.append(lines[i].rstrip())
                    i += 1
                multiline_value.append(lines[i].rstrip().rstrip(',').rstrip())
                i += 1
                value = '\n'.join(multiline_value)
            workspace[-1]['props'][key.strip()] = value
            continue
    return header, loads, workspace


def write_workspace(header, loads, workspace):
    lines = [header]
    if len(loads) > 0:
        lines.append('')
        lines.extend(loads)
    for entry in workspace:
        lines.append('')
        lines.append(entry['type']+'(')
        props = ['    %s = %s' % entry for entry in list(entry['props'].items())]
        lines.append(',\n'.join(props))
        lines.append(')')
    with open('WORKSPACE', 'w') as f:
        f.write('\n'.join(lines)+'\n')


def parse_python_package(path):
    """ Parse a package download path into its name, version and archive type """
    fmts = ['tar.gz', 'zip']
    typ = None
    for fmt in fmts:
        if path.endswith('.'+fmt):
            typ = fmt
            path = path[:-(len(fmt)+1)]
    if typ is None:
        path, _ , typ = path.rpartition('.')
    version = None
    while version is None:
        path, _, version = path.rpartition('-')
        if not re.match('[0-9].+', version):
            version = None
    name = path
    return name, version, typ


def get_python_package_versions(package, typ):
    """ Get the versions for a given python package and release type """
    versions = OrderedDict()
    url = 'https://pypi.python.org/simple/%s/' % package.lower()
    with urllib.request.urlopen(url) as f:
        lines = [x.decode() for x in f.readlines()]
        for line in [line.strip() for line in lines if '<a ' in line]:
            match = re.search(r'href="([^"]+")', line)
            url = urllib.parse.urlparse(urllib.parse.urldefrag(match.group(1))[0])
            assert(url.path.startswith('/packages/'))
            if url.path.endswith(typ):
                _, version, _ = parse_python_package(url.path.rpartition('/')[2])
                try:
                    packaging.version.Version(version)
                except packaging.version.InvalidVersion:
                    continue
                if 'a' in version or 'b' in version:
                    continue
                versions[version] = url.geturl()
    return versions


def is_pypi_dep(props):
    """ Return true if the given workspace entry is a pypi package """
    if 'name' not in props:
        return False
    if not props['name'].startswith('\'python_'):
        return False
    if 'url' not in props:
        return False
    if 'pythonhosted.org' in props['url']:
        return True
    return False


def sha256file(path):
    """ Generate sha256 sum of a file """
    BUFFER_SIZE = 128 * 1024
    sha256 = hashlib.sha256()
    with open(path, 'rb') as f:
        while True:
            data = f.read(BUFFER_SIZE)
            if not data:
                break
            sha256.update(data)
    return sha256.hexdigest()


def main():
    print('Updating WORKSPACE')
    workspace_header, loads, workspace = read_workspace()
    for entry in workspace:
        props = entry['props']
        if is_pypi_dep(props):
            url = props['url'][1:-1]
            path = url.rpartition('/')[2]
            package, version, typ = parse_python_package(path)
            versions = get_python_package_versions(package, typ)
            latest_version = sorted(versions.keys(), key=packaging.version.Version)[-1]
            url = versions[latest_version]
            path = '\'%s\'' % url
            if props['url'] == path and props['sha256'] != '\'\'':
                print(props['name'][1:-1], 'is up to date')
            else:
                print('Updating', props['name'][1:-1], 'to', latest_version)
                props['url'] = path
                result = urllib.request.urlretrieve(props['url'][1:-1])
                entry['props']['sha256'] = '\'%s\'' % sha256file(result[0])
            entry['props']['downloaded_file_path'] = f"'{url.rpartition('/')[2]}'"
    write_workspace(workspace_header, loads, workspace)
    print('Done')


if __name__ == '__main__':
    main()
