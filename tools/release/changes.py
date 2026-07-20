#!/usr/bin/env python3

# Prints the changelog for a release, formatted for the GitHub release notes
# or for the mod-hosting sites (SpaceDock/CurseForge). Covers every component
# on the kRPC release version train.

import argparse
import re
import sys

COMPONENTS = [
    ('Server', 'server/CHANGES.txt'),
    ('Core', 'core/CHANGES.txt'),
    ('DockingCamera service', 'service/DockingCamera/CHANGES.txt'),
    ('Drawing service', 'service/Drawing/CHANGES.txt'),
    ('InfernalRobotics service', 'service/InfernalRobotics/CHANGES.txt'),
    ('KerbalAlarmClock service', 'service/KerbalAlarmClock/CHANGES.txt'),
    ('LiDAR service', 'service/LiDAR/CHANGES.txt'),
    ('RemoteTech service', 'service/RemoteTech/CHANGES.txt'),
    ('SpaceCenter service', 'service/SpaceCenter/CHANGES.txt'),
    ('UI service', 'service/UI/CHANGES.txt'),
    ('C# client', 'client/csharp/CHANGES.txt'),
    ('C++ client', 'client/cpp/CHANGES.txt'),
    ('C-nano client', 'client/cnano/CHANGES.txt'),
    ('Java client', 'client/java/CHANGES.txt'),
    ('Lua client', 'client/lua/CHANGES.txt'),
    ('Python client', 'client/python/CHANGES.txt'),
    ('krpctools', 'tools/krpctools/CHANGES.txt'),
]


def current_version():
    version = None
    with open('config.bzl', 'r') as config:
        for line in config.readlines():
            m = re.search(r'^version\s*=\s*"(.+)"$', line)
            if m:
                version = m.group(1)
    if version is None:
        print('Failed to get version from config.bzl', file=sys.stderr)
        sys.exit(1)
    return version


def render(site, version):
    """Return the changelog for a version, formatted for the given site."""
    changelist = []
    for name, path in COMPONENTS:
        changes = get_changes(path)
        if version in changes and \
                (len(changes[version]) > 1 or changes[version][0] != 'None'):
            changelist.append((name, changes[version]))

    out = []
    if site == 'github':
        with open('tools/release/github-changes.tmpl', 'r') as tmpl:
            out.append(''.join(tmpl.readlines()).replace('%VERSION%', version))
        out.append('### Changes\n')
        for name, items in changelist:
            out.append('#### ' + name + '\n')
            out.extend('* ' + item for item in items)
            out.append('')
    else:  # spacedock or curse; issue references become explicit links
        pattern = re.compile(r'#([0-9]+)')
        for name, items in changelist:
            out.append('#### ' + name + '\n')
            for item in items:
                item = pattern.sub(
                    r'[#\1](https://github.com/krpc/krpc/issues/\1)', item)
                out.append('* ' + item)
            out.append('')
    return '\n'.join(out)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('site', choices=('github', 'spacedock', 'curse'))
    parser.add_argument('version', nargs='?', default=current_version())
    args = parser.parse_args()
    print(render(args.site, args.version))


def get_changes(path):
    changes = {}
    with open(path, 'r') as f:
        version = None
        for line in f.readlines():
            line = line.rstrip('\n')
            if line == '':
                continue
            m = re.match(r'^v([0-9]+\.[0-9]+\.[0-9]+).*?$', line)
            if m:
                version = m.group(1)
            elif line.startswith(' * '):
                if version not in changes:
                    changes[version] = []
                changes[version].append(line[3:])
            elif line.startswith('   '):
                changes[version][-1] += line[2:]
            else:
                print('Invalid line in ' + path + ':', file=sys.stderr)
                print(line, file=sys.stderr)
                sys.exit(1)
    return changes


if __name__ == '__main__':
    main()
