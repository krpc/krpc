#!/usr/bin/env python3
"""Publishes the JavaScript client to npm.

The package is staged with NPM_TOKEN, then approved with the maintainer's 2FA,
which npm asks for in the terminal. Requires npm 11.15.0 or later, for
`npm stage`.
"""

import json
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


def staged_id(env):
    """The stage id of this version, if an earlier run staged it."""
    output = lib.capture(
        "npm",
        "stage",
        "list",
        lib.NPM_PACKAGE,
        "--json",
        "--registry",
        lib.NPM_REGISTRY + "/",
        env=env,
    )
    for item in json.loads(output):
        if (
            item.get("packageName") == lib.NPM_PACKAGE
            and item.get("version") == lib.VERSION
        ):
            return item["id"]
    return None


def stage(env):
    """Stage the package, and return its stage id."""
    output = lib.capture(
        "npm",
        "stage",
        "publish",
        Path(f"bazel-bin/client/js/krpc-js-{lib.VERSION}.tgz").resolve(),
        # A scoped package is private unless published with public access
        "--access",
        "public",
        "--json",
        "--registry",
        lib.NPM_REGISTRY + "/",
        env=env,
    )
    try:
        return json.loads(output)[lib.NPM_PACKAGE]["stageId"]
    except (ValueError, KeyError) as exception:
        raise lib.ReleaseError(f"npm returned no stage id:\n{output}") from exception


def main():
    lib.require("bazel", "npm")
    if not lib.npm_supports_staging():
        raise lib.ReleaseError(
            f"npm {lib.NPM_MINIMUM} or later is needed, for npm stage"
        )
    credentials = lib.load_credentials()

    if lib.npm_published():
        raise lib.ReleaseError(f"{lib.NPM_PACKAGE} {lib.VERSION} is already on npm")

    lib.banner("Building and testing the JavaScript client")
    lib.run("bazel", "build", "//client/js")
    lib.run("bazel", "test", "//client/js:test", "--cache_test_results=no")

    user = lib.npm_user(credentials["NPM_TOKEN"])
    with tempfile.TemporaryDirectory() as directory:
        npmrc = Path(directory) / ".npmrc"
        npmrc.touch(mode=0o600)
        registry = lib.NPM_REGISTRY.removeprefix("https:")
        npmrc.write_text(f"{registry}/:_authToken={credentials['NPM_TOKEN']}\n")
        env = environment(npmrc)

        lib.banner("Staging on npm")
        stage_id = staged_id(env)
        if stage_id:
            print(f"{lib.NPM_PACKAGE} {lib.VERSION} is already staged, as {stage_id}")
        else:
            lib.confirm(f"Stage {lib.NPM_PACKAGE} {lib.VERSION} on npm as {user}?")
            stage_id = stage(env)
            print(f"Staged as {stage_id}")

        lib.banner("Publishing to npm")
        lib.confirm(
            f"Approve and publish {lib.NPM_PACKAGE} {lib.VERSION}? "
            f"npm asks for your 2FA"
        )
        lib.run(
            "npm",
            "stage",
            "approve",
            stage_id,
            "--registry",
            lib.NPM_REGISTRY + "/",
            env=env,
        )


if __name__ == "__main__":
    lib.main(main)
