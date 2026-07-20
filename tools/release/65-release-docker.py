#!/usr/bin/env python3
"""Builds the TestServer docker image and pushes it to ghcr.io.

Requires docker and a GITHUB_TOKEN with the write:packages scope.
"""

import os
import tempfile

import lib


def main():
    lib.require('docker', 'make', 'bazel')
    credentials = lib.load_credentials()
    token = credentials['GITHUB_TOKEN']

    lib.banner('Building the TestServer image')
    lib.run('make', '-C', 'tools/TestServer/docker', 'build')

    # Log in to ghcr.io in a throwaway config directory, so the push neither
    # depends on nor disturbs the docker login in ~/.docker. make inherits
    # DOCKER_CONFIG, so the deploy target uses the same login.
    with tempfile.TemporaryDirectory() as config:
        environment = dict(os.environ, DOCKER_CONFIG=config)

        lib.banner('Logging in to ghcr.io')
        login, _ = lib.github_user(token)
        lib.run('docker', 'login', 'ghcr.io', '-u', login,
                '--password-stdin', env=environment, stdin=token)

        lib.banner('Pushing to ghcr.io')
        print('To try the image first: '
              'make -C tools/TestServer/docker test')
        lib.confirm(f'Push ghcr.io/krpc/testserver:{lib.VERSION} and :latest?')
        lib.run('make', '-C', 'tools/TestServer/docker', 'deploy',
                env=environment)


if __name__ == '__main__':
    lib.main(main)
