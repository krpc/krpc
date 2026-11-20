#!/usr/bin/env python3
"""Stages the JavaScript client on npm.

Builds and tests the client, then stages it with NPM_TOKEN. 70-approve-npm.py
publishes it, once npm's automated review of the staged package finishes.
Requires npm 11.15.0 or later, for `npm stage`.
"""

import lib
import npm


def main():
    lib.require("bazel")
    npm.check_npm()
    credentials = lib.load_credentials()

    lib.banner("Building and testing the JavaScript client")
    lib.run("bazel", "build", "//client/js")
    lib.run("bazel", "test", "//client/js:test", "--cache_test_results=no")

    user = lib.npm_user(credentials["NPM_TOKEN"])
    with npm.environment(credentials["NPM_TOKEN"]) as env:
        lib.banner("Staging on npm")
        stage_id = npm.staged_id(env)
        if stage_id:
            print(f"{lib.NPM_PACKAGE} {lib.VERSION} is already staged, as {stage_id}")
        else:
            lib.confirm(f"Stage {lib.NPM_PACKAGE} {lib.VERSION} on npm as {user}?")
            stage_id = npm.stage(env)
            print(f"Staged as {stage_id}")

    lib.banner("Done")
    print("npm reviews the staged package, which takes a few minutes.")
    print("Next: tools/release/70-approve-npm.py, once the review finishes")


if __name__ == "__main__":
    lib.main(main)
