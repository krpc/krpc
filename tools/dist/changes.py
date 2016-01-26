#!/usr/bin/env python

import argparse
import re

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('site', choices=('github', 'kerbalstuff', 'curse'))
    args = parser.parse_args()

    data = [
        ('Server', get_changes('server/CHANGES.txt')),
        ('SpaceCenter service', get_changes('service/SpaceCenter/CHANGES.txt')),
        ('KerbalAlarmClock service', get_changes('service/KerbalAlarmClock/CHANGES.txt')),
        ('InfernalRobotics service', get_changes('service/InfernalRobotics/CHANGES.txt')),
        ('Python client', get_changes('client/python/CHANGES.txt')),
        ('C++ client', get_changes('client/cpp/CHANGES.txt')),
        ('C# client', get_changes('client/csharp/CHANGES.txt')),
        ('Lua client', get_changes('client/lua/CHANGES.txt'))
    ]

    version = re.match('version\s*=\s*\'(.+)\'', ''.join(open('config.bzl', 'r').readlines()), re.MULTILINE).group(1)

    if args.site == 'github':
        print ''.join(open('tools/dist/github-changes.tmpl', 'r').readlines()).replace('%VERSION%', version)
        print '### Changes ###\n'
        for name,changes in data:
            if len(changes[version]) == 1 and changes[version][0] == 'None':
	        continue
            print '####', name, '####\n'
            for item in changes[version]:
                print '*', item
            print
    elif args.site == 'kerbalstuff':
        for name,changes in data:
            if len(changes[version]) == 1 and changes[version][0] == 'None':
                continue
            print '*', name
            for item in changes[version]:
                print '  *', item
    else: # curse
        print '<ul>'
        for name,changes in data:
            if len(changes[version]) == 1 and changes[version][0] == 'None':
                continue
            print '<li>'
            print name
            print '<ul>'
            for item in changes[version]:
                print '<li>'
                print item
                print '</li>'
            print '</ul>'
            print '</li>'
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

def sort_changes(changes):
    return sorted(changes.items(), key=lambda (v,_): [-int(x) for x in v.split('.')])

if __name__ == '__main__':
    main()
