#!/usr/bin/env python3
"""Publishes the Python client to PyPI.

Requires twine and PYPI_TOKEN_KRPC.
"""

import lib


def main():
    lib.require('bazel', 'twine')
    credentials = lib.load_credentials()

    lib.banner('Building and testing the Python client')
    lib.run('bazel', 'build', '//client/python:python-pypi')
    lib.run('bazel', 'test', '//client/python:test', '--cache_test_results=no')

    lib.banner('Uploading to PyPI')
    lib.confirm(f'Upload krpc {lib.VERSION} to PyPI? '
                f'(uploads cannot be replaced)')
    lib.twine_upload(credentials['PYPI_TOKEN_KRPC'],
                     f'bazel-bin/client/python/krpc-{lib.VERSION}.tar.gz')


if __name__ == '__main__':
    lib.main(main)
