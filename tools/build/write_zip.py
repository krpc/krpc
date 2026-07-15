"""Writes a deterministic zip archive.

  write_zip.py --out OUT [--entry SRC=ARCNAME]... [--tree DIR]...

--entry adds a single file as ARCNAME. --tree adds every file under DIR at its
path relative to DIR. Entries are sorted and stamped with a fixed timestamp so
the archive is reproducible, with no dependency on a system `zip`.
"""

import argparse
import os
import sys
import zipfile

# Fixed timestamp (the zip epoch) for reproducible archives.
FIXED_DATE = (1980, 1, 1, 0, 0, 0)


def _add(archive, src, arcname):
    info = zipfile.ZipInfo(arcname.replace(os.sep, "/"), date_time=FIXED_DATE)
    info.compress_type = zipfile.ZIP_DEFLATED
    info.external_attr = 0o644 << 16
    with open(src, "rb") as handle:
        archive.writestr(info, handle.read())


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", required=True)
    parser.add_argument("--entry", action="append", default=[])
    parser.add_argument("--tree", action="append", default=[])
    opts = parser.parse_args()

    entries = []
    for spec in opts.entry:
        src, arcname = spec.split("=", 1)
        entries.append((arcname, src))
    for tree in opts.tree:
        for root, _, files in os.walk(tree):
            for name in files:
                full = os.path.join(root, name)
                entries.append((os.path.relpath(full, tree), full))
    entries.sort()

    with zipfile.ZipFile(opts.out, "w") as archive:
        for arcname, src in entries:
            _add(archive, src, arcname)
    return 0


if __name__ == "__main__":
    sys.exit(main())
