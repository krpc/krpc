"""Check that every C# source file is listed in its project's .csproj.

The Bazel build globs its sources, so a .cs file that is missing from the .csproj
still builds and tests clean here; only a `dotnet build` of KRPC.sln notices. This
test closes that gap, and catches the stale entries (a .csproj naming a file that
no longer exists) that break that build outright.

It compares file lists rather than compiling, so it also catches divergences a
build cannot see: a test fixture reached only by reflection is a compile-clean
omission that silently drops the fixture from the solution's test assembly.
"""

import os
import re
import sys
import xml.etree.ElementTree as ET

# Project paths as listed in the solution, e.g.
#     Project("{FAE0...}") = "KRPC.Core", "core\src\KRPC.Core.csproj", "{960F...}"
SLN_PROJECT = re.compile(r'^Project\([^)]*\)\s*=\s*"[^"]*",\s*"([^"]*\.csproj)"', re.MULTILINE)

# Sources outside the project directory: the generated AssemblyInfo.cs, pulled in
# as $(bazel-bin)\..., and files shared with a sibling project via a relative path.
# Neither can diverge from a glob of the project directory, so neither is checked.
EXTERNAL_INCLUDE = re.compile(r"^\$\(|^\.\.")

IGNORED_DIRS = {"obj", "bin"}


def compile_includes(csproj):
    """Return the project-relative paths the .csproj declares as <Compile Include>."""
    declared = set()
    for element in ET.parse(csproj).iter():
        # Strip the MSBuild namespace that ElementTree keeps on the tag.
        if element.tag.rsplit("}", 1)[-1] != "Compile":
            continue
        include = element.get("Include")
        if include is None or EXTERNAL_INCLUDE.match(include):
            continue
        declared.add(os.path.normpath(include.replace("\\", "/")))
    return declared


def sources_on_disk(directory):
    """Return the paths of the .cs files under a project directory, relative to it."""
    found = set()
    for dirpath, dirnames, filenames in os.walk(directory):
        dirnames[:] = [d for d in dirnames if d not in IGNORED_DIRS]
        for filename in filenames:
            if filename.endswith(".cs"):
                path = os.path.join(dirpath, filename)
                found.add(os.path.normpath(os.path.relpath(path, directory)))
    return found


def main():
    with open("KRPC.sln", encoding="utf-8") as f:
        projects = [p.replace("\\", "/") for p in SLN_PROJECT.findall(f.read())]
    if not projects:
        sys.exit("no projects found in KRPC.sln")

    errors = []
    for csproj in sorted(projects):
        # The sources reach this test through a per-package filegroup. A project
        # added to the solution without one would otherwise be skipped silently.
        if not os.path.exists(csproj):
            errors.append(
                "%s: not in the test's inputs -- add the package to the srcs of "
                "//:csproj-test" % csproj
            )
            continue
        declared = compile_includes(csproj)
        found = sources_on_disk(os.path.dirname(csproj))
        for path in sorted(found - declared):
            errors.append(
                "%s: %s exists but is not listed -- add "
                '<Compile Include="%s" />' % (csproj, path, path.replace("/", "\\"))
            )
        for path in sorted(declared - found):
            errors.append("%s: %s is listed but does not exist" % (csproj, path))

    if errors:
        print("\n".join(errors), file=sys.stderr)
        sys.exit(
            "\n%d problem(s) found. The Bazel build globs its sources, so these "
            "break only `dotnet build KRPC.sln`." % len(errors)
        )
    print("checked %d projects" % len(projects))


if __name__ == "__main__":
    main()
