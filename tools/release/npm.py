"""Staging and approving the JavaScript client on npm.

The package is staged with NPM_TOKEN, then approved with the maintainer's 2FA.
npm refuses the approval until its automated review of the staged package
finishes, so the two are separate steps.
"""

import contextlib
import json
import os
import tempfile
from pathlib import Path

import lib


def check_npm():
    lib.require("npm")
    if not lib.npm_supports_staging():
        raise lib.ReleaseError(
            f"npm {lib.NPM_MINIMUM} or later is needed, for npm stage"
        )
    if lib.npm_published():
        raise lib.ReleaseError(f"{lib.NPM_PACKAGE} {lib.VERSION} is already on npm")


@contextlib.contextmanager
def environment(token):
    """The environment for npm, authenticated as a token.

    npm reads only a temporary configuration file holding the token. It takes
    settings from npm_config_* variables over its configuration files, so those
    are removed.
    """
    with tempfile.TemporaryDirectory() as directory:
        npmrc = Path(directory) / ".npmrc"
        npmrc.touch(mode=0o600)
        registry = lib.NPM_REGISTRY.removeprefix("https:")
        npmrc.write_text(f"{registry}/:_authToken={token}\n")
        result = {
            key: value
            for key, value in os.environ.items()
            if not key.lower().startswith("npm_config_")
        }
        result["NPM_CONFIG_USERCONFIG"] = str(npmrc)
        yield result


def staged_id(env):
    """The stage id of this version, if it is staged."""
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


def approve(env, stage_id):
    lib.run(
        "npm",
        "stage",
        "approve",
        stage_id,
        "--registry",
        lib.NPM_REGISTRY + "/",
        env=env,
    )
