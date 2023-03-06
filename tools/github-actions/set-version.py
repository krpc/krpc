#!/usr/bin/env python3

# Set version in config to be the current version
# suffixed with a build number derived from the commit sha
# followed by the truncated commit sha
# The build number is between 0-65535 incl. to meet the required
# format for .NET assemblies

import os

with open("config.bzl", "r") as f:
    lines = f.readlines()
    for i, line in enumerate(lines):
        if line.startswith("version = "):
            version = line.partition("=")[2].strip()[1:-1]
            sha = os.getenv("GITHUB_SHA")[:7]
            sha_int = int(sha, 16) % 65536
            version += "-" + str(sha_int) + "-" + sha
            lines[i] = "version = '%s'\n" % version
            break

with open("config.bzl", "w") as f:
    f.write("".join(lines))
