#!/usr/bin/env python3
"""Builds the archive of generated sources that ships with a release.

Collects the generated C# sources and the assemblies they are built against,
so that the C# projects can be opened without running the Bazel build.
"""

import glob
import os
import zipfile
from pathlib import Path

import lib

# Each is required: an entry matching nothing means the build has moved and the
# archive would silently ship incomplete.
CONTENTS = (
    'bazel-bin/core/AssemblyInfo.cs',
    'bazel-bin/core/TestAssemblyInfo.cs',
    'bazel-bin/server/AssemblyInfo.cs',
    'bazel-bin/protobuf/KRPC_unity.cs',
    'bazel-bin/client/csharp/AssemblyInfo.cs',
    'bazel-bin/protobuf/KRPC.cs',
    'bazel-bin/client/csharp/Services/',
    'bazel-bin/service/*/AssemblyInfo.cs',
    'bazel-bin/tools/*/AssemblyInfo.cs',
    'bazel-krpc/external/+http_archive+csharp_json',
    'bazel-krpc/external/+http_archive+csharp_moq',
    'bazel-krpc/external/+http_archive+csharp_options',
    'bazel-krpc/external/+http_archive+ksp/KSP_Data/Managed',
    'bazel-bin/tools/build/ksp/Google.Protobuf.dll',
    'bazel-bin/tools/build/ksp/KRPC.IO.Ports.dll',
)


def add(archive, path):
    """Add a file, or every file under a directory, at its own path.

    Symbolic links are followed and stored as the files they point at; the
    Bazel output paths are reached through them.
    """
    if path.is_dir():
        for directory, _, names in os.walk(path, followlinks=True):
            for name in sorted(names):
                child = Path(directory, name)
                if child.is_file():
                    archive.write(child, child.as_posix())
    else:
        archive.write(path, path.as_posix())


def main():
    lib.require('bazel')
    lib.run('bazel', 'build', '//:csproj')

    output = Path(f'bazel-bin/krpc-genfiles-{lib.VERSION}.zip')
    output.unlink(missing_ok=True)
    with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED) as archive:
        for pattern in CONTENTS:
            matches = sorted(glob.glob(pattern))
            if not matches:
                raise lib.ReleaseError(f'nothing matched {pattern}')
            for match in matches:
                add(archive, Path(match))
    print(f'Wrote {output}')


if __name__ == '__main__':
    lib.main(main)
