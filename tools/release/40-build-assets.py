#!/usr/bin/env python3
"""Builds all the release assets and collects them in the assets/ directory,
ready for uploading to the GitHub release.
"""

import hashlib
import shutil
from pathlib import Path

import genfiles
import lib

TARGETS = (
    '//:krpc', '//client/cnano', '//client/cpp', '//client/csharp',
    '//client/java', '//client/lua', '//client/python', '//doc:pdf',
    '//tools/krpctools', '//tools/TestServer:archive',
)


def assets():
    return [Path(path.format(version=lib.VERSION)) for path in (
        'bazel-bin/krpc-{version}.zip',
        'bazel-bin/client/cnano/krpc-cnano-{version}.zip',
        'bazel-bin/client/cpp/krpc-cpp-{version}.zip',
        'bazel-bin/client/csharp/krpc-csharp-{version}.zip',
        'bazel-bin/client/java/krpc-java-{version}.jar',
        'bazel-bin/client/lua/krpc-lua-{version}.zip',
        'bazel-bin/client/python/krpc-python-{version}.tar.gz',
        'bazel-bin/doc/krpc-doc-{version}.pdf',
        'bazel-bin/tools/krpctools/krpctools-{version}.tar.gz',
        'bazel-bin/tools/TestServer/TestServer-{version}.zip',
        'bazel-bin/krpc-genfiles-{version}.zip',
    )]


def main():
    lib.require('bazel')
    files = assets()

    lib.banner('Building the release assets')
    lib.run('bazel', 'build', *TARGETS)
    genfiles.main()

    lib.banner('Collecting assets into assets/')
    directory = Path('assets')
    if directory.is_dir() and any(directory.iterdir()):
        lib.confirm('assets/ is not empty and will be replaced. Continue?')
    shutil.rmtree(directory, ignore_errors=True)
    directory.mkdir()
    for asset in files:
        if not asset.is_file():
            raise lib.ReleaseError(f'{asset} was not built')
        shutil.copy(asset, directory)

    # Same format as sha256sum, so the file can be checked with it. Written
    # over the assets alone, never over the checksum file itself.
    checksums = directory / f'SHA256SUMS-{lib.VERSION}.txt'
    lines = []
    for name in sorted(path.name for path in files):
        digest = hashlib.sha256((directory / name).read_bytes()).hexdigest()
        lines.append(f'{digest}  {name}')
    checksums.write_text('\n'.join(lines) + '\n')

    lib.banner('Done')
    for path in sorted(directory.iterdir()):
        print(f'{path.stat().st_size:>12}  {path.name}')
    print()
    print(f'{len(files) + 1} files ({len(files)} assets + checksums).')
    print('Next: tools/release/50-release-github.py')


if __name__ == '__main__':
    lib.main(main)
