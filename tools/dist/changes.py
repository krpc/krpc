#!/usr/bin/env python

import argparse
import re

def main():
    config = open('config.bzl', 'r').readlines()
    current_version = None
    for line in config:
        m = re.search('^version\s*=\s*\"(.+)\"$', line)
        if m:
            current_version = m.group(1)
    if current_version is None:
        print('Failed to get version from config.bzl')
        return 1

    parser = argparse.ArgumentParser()
    parser.add_argument('site', choices=('github', 'spacedock', 'curse'))
    parser.add_argument('version', nargs='?', default=current_version)
    args = parser.parse_args()

    data = [
        ('Server', get_changes('server/CHANGES.txt')),
        ('SpaceCenter service', get_changes('service/SpaceCenter/CHANGES.txt')),
        ('Drawing service', get_changes('service/Drawing/CHANGES.txt')),
        ('InfernalRobotics service', get_changes('service/InfernalRobotics/CHANGES.txt')),
        ('KerbalAlarmClock service', get_changes('service/KerbalAlarmClock/CHANGES.txt')),
        ('RemoteTech service', get_changes('service/RemoteTech/CHANGES.txt')),
        ('UI service', get_changes('service/UI/CHANGES.txt')),
        ('C# client', get_changes('client/csharp/CHANGES.txt')),
        ('C++ client', get_changes('client/cpp/CHANGES.txt')),
        ('C-nano client', get_changes('client/cnano/CHANGES.txt')),
        ('Java client', get_changes('client/java/CHANGES.txt')),
        ('Lua client', get_changes('client/lua/CHANGES.txt')),
        ('Python client', get_changes('client/python/CHANGES.txt')),
        ('krpctools', get_changes('tools/krpctools/CHANGES.txt'))
    ]

    changelist = []
    for name,changes in data:
        if args.version in changes and (len(changes[args.version]) > 1 or changes[args.version][0] != 'None'):
            changelist.append((name, changes[args.version]))

    if args.site == 'github':
        print(''.join(open('tools/dist/github-changes.tmpl', 'r').readlines()).replace('%VERSION%', args.version))
        print('### Changes\n')
    if args.site == 'github':
        for name,items in changelist:
            print('#### '+ name + '\n')
            for item in items:
                print('* ' + item)
            print('')
    else: # spacedock or curse
        for name,items in changelist:
            print('#### '+ name + '\n')
            for item in items:
                pattern = re.compile(r'#([0-9]+)')
                item = pattern.sub(r'[#\1](https://github.com/krpc/krpc/issues/\1)', item)
                print('* ' + item)
            print('')

def get_changes(path):
    changes = {}
    with open(path, 'r') as f:
        version = None
        for line in f.readlines():
            line = line.rstrip('\n')
            if line == '':
                continue
            m = re.match('^v([0-9]+\.[0-9]+\.[0-9]+).*?$', line)
            if m:
                version = m.group(1)
            elif line.startswith(' * '):
                if version not in changes:
                    changes[version] = []
                changes[version].append(line[3:])
            elif line.startswith('   '):
                changes[version][-1] += line[2:]
            else:
                print('Invalid line in ' + path + ':')
                print(line)
                exit(1)
    return changes

if __name__ == '__main__':
    main()
