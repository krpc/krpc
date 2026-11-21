"""Installs the kRPC for KSA mod into the KSA mods folder, and enables it.

Usage:
  bazel run //experimental/ksa/tools/game:install [-- --testing]

--testing installs the mod with the TestingTools service and the test settings. KSA_MODS_DIR
is the mods folder, defaulting to "<Documents>/My Games/Kitten Space Agency/mods".
"""

import argparse

import mods_folder
from python.runfiles import runfiles


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--mod", required=True)
    parser.add_argument("--mod-testing", required=True)
    parser.add_argument("--testing", action="store_true")
    args = parser.parse_args()

    r = runfiles.Create()
    if r is None:
        raise RuntimeError("runfiles not found")
    archive = r.Rlocation(args.mod_testing if args.testing else args.mod)
    if archive is None:
        raise RuntimeError("mod archive not found in the runfiles")
    print(f"Installed into {mods_folder.install(archive)}")


if __name__ == "__main__":
    main()
