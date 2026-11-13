#!/usr/bin/env python3
"""Publishes the JavaScript client to npm.

Requires npm and NPM_TOKEN.
"""

import os
import tempfile
from pathlib import Path

import lib


def environment(npmrc):
    """The environment for npm, reading only the configuration file given.

    npm takes settings from npm_config_* variables over its configuration
    files, so those are removed.
    """
    result = {
        key: value
        for key, value in os.environ.items()
        if not key.lower().startswith("npm_config_")
    }
    result["NPM_CONFIG_USERCONFIG"] = str(npmrc)
    return result


def main():
    lib.require("bazel", "npm")
    credentials = lib.load_credentials()

    if lib.npm_published():
        raise lib.ReleaseError(f"{lib.NPM_PACKAGE} {lib.VERSION} is already on npm")

    lib.banner("Building and testing the JavaScript client")
    lib.run("bazel", "build", "//client/js")
    lib.run("bazel", "test", "//client/js:test", "--cache_test_results=no")

    lib.banner("Publishing to npm")
    user = lib.npm_user(credentials["NPM_TOKEN"])
    lib.confirm(f"Publish {lib.NPM_PACKAGE} {lib.VERSION} to npm as {user}?")
    with tempfile.TemporaryDirectory() as directory:
        npmrc = Path(directory) / ".npmrc"
        npmrc.touch(mode=0o600)
        registry = lib.NPM_REGISTRY.removeprefix("https:")
        npmrc.write_text(f"{registry}/:_authToken={credentials['NPM_TOKEN']}\n")
        # A scoped package is private unless published with public access
        lib.run(
            "npm",
            "publish",
            f"bazel-bin/client/js/krpc-js-{lib.VERSION}.tgz",
            "--access",
            "public",
            "--registry",
            lib.NPM_REGISTRY + "/",
            env=environment(npmrc),
        )


if __name__ == "__main__":
    lib.main(main)
