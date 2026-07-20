#!/usr/bin/env python3
"""Publishes the C# client to nuget.org.

Requires the .NET SDK and NUGET_API_KEY.
"""

import lib


def main():
    lib.require('bazel', 'dotnet')
    credentials = lib.load_credentials()

    lib.banner('Building and testing the C# client')
    lib.run('bazel', 'build', '//client/csharp:nuget')
    lib.run('bazel', 'test', '//client/csharp:test', '--cache_test_results=no')

    lib.banner('Uploading to nuget.org')
    lib.confirm(f'Push KRPC.Client {lib.VERSION} to nuget.org?')
    lib.run('dotnet', 'nuget', 'push',
            f'bazel-bin/client/csharp/KRPC.Client.{lib.VERSION}.nupkg',
            '--source', 'https://api.nuget.org/v3/index.json',
            '--api-key', credentials['NUGET_API_KEY'])


if __name__ == '__main__':
    lib.main(main)
