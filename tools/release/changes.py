#!/usr/bin/env python3

# Prints the changelog for a release, formatted for the GitHub release notes
# or for the mod-hosting sites (SpaceDock/CurseForge). Covers every component
# on the kRPC release version train.

import argparse
import re
import sys

COMPONENTS = [
    ('Server', 'server/CHANGELOG.md'),
    ('Core', 'core/CHANGELOG.md'),
    ('DockingCamera service', 'service/DockingCamera/CHANGELOG.md'),
    ('Drawing service', 'service/Drawing/CHANGELOG.md'),
    ('InfernalRobotics service', 'service/InfernalRobotics/CHANGELOG.md'),
    ('KerbalAlarmClock service', 'service/KerbalAlarmClock/CHANGELOG.md'),
    ('LiDAR service', 'service/LiDAR/CHANGELOG.md'),
    ('RemoteTech service', 'service/RemoteTech/CHANGELOG.md'),
    ('SpaceCenter service', 'service/SpaceCenter/CHANGELOG.md'),
    ('UI service', 'service/UI/CHANGELOG.md'),
    ('C# client', 'client/csharp/CHANGELOG.md'),
    ('C++ client', 'client/cpp/CHANGELOG.md'),
    ('C-nano client', 'client/cnano/CHANGELOG.md'),
    ('Java client', 'client/java/CHANGELOG.md'),
    ('Lua client', 'client/lua/CHANGELOG.md'),
    ('Python client', 'client/python/CHANGELOG.md'),
    ('krpctools', 'tools/krpctools/CHANGELOG.md'),
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
            # '## [X.Y.Z]' header; any suffix inside the brackets (e.g. a
            # '.postN') or after them (' - unreleased') is ignored.
            m = re.match(r'^##\s+\[([0-9]+\.[0-9]+\.[0-9]+)[^\]]*\]', line)
            if m:
                version = m.group(1)
            elif version is None:
                continue  # anything before the first version header
            elif line.startswith('- '):
                changes.setdefault(version, []).append(line[2:])
            elif line.startswith('  '):
                changes[version][-1] += ' ' + line.strip()
            else:
                print('Invalid line in ' + path + ':', file=sys.stderr)
                print(line, file=sys.stderr)
                sys.exit(1)
    return changes


if __name__ == '__main__':
    main()
