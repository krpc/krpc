#!/usr/bin/env python3
"""Publishes the Lua client.

Uploads the source archive to S3, where the rockspec's source URL points, and
the rockspec to luarocks.org. Requires AWS_ACCESS_KEY_ID and
AWS_SECRET_ACCESS_KEY for s3://krpc, and LUAROCKS_API_KEY.
"""

import glob
import os
import tempfile
import zipfile

import lib


def check_modules_install(rockspec, archive):
    """Build the rockspec into a throwaway tree and check the modules landed.

    The rockspec names no modules, so luarocks finds them by scanning the source
    it downloads, and installs nothing at all, without complaining, when it
    finds none. So a source layout the scan cannot reach shows up as a rock that
    installs but provides nothing, which is what comparing the tree against the
    archive catches.

    Only the client's own modules are built. Its dependencies are what luarocks
    resolves on the machine installing the rock, rather than part of what is
    published here.
    """
    with tempfile.TemporaryDirectory() as tree:
        lib.run('luarocks', f'--tree={tree}', 'build', '--deps-mode=none',
                rockspec)
        installed = {
            os.path.relpath(module, root).replace(os.sep, '/')
            for root in glob.glob(f'{tree}/share/lua/*')
            for module in glob.glob(f'{root}/**/*.lua', recursive=True)
        }

    prefix = f'krpc-{lib.VERSION}/lua/'
    with zipfile.ZipFile(archive) as source:
        expected = {name[len(prefix):] for name in source.namelist()
                    if name.startswith(prefix) and name.endswith('.lua')}

    if installed != expected:
        raise lib.ReleaseError(
            f'the rock installs {len(installed)} of the {len(expected)} '
            f'modules the archive holds. '
            f'Missing: {sorted(expected - installed)}. '
            f'Unexpected: {sorted(installed - expected)}')
    print(f'{len(installed)} modules install as expected')


def main():
    lib.require('bazel', 'aws', 'luarocks')
    credentials = lib.load_credentials()

    lib.banner('Building and testing the Lua client')
    lib.run('bazel', 'build', '//client/lua')
    lib.run('bazel', 'test', '//client/lua:test', '--cache_test_results=no')

    archive = f'bazel-bin/client/lua/krpc-{lib.VERSION}.zip'
    rockspec = f'bazel-bin/client/lua/krpc-{lib.VERSION}-0.rockspec'

    lib.banner('Uploading the source archive to S3')
    lib.confirm(f'Upload krpc-{lib.VERSION}.zip to s3://krpc/lua/?')
    lib.run('aws', 's3', 'cp', archive,
            f's3://krpc/lua/krpc-{lib.VERSION}.zip',
            env=lib.aws_environment(credentials))

    lib.banner('Checking the rockspec')
    # The build has no use for luarocks, so these are the first things to read
    # the rockspec as one rather than as a file to copy. They run after the
    # upload because what they check it against is the archive it points at.
    lib.run('luarocks', 'lint', rockspec)
    check_modules_install(rockspec, archive)

    lib.banner('Publishing the rockspec to luarocks.org')
    lib.confirm(f'Upload the krpc {lib.VERSION} rockspec to luarocks.org?')
    # --temp-key, rather than --api-key, so the key is used for this upload
    # only and not written into the luarocks configuration
    lib.run('luarocks', 'upload', '--skip-pack',
            '--temp-key', credentials['LUAROCKS_API_KEY'],
            rockspec)


if __name__ == '__main__':
    lib.main(main)
