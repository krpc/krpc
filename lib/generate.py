# Script used to generate BUILD.bazel and ksp.bzl
# Should be run from the root of the repository
# Usage: python lib/generate.py

import os

dlls = filter(lambda x: x.endswith('.dll'), os.listdir('lib/ksp/KSP_Data/Managed'))

build_bazel = 'load("@rules_dotnet//dotnet:defs.bzl", "import_dll")\n'
ksp2_bzl = 'ksp_deps = [\n'

for dll in dlls:
    name = dll.replace('.dll', '')
    build_bazel += f"""
import_dll(
    name = "{name}",
    dll = ":ksp/KSP_Data/Managed/{dll}",
    visibility = ["//visibility:public"],
)
"""
    ksp2_bzl += f'    "//lib:{name}",\n'

ksp2_bzl += ']\n'

with open('lib/BUILD.bazel', 'w') as fp:
    fp.write(build_bazel)

with open('lib/ksp.bzl', 'w') as fp:
    fp.write(ksp2_bzl)
