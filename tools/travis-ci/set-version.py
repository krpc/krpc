#!/usr/bin/env python

import os
import re
import subprocess

version_pattern = r'^v[0-9]+\.[0-9]+\.[0-9]+$'

# Get most recent tag, commits since tag and commit hash
desc = subprocess.check_output(
    ['git', 'describe', '--long', '--always']).strip()
commit_hash = ''
num_commits = ''
if re.search(r'-g[0-9a-f]+$', desc):
    desc, _, commit_hash = desc.rpartition('-')
if re.search(r'-[0-9]+$', desc):
    desc, _, num_commits = desc.rpartition('-')
tag = desc

# Get current branch
branch = os.getenv('TRAVIS_BRANCH')

# Compute version number
if re.match(version_pattern, branch):
    # Version branch - use version from branch name
    version = (branch[1:], num_commits, commit_hash)
elif re.match(version_pattern, tag):
    # Version tag - use version from tag, incremented by 0.0.1
    parts = tag[1:].split('.')
    parts[2] = str(int(parts[2])+1)
    version = ('.'.join(parts), num_commits, commit_hash)
else:
    # Tag is not a version, use it verbatim
    version = (tag, num_commits, commit_hash)

# Compute version string
version = '%s-%s-%s' % version
print 'version =', version

# Update config.bzl
with open('config.bzl', 'r') as f:
    lines = f.readlines()
    for i, line in enumerate(lines):
        if line.startswith('version = '):
            lines[i] = 'version = \'%s\'\n' % version
            break
with open('config.bzl', 'w') as f:
    f.write(''.join(lines))
