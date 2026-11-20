#!/usr/bin/env python3
"""Publishes the JavaScript client on npm, by approving the version staged by
69-stage-npm.py.

npm asks for the maintainer's 2FA in the terminal. npm refuses the approval
with a 409 error until its automated review of the staged package finishes, a
few minutes after staging, so wait and re-run the step. Requires npm 11.15.0 or
later, for `npm stage`.
"""

import lib
import npm


def main():
    npm.check_npm()
    credentials = lib.load_credentials()

    with npm.environment(credentials["NPM_TOKEN"]) as env:
        stage_id = npm.staged_id(env)
        if not stage_id:
            raise lib.ReleaseError(
                f"{lib.NPM_PACKAGE} {lib.VERSION} is not staged "
                f"(run 69-stage-npm.py first)"
            )

        lib.banner("Publishing to npm")
        lib.confirm(
            f"Approve and publish {lib.NPM_PACKAGE} {lib.VERSION} ({stage_id})? "
            f"npm asks for your 2FA"
        )
        npm.approve(env, stage_id)


if __name__ == "__main__":
    lib.main(main)
