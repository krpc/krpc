#!/usr/bin/env python3
"""Updates the krpc-arduino repository from the C-nano client and tags a new
release of it.

The Arduino library is a rearrangement of the C-nano client: its sources and
the nanopb runtime the C-nano archive depends on, laid out the way the Arduino
builder expects. The krpc-arduino working copy is kept between runs, so a
release only commits and tags the difference.

Uses the normal development git setup, which needs push access to
github.com/krpc/krpc-arduino.
"""

import re
import shutil
import zipfile
from pathlib import Path

import lib

# nanopb runtime files the Arduino library vendors. The C-nano archive takes
# nanopb as an external dependency rather than bundling it, so it is copied in
# separately: headers beside the C-nano headers in krpc_cnano/ (they are
# included as <krpc_cnano/pb.h>), sources at the top level with the other
# C-nano sources.
NANOPB_HEADERS = ('pb.h', 'pb_common.h', 'pb_encode.h', 'pb_decode.h')
NANOPB_SOURCES = ('pb_common.c', 'pb_encode.c', 'pb_decode.c')


def nanopb_files():
    """Absolute paths of the nanopb runtime files, keyed by file name.

    Their location inside Bazel's external repository tree is obtained from
    Bazel rather than assembled from the mangled repository name by hand, which
    changes between Bazel versions.
    """
    output = lib.capture('bazel', 'query',
                         'kind("source file", deps(@c_nanopb//:srcs))',
                         '--output=location')
    files = {}
    for line in output.splitlines():
        match = re.match(r'(.*):\d+:\d+: source file @c_nanopb//:(.+)', line)
        if match:
            files[match.group(2)] = Path(match.group(1))
    missing = [name for name in NANOPB_HEADERS + NANOPB_SOURCES
               if name not in files]
    if missing:
        raise lib.ReleaseError(
            f'nanopb runtime is missing {" ".join(missing)}')
    return files


def rebuild_source(arduino, archive_path, nanopb):
    """Replace the Arduino library's src/ with a freshly built C-nano tree.

    Arduino compiles every source under src/ with a flat include path, so the
    library is laid out to suit: the C-nano headers keep their krpc_cnano/
    subdirectory (they are included as <krpc_cnano/...>), the sources sit
    directly in src/, and .c files are renamed to .cpp because the Arduino
    builder only compiles C++.
    """
    source = arduino / 'src'
    shutil.rmtree(source, ignore_errors=True)
    source.mkdir()

    # Unpack the C-nano archive and lift the contents of its include/ and src/
    # directories into place, discarding the rest of the package.
    with zipfile.ZipFile(archive_path) as archive:
        archive.extractall(source)
    unpacked = source / f'krpc-cnano-{lib.VERSION}'
    for entry in (unpacked / 'include').iterdir():
        shutil.move(entry, source)
    for entry in (unpacked / 'src').glob('*.c'):
        shutil.move(entry, source)
    shutil.rmtree(unpacked)

    # Vendor the nanopb runtime.
    for name in NANOPB_HEADERS:
        shutil.copy(nanopb[name], source / 'krpc_cnano')
    for name in NANOPB_SOURCES:
        shutil.copy(nanopb[name], source)

    # The Arduino builder only compiles C++, so give sources a .cpp suffix.
    for entry in list(source.glob('*.c')):
        entry.rename(entry.with_suffix('.cpp'))

    # Build nanopb without error-message strings by default, to save flash on
    # the microcontroller.
    header = source / 'krpc_cnano' / 'pb.h'
    header.write_text('#ifndef PB_NO_ERRMSG\n#define PB_NO_ERRMSG\n#endif\n\n'
                      + header.read_text())


def set_version(arduino):
    """Set the version in the library's library.properties to the release."""
    properties = arduino / 'library.properties'
    properties.write_text(re.sub(r'(?m)^version=.*$',
                                 f'version={lib.VERSION}',
                                 properties.read_text()))


def main():
    lib.require('bazel', 'git')
    commit = lib.capture('git', 'rev-parse', 'HEAD')

    lib.banner('Building the C-nano library and nanopb runtime')
    lib.run('bazel', 'build', '//client/cnano', '@c_nanopb//:srcs')
    nanopb = nanopb_files()
    bin_dir = Path(lib.capture('bazel', 'info', 'bazel-bin'))
    cnano = bin_dir / 'client' / 'cnano'
    archive_path = cnano / f'krpc-cnano-{lib.VERSION}.zip'
    if not archive_path.is_file():
        raise lib.ReleaseError(f'{archive_path} was not built')

    # The working copy is kept beside the C-nano build outputs, under a name
    # that does not collide with the 'arduino' test binary Bazel builds there.
    arduino = cnano / 'arduino-library'
    if not arduino.is_dir():
        lib.banner('Cloning the krpc-arduino repository')
        lib.run('git', 'clone', 'git@github.com:krpc/krpc-arduino', arduino)

    lib.banner('Rebuilding the Arduino library source')
    rebuild_source(arduino, archive_path, nanopb)
    set_version(arduino)
    print(f'Arduino library source in {arduino}')

    lib.banner('Updating the Arduino library repository')
    lib.confirm('Push updated library source to krpc/krpc-arduino?')
    lib.run('git', 'add', '.', cwd=arduino)
    if lib.capture('git', 'status', '--porcelain', cwd=arduino):
        lib.run('git', 'commit', '-m',
                f'Updated from https://github.com/krpc/krpc commit {commit}',
                cwd=arduino)
    lib.run('git', 'push', 'origin', 'main', cwd=arduino)

    lib.banner('Tagging the Arduino library release')
    lib.confirm(f'Tag and push krpc-arduino {lib.TAG}?')
    lib.run('git', 'tag', '-a', lib.TAG, '-m', lib.TAG, cwd=arduino)
    lib.run('git', 'push', '--tags', cwd=arduino)


if __name__ == '__main__':
    lib.main(main)
