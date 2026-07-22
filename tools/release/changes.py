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


def render_nodes(out, nodes, level, linkify=None):
    """Append markdown bullets for nodes and their nested sub-items, indenting
    sub-lists by two spaces per level. linkify, if given, rewrites each line."""
    for node in nodes:
        text = linkify(node['text']) if linkify else node['text']
        out.append('  ' * level + '* ' + text)
        render_nodes(out, node['children'], level + 1, linkify)


def render(site, version):
    """Return the changelog for a version, formatted for the given site."""
    changelist = []
    for name, path in COMPONENTS:
        changes = get_changes(path)
        nodes = changes.get(version)
        if nodes and (len(nodes) > 1 or nodes[0]['text'] != 'None'):
            changelist.append((name, nodes))

    out = []
    if site == 'github':
        with open('tools/release/github-changes.tmpl', 'r') as tmpl:
            out.append(''.join(tmpl.readlines()).replace('%VERSION%', version))
        out.append('### Changes\n')
        for name, nodes in changelist:
            out.append('#### ' + name + '\n')
            render_nodes(out, nodes, 0)
            out.append('')
    else:  # spacedock or curse; issue references become explicit links
        pattern = re.compile(r'#([0-9]+)')
        def linkify(text):
            return pattern.sub(
                r'[#\1](https://github.com/krpc/krpc/issues/\1)', text)
        for name, nodes in changelist:
            out.append('#### ' + name + '\n')
            render_nodes(out, nodes, 0, linkify)
            out.append('')
    return '\n'.join(out)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('site', choices=('github', 'spacedock', 'curse'))
    parser.add_argument('version', nargs='?', default=current_version())
    args = parser.parse_args()
    print(render(args.site, args.version))


def get_changes(path):
    """Parse a CHANGELOG.md into {version: [node, ...]}, where each node is
    {'text': str, 'children': [node, ...]}. A '- ' bullet at the margin is a
    top-level entry; a '  - ' bullet under it is a sub-item; other indented
    lines continue the current bullet's text."""
    changes = {}
    with open(path, 'r') as f:
        version = None
        top = None  # current top-level node
        for line in f.readlines():
            line = line.rstrip('\n')
            if line == '':
                continue
            # '## [X.Y.Z]' header; any suffix inside the brackets (e.g. a
            # '.postN') or after them (' - unreleased') is ignored.
            m = re.match(r'^##\s+\[([0-9]+\.[0-9]+\.[0-9]+)[^\]]*\]', line)
            if m:
                version = m.group(1)
                top = None
                continue
            if version is None:
                continue  # anything before the first version header
            indent = len(line) - len(line.lstrip(' '))
            stripped = line.strip()
            if stripped.startswith('- '):
                node = {'text': stripped[2:], 'children': []}
                if indent == 0 or top is None:
                    top = node
                    changes.setdefault(version, []).append(node)
                else:
                    top['children'].append(node)
            elif indent > 0 and top is not None:
                # continuation of the current bullet (sub-item if one is open)
                target = top['children'][-1] if top['children'] else top
                target['text'] += ' ' + stripped
            else:
                print('Invalid line in ' + path + ':', file=sys.stderr)
                print(line, file=sys.stderr)
                sys.exit(1)
    return changes


if __name__ == '__main__':
    main()
