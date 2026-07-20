#!/usr/bin/env python3
"""Publishes the Lua client.

Uploads the source archive to S3, where the rockspec's source URL points, and
the rockspec to luarocks.org. Requires AWS_ACCESS_KEY_ID and
AWS_SECRET_ACCESS_KEY for s3://krpc, and LUAROCKS_API_KEY.
"""

import lib


def main():
    lib.require('bazel', 'aws', 'luarocks')
    credentials = lib.load_credentials()

    lib.banner('Building and testing the Lua client')
    lib.run('bazel', 'build', '//client/lua')
    lib.run('bazel', 'test', '//client/lua:test', '--cache_test_results=no')

    lib.banner('Uploading the source archive to S3')
    lib.confirm(f'Upload krpc-{lib.VERSION}.zip to s3://krpc/lua/?')
    lib.run('aws', 's3', 'cp',
            f'bazel-bin/client/lua/krpc-{lib.VERSION}.zip',
            f's3://krpc/lua/krpc-{lib.VERSION}.zip',
            env=lib.aws_environment(credentials))

    lib.banner('Publishing the rockspec to luarocks.org')
    lib.confirm(f'Upload the krpc {lib.VERSION} rockspec to luarocks.org?')
    # --temp-key, rather than --api-key, so the key is used for this upload
    # only and not written into the luarocks configuration
    lib.run('luarocks', 'upload', '--skip-pack',
            '--temp-key', credentials['LUAROCKS_API_KEY'],
            f'bazel-bin/client/lua/krpc-{lib.VERSION}-0.rockspec')


if __name__ == '__main__':
    lib.main(main)
