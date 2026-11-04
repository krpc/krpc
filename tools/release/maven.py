"""Signing and uploading the Java client to Maven Central.

Maven Central takes a bundle: a zip holding each file of a release in the
Maven repository layout, each with an ASCII-armored GPG signature and MD5 and
SHA-1 checksums. The bundle goes to the Central Portal, which validates it and
holds it until it is published. A published version cannot be removed or
replaced.

https://central.sonatype.org/publish/publish-portal-api/
"""

import base64
import contextlib
import hashlib
import json
import os
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
import zipfile
from pathlib import Path

import lib

GROUP = "io.github.krpc"
ARTIFACT = "krpc-java"

PORTAL = "https://central.sonatype.com/api/v1/publisher"
DEPLOYMENTS_PAGE = "https://central.sonatype.com/publishing/deployments"

CREDENTIAL_KEYS = tuple(key for key in lib.CREDENTIAL_KEYS if key.startswith("MAVEN_"))


class DeploymentTimeout(lib.ReleaseError):
    """A deployment did not reach the state waited for in time."""


# The key servers the Central Portal looks up signing keys on. A key on any
# one of them is enough.
KEY_SERVERS = (
    "https://keys.openpgp.org/vks/v1/by-fingerprint/{fingerprint}",
    "https://keyserver.ubuntu.com/pks/lookup?op=get&search=0x{fingerprint}",
)


def files():
    """The files of the release, by their name in the bundle."""
    stem = f"{ARTIFACT}-{lib.VERSION}"
    return {
        f"{stem}.jar": Path(f"bazel-bin/client/java/{stem}.jar"),
        f"{stem}.pom": Path("bazel-bin/client/java/krpc-pom.xml"),
        f"{stem}-sources.jar": Path(f"bazel-bin/client/java/{stem}-sources.jar"),
        f"{stem}-javadoc.jar": Path("bazel-bin/client/java/krpc-docs.jar"),
    }


@contextlib.contextmanager
def gpg(credentials):
    """A gpg holding only the release signing key.

    Yields a function that signs a file, and the key's fingerprint. The key is
    imported into a throwaway home directory, so the keyring and agent of the
    person running the release are left alone.
    """
    lib.require("gpg", "gpgconf")
    # Under /tmp where there is one: gpg-agent puts its socket in the home
    # directory on macOS, and a socket path is limited to about 100 bytes
    parent = "/tmp" if Path("/tmp").is_dir() else None
    with tempfile.TemporaryDirectory(dir=parent) as home:
        environment = dict(os.environ, GNUPGHOME=home)
        try:
            lib.capture(
                "gpg",
                "--batch",
                "--import",
                stdin=credentials["MAVEN_GPG_KEY"],
                env=environment,
            )
            fingerprint = _fingerprint(environment)

            def sign(path):
                lib.capture(
                    "gpg",
                    "--batch",
                    "--yes",
                    "--pinentry-mode",
                    "loopback",
                    "--passphrase-fd",
                    "0",
                    "--local-user",
                    fingerprint,
                    "--armor",
                    "--detach-sign",
                    "--output",
                    f"{path}.asc",
                    path,
                    stdin=credentials["MAVEN_GPG_PASSPHRASE"],
                    env=environment,
                )
                lib.capture(
                    "gpg", "--batch", "--verify", f"{path}.asc", path, env=environment
                )

            yield sign, fingerprint
        finally:
            lib.succeeds("gpgconf", "--kill", "gpg-agent", env=environment)
            # The socket directory is under /run/user on Linux, outside the home
            lib.succeeds("gpgconf", "--remove-socketdir", env=environment)


def _fingerprint(environment):
    secret_keys = [
        line.split(":")[9]
        for line in lib.capture(
            "gpg", "--batch", "--with-colons", "--list-secret-keys", env=environment
        ).splitlines()
        if line.startswith("fpr:")
    ]
    if not secret_keys:
        raise lib.ReleaseError("MAVEN_GPG_KEY holds no secret key")
    # The first fingerprint is the primary key's, which the Central Portal
    # looks up on the key servers
    return secret_keys[0]


def check_key_published(fingerprint):
    """Check a key server the Central Portal uses has the public key.

    A server that answers 404 does not have it. Any other failure leaves it
    unknown, and is reported as such.
    """
    unreachable = []
    for url in KEY_SERVERS:
        url = url.format(fingerprint=fingerprint)
        host = urllib.parse.urlparse(url).hostname
        try:
            with urllib.request.urlopen(url, timeout=30) as response:
                if b"BEGIN PGP PUBLIC KEY BLOCK" in response.read():
                    return
        except urllib.error.HTTPError as exception:
            if exception.code != 404:
                unreachable.append(f"{host} ({exception.code} {exception.reason})")
        except OSError as exception:
            unreachable.append(f"{host} ({exception})")
    if unreachable:
        raise lib.ReleaseError(
            f"the public key {fingerprint} was not found, and could not be "
            f"looked up on {', '.join(unreachable)}"
        )
    raise lib.ReleaseError(
        f"the public key {fingerprint} is on neither keys.openpgp.org "
        f"nor keyserver.ubuntu.com"
    )


def check_signing_key(sign, fingerprint):
    """Check the key from gpg() signs, and is on a key server."""
    with tempfile.TemporaryDirectory() as directory:
        probe = Path(directory) / "probe"
        probe.write_text("probe")
        sign(probe)
    check_key_published(fingerprint)


def bundle(sign, directory):
    """Write the signed bundle into a directory, and return its path."""
    repository = Path(directory) / "repository"
    version_directory = repository.joinpath(*GROUP.split("."), ARTIFACT, lib.VERSION)
    version_directory.mkdir(parents=True)
    for name, source in files().items():
        if not source.is_file():
            raise lib.ReleaseError(f"{source} was not built")
        path = version_directory / name
        path.write_bytes(source.read_bytes())
        sign(path)
        content = path.read_bytes()
        path.with_name(f"{name}.md5").write_text(hashlib.md5(content).hexdigest())
        path.with_name(f"{name}.sha1").write_text(hashlib.sha1(content).hexdigest())

    archive = Path(directory) / f"{ARTIFACT}-{lib.VERSION}-bundle.zip"
    with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as bundle_zip:
        for path in sorted(version_directory.iterdir()):
            bundle_zip.write(path, path.relative_to(repository).as_posix())
    return archive


def _request(credentials, method, path, body=None, content_type=None):
    """Call the Central Portal API, returning the response body."""
    token = base64.b64encode(
        f"{credentials['MAVEN_CENTRAL_USERNAME']}:"
        f"{credentials['MAVEN_CENTRAL_PASSWORD']}".encode()
    ).decode()
    headers = {"Authorization": f"Bearer {token}"}
    if content_type:
        headers["Content-Type"] = content_type
    request = urllib.request.Request(
        f"{PORTAL}/{path}", data=body, method=method, headers=headers
    )
    try:
        with urllib.request.urlopen(request, timeout=300) as response:
            return response.read().decode()
    except urllib.error.HTTPError as exception:
        detail = exception.read().decode(errors="replace").strip()
        raise lib.ReleaseError(
            f"the Central Portal rejected {method} {path.split('?')[0]} "
            f"({exception.code} {exception.reason}): {detail}"
        ) from exception
    except OSError as exception:
        raise lib.ReleaseError(
            f"could not reach the Central Portal: {exception}"
        ) from exception


def published(credentials):
    """Whether this version is already on Maven Central.

    Needs the Central Portal credentials, so it also checks them.
    """
    query = urllib.parse.urlencode(
        {"namespace": GROUP, "name": ARTIFACT, "version": lib.VERSION}
    )
    return json.loads(_request(credentials, "GET", f"published?{query}"))["published"]


def upload(credentials, archive):
    """Upload a bundle for validation, and return its deployment id.

    The deployment is user managed: it waits in the Central Portal, validated,
    until publish() publishes it.
    """
    boundary = uuid.uuid4().hex
    body = b"".join(
        [
            f"--{boundary}\r\n".encode(),
            f'Content-Disposition: form-data; name="bundle"; '
            f'filename="{archive.name}"\r\n'.encode(),
            b"Content-Type: application/octet-stream\r\n\r\n",
            archive.read_bytes(),
            f"\r\n--{boundary}--\r\n".encode(),
        ]
    )
    query = urllib.parse.urlencode(
        {"name": f"{ARTIFACT} {lib.VERSION}", "publishingType": "USER_MANAGED"}
    )
    return _request(
        credentials,
        "POST",
        f"upload?{query}",
        body,
        f"multipart/form-data; boundary={boundary}",
    ).strip()


def status(credentials, deployment):
    """The status of a deployment, as the Central Portal reports it."""
    query = urllib.parse.urlencode({"id": deployment})
    return json.loads(_request(credentials, "POST", f"status?{query}"))


def wait(credentials, deployment, states, timeout=1800):
    """Wait for a deployment to reach one of the given states, and return it.

    Raises if the deployment fails validation, listing the errors.
    """
    deadline = time.monotonic() + timeout
    last = None
    while True:
        deployment_status = status(credentials, deployment)
        state = deployment_status["deploymentState"]
        if state != last:
            print(f"deployment {deployment}: {state}")
            last = state
        if state in states:
            return state
        if state == "FAILED":
            errors = json.dumps(deployment_status.get("errors", {}), indent=2)
            raise lib.ReleaseError(f"the Central Portal rejected the bundle:\n{errors}")
        if time.monotonic() > deadline:
            raise DeploymentTimeout(
                f"deployment {deployment} is still {state} after {timeout} s; "
                f"check {DEPLOYMENTS_PAGE}"
            )
        time.sleep(10)


def publish(credentials, deployment):
    _request(credentials, "POST", f"deployment/{urllib.parse.quote(deployment)}")


def drop(credentials, deployment):
    """Drop a deployment that is not published, reporting rather than raising
    if the Central Portal refuses."""
    try:
        _request(credentials, "DELETE", f"deployment/{urllib.parse.quote(deployment)}")
        print(f"dropped deployment {deployment}")
    except lib.ReleaseError as exception:
        lib.warning(
            f"could not drop deployment {deployment}: {exception}. Drop it at "
            f"{DEPLOYMENTS_PAGE}"
        )
