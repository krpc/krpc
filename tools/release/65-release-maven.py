#!/usr/bin/env python3
"""Publishes the Java client to Maven Central.

Signs the jar, POM, sources jar and javadoc jar, uploads them to the Central
Portal as a bundle, and publishes the bundle once the Central Portal has
validated it. Requires gpg, MAVEN_CENTRAL_USERNAME, MAVEN_CENTRAL_PASSWORD,
MAVEN_GPG_KEY and MAVEN_GPG_PASSPHRASE.
"""

import tempfile

import lib
import maven


def build_bundle(credentials, directory):
    """Check the credentials, build and test the client, and sign the bundle."""
    # The credentials are checked before the build, so a bad one fails in seconds
    coordinates = f"{maven.GROUP}:{maven.ARTIFACT}:{lib.VERSION}"
    if maven.published(credentials):
        raise lib.ReleaseError(f"{coordinates} is already on Maven Central")
    with maven.gpg(credentials) as (sign, fingerprint):
        maven.check_signing_key(sign, fingerprint)
        print(f"signing with key {fingerprint}")

        lib.banner("Building and testing the Java client")
        lib.run(
            "bazel",
            "build",
            "//client/java:krpc-jar",
            "//client/java:krpc-pom",
            "//client/java:krpc-sources",
            "//client/java:krpc-docs",
        )
        lib.run("bazel", "test", "//client/java:test", "--cache_test_results=no")

        lib.banner("Signing the bundle")
        return maven.bundle(sign, directory)


def upload(credentials, archive):
    lib.banner("Uploading the bundle to the Central Portal for validation")
    try:
        return maven.upload(credentials, archive)
    except BaseException:
        # The portal may have stored the bundle before the failure
        lib.warning(
            f"check {maven.DEPLOYMENTS_PAGE} for a deployment this upload left "
            f"behind, and drop it before re-running this step"
        )
        raise


def publish(credentials, deployment):
    """Publish a deployment, returning whether the portal is publishing it.

    A failed request may still have reached the portal, so the deployment is
    dropped only if it is still validated.
    """
    try:
        maven.publish(credentials, deployment)
        return True
    except lib.ReleaseError as exception:
        state = maven.status(credentials, deployment)["deploymentState"]
        if state in ("PUBLISHING", "PUBLISHED"):
            lib.warning(f"{exception}, but the deployment is {state}")
            return True
        raise


def main():
    lib.require("bazel", "gpg", "gpgconf")
    credentials = lib.load_credentials()
    coordinates = f"{maven.GROUP}:{maven.ARTIFACT}:{lib.VERSION}"

    with tempfile.TemporaryDirectory() as directory:
        archive = build_bundle(credentials, directory)
        deployment = upload(credentials, archive)

    # Until it is published, a deployment left behind by a failure or an
    # interrupt is dropped, so a re-run starts from nothing
    try:
        maven.wait(credentials, deployment, {"VALIDATED"})
        lib.banner("Publishing to Maven Central")
        lib.confirm(f"Publish {coordinates} to Maven Central? This cannot be undone.")
        publish(credentials, deployment)
    except BaseException:
        maven.drop(credentials, deployment)
        raise

    try:
        maven.wait(credentials, deployment, {"PUBLISHED"})
    except maven.DeploymentTimeout as exception:
        # Published already: the deployment goes out without the step
        lib.warning(f"{exception}. Do not re-run this step.")
        return
    print("Published. It reaches https://repo1.maven.org/maven2/ within the hour.")


if __name__ == "__main__":
    lib.main(main)
