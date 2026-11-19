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
    "bazel-bin/core/AssemblyInfo.cs",
    "bazel-bin/core/TestAssemblyInfo.cs",
    "bazel-bin/server/AssemblyInfo.cs",
    "bazel-bin/protobuf/KRPC_unity.cs",
    "bazel-bin/client/csharp/AssemblyInfo.cs",
    "bazel-bin/protobuf/KRPC.cs",
    "bazel-bin/client/csharp/Services/",
    "bazel-bin/service/*/AssemblyInfo.cs",
    "bazel-bin/service/*/ServiceInfo.cs",
    "bazel-bin/tools/*/AssemblyInfo.cs",
    "bazel-bin/tools/build/ksp/Google.Protobuf.dll",
)

# Paths under the external repositories of the output base, stored under
# bazel-krpc/external, where the .csproj files look for them
EXTERNAL = ("+http_archive+ksp/KSP_Data/Managed",)
EXTERNAL_ARCHIVE_DIR = Path("bazel-krpc/external")


def add(archive, path, name):
    """Add a file, or every file under a directory, stored at name.

    Symbolic links are followed and stored as the files they point at; the
    Bazel output paths are reached through them.
    """
    if path.is_dir():
        for directory, _, names in os.walk(path, followlinks=True):
            for child_name in sorted(names):
                child = Path(directory, child_name)
                if child.is_file():
                    archive.write(child, (name / child.relative_to(path)).as_posix())
    else:
        archive.write(path, name.as_posix())


def add_matches(archive, pattern, base, archive_dir):
    """Add every path under base matching pattern, stored under archive_dir."""
    matches = sorted(glob.glob(pattern, root_dir=base))
    if not matches:
        raise lib.ReleaseError(f"nothing matched {pattern} in {base}")
    for match in matches:
        add(archive, base / match, archive_dir / match)


def main():
    lib.require("bazel")
    lib.run("bazel", "build", "//:csproj")

    # The bazel-<folder> symlink is named for the checkout, so the external
    # repositories are found through the output base
    external = Path(lib.capture("bazel", "info", "output_base")) / "external"

    output = Path(f"bazel-bin/krpc-genfiles-{lib.VERSION}.zip")
    output.unlink(missing_ok=True)
    with zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED) as archive:
        for pattern in CONTENTS:
            add_matches(archive, pattern, Path("."), Path())
        for pattern in EXTERNAL:
            add_matches(archive, pattern, external, EXTERNAL_ARCHIVE_DIR)
    print(f"Wrote {output}")


if __name__ == "__main__":
    lib.main(main)
