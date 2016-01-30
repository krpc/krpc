#!/usr/bin/env python

import argparse
import re

def main():
    current_version = re.match('version\s*=\s*\'(.+)\'', ''.join(open('config.bzl', 'r').readlines()), re.MULTILINE).group(1)

    parser = argparse.ArgumentParser()
    parser.add_argument('site', choices=('github', 'kerbalstuff', 'curse'))
    parser.add_argument('version', nargs='?', default=current_version)
    args = parser.parse_args()

    data = [
        ('Server', get_changes('server/CHANGES.txt')),
        ('SpaceCenter service', get_changes('service/SpaceCenter/CHANGES.txt')),
        ('KerbalAlarmClock service', get_changes('service/KerbalAlarmClock/CHANGES.txt')),
        ('InfernalRobotics service', get_changes('service/InfernalRobotics/CHANGES.txt')),
        ('Python client', get_changes('client/python/CHANGES.txt')),
        ('C++ client', get_changes('client/cpp/CHANGES.txt')),
        ('C# client', get_changes('client/csharp/CHANGES.txt')),
        ('Lua client', get_changes('client/lua/CHANGES.txt')),
        ('krpcgen', get_changes('tools/krpcgen/CHANGES.txt'))
    ]

    changelist = []
    for name,changes in data:
        if args.version in changes and (len(changes[args.version]) > 1 or changes[args.version][0] != 'None'):
            changelist.append((name, changes[args.version]))

    if args.site == 'github':
        print ''.join(open('tools/dist/github-changes.tmpl', 'r').readlines()).replace('%VERSION%', args.version)
        print '### Changes ###\n'
        for name,items in changelist:
            print '####', name, '####\n'
            for item in items:
                print '*', item
            print
    elif args.site == 'kerbalstuff':
        for name,items in changelist:
            print '*', name
            for item in items:
                print '  *', item
    else: # curse
        print '<ul>'
        for name,items in changelist:
            print '<li>'+name+'<ul>'
            for item in items:
                print '<li>'+item+'</li>'
            print '</ul></li>'
        print '</ul>'

def get_changes(path):
    changes = {}
    with open(path, 'r') as f:
        version = None
        for line in f.readlines():
            line = line.rstrip('\n')
            if line == '':
                continue
            m = re.match('^v([0-9]+\.[0-9]+\.[0-9]+)$', line)
            if m:
                version = m.group(1)
            elif line.startswith(' * '):
                if version not in changes:
                    changes[version] = []
                changes[version].append(line[3:])
            elif line.startswith('   '):
                changes[version][0] += line[2:]
            else:
                print 'Invalid line:'
                print line
                exit(1)
    return changes

if __name__ == '__main__':
    main()
