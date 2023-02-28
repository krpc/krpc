#!/usr/bin/env python3

import sys

lines = sys.stdin.readlines()

ok = False
ignores = {}

for line in lines:
    print(line.rstrip('\n'))

    if 'found no defects' in line:
        ok = True

    if '* Target:' in line:
        target = line[9:].strip()
        if ' ' in target and '(' in target:
            target = target.partition(' ')[2]
            target = target[:target.find('::')]
    if 'gendarme/wiki/Gendarme.Rules.' in line:
        start = line.find('Gendarme.Rules.')
        end = line.find('(')
        rule = line[start:end]
        if rule not in ignores:
            ignores[rule] = set()
        ignores[rule].add(target)


if not ok:
    print('------------------------------------------------------------')
    print()
    print('Use the following ignores to ignore all of these errors:')
    print()
    for rule, targets in sorted(ignores.items(), key=lambda x: x[0]):
        print('R:', rule)
        for target in sorted(targets):
            print('T:', target)

    exit(1)
