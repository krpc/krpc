#!/usr/bin/env python

import sys
import json

with open(sys.argv[1], 'r') as f:
    expected = set(x.strip() for x in f.readlines() if x.strip() != '')

with open(sys.argv[2], 'r') as f:
    actual = set(x.strip() for x in f.readlines() if x.strip() != '')

if expected != actual:
    print 'Following were expected to be documented but where not:'
    for x in expected.difference(actual):
        print x
    print
    print 'Following were documented but where not expected to be:'
    for x in actual.difference(expected):
        print x

    exit(1)

else:
    print 'All members documented'
