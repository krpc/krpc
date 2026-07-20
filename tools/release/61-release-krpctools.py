#!/usr/bin/env python3
"""Publishes krpctools to PyPI.

Requires twine and PYPI_TOKEN_KRPCTOOLS.
"""

import lib


def main():
    lib.require('bazel', 'twine')
    credentials = lib.load_credentials()

    lib.banner('Building and testing krpctools')
    lib.run('bazel', 'build', '//tools/krpctools')
    lib.run('bazel', 'test', '//tools/krpctools:test',
            '--cache_test_results=no')

    lib.banner('Uploading to PyPI')
    lib.confirm(f'Upload krpctools {lib.VERSION} to PyPI? '
                f'(uploads cannot be replaced)')
    lib.twine_upload(credentials['PYPI_TOKEN_KRPCTOOLS'],
                     f'bazel-bin/tools/krpctools/krpctools-{lib.VERSION}.tar.gz')


if __name__ == '__main__':
    lib.main(main)
