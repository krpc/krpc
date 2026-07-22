"""Publishing a kRPC vcpkg port to the microsoft/vcpkg registry.

The C++ and C-nano clients are distributed through microsoft/vcpkg, whose ports
live in one large monorepo rather than in a service that packages get uploaded
to. Releasing a new version is therefore a pull request against it: the updated
port, with the released archive's SHA-512 filled in and the version bumped, plus
the matching entry in vcpkg's own versions database.

The port is built on a branch of a local vcpkg checkout (VCPKG_ROOT, the same
variable the client's vcpkg test scripts use), pushed to a fork of vcpkg on the
release account, and turned into a PR with gh. The two clients differ only in
the port name and where its overlay lives, so both release steps call publish()
with those two things.
"""

import hashlib
import json
import os
import re
import urllib.error
import urllib.request
from pathlib import Path

import lib

# microsoft/vcpkg's default branch: a port PR both branches from it and targets
# it. Fetched by URL rather than through a remote, so the step does not depend
# on how the VCPKG_ROOT checkout names its remotes.
UPSTREAM = 'microsoft/vcpkg'
UPSTREAM_URL = 'https://github.com/microsoft/vcpkg.git'
UPSTREAM_BRANCH = 'master'


def vcpkg_root():
    """The vcpkg checkout to build the port in, from VCPKG_ROOT.

    The port is prepared on a branch of this checkout, so it must be a git clone
    of vcpkg (as the normal ``git clone`` install gives), not an unpacked
    release.
    """
    root = os.environ.get('VCPKG_ROOT')
    if not root:
        raise lib.ReleaseError(
            'VCPKG_ROOT is not set; point it at a vcpkg git checkout')
    root = Path(root)
    binary = root / 'vcpkg'
    if not binary.is_file():
        raise lib.ReleaseError(f'no vcpkg executable at {binary}')
    if not (root / '.git').exists():
        raise lib.ReleaseError(f'{root} is not a git checkout of vcpkg')
    return root, binary


def download_url(overlay):
    """The URL the port downloads its archive from, with the version filled in.

    Read from the port itself so that the archive being hashed is exactly the
    one the published port will point at, whatever host or path that is.
    """
    text = (overlay / 'portfile.cmake').read_text()
    match = re.search(r'URLS "([^"]+)"', text)
    if match is None:
        raise lib.ReleaseError(f'no URLS line in {overlay}/portfile.cmake')
    return match.group(1).replace('${VERSION}', lib.VERSION)


def archive_sha512(url):
    """The SHA-512 of the archive at a URL, streamed so that a large archive is
    not held in memory."""
    digest = hashlib.sha512()
    try:
        with urllib.request.urlopen(url, timeout=120) as response:
            for chunk in iter(lambda: response.read(1 << 20), b''):
                digest.update(chunk)
    except urllib.error.HTTPError as exception:
        if exception.code == 404:
            raise lib.ReleaseError(
                f'{url} was not found; publish the GitHub release '
                f'(50-release-github.py) before its vcpkg port') from exception
        raise lib.ReleaseError(
            f'could not download {url} '
            f'({exception.code} {exception.reason})') from exception
    except urllib.error.URLError as exception:
        raise lib.ReleaseError(
            f'could not download {url}: {exception.reason}') from exception
    return digest.hexdigest()


def write_port(root, port, overlay, sha512):
    """Copy the overlay port into the vcpkg checkout, filling in the released
    archive's SHA-512 and version, and return its directory."""
    # vcpkg.json's version must be bare semver; a development build's suffix
    # (0.6.0-1234-abc) is not one vcpkg accepts.
    semver = re.match(r'\d+\.\d+\.\d+', lib.VERSION)
    if semver is None:
        raise lib.ReleaseError(f'{lib.VERSION} is not a semver version')

    destination = root / 'ports' / port
    if destination.is_dir():
        for existing in destination.iterdir():
            existing.unlink()
    else:
        destination.mkdir(parents=True)
    for name in ('portfile.cmake', 'vcpkg.json', 'usage'):
        (destination / name).write_text((overlay / name).read_text())

    portfile = destination / 'portfile.cmake'
    text, count = re.subn(r'SHA512 0(\s*#.*)?', f'SHA512 {sha512}',
                          portfile.read_text(), count=1)
    if count == 0:
        raise lib.ReleaseError(
            f'{overlay}/portfile.cmake has no "SHA512 0" placeholder to fill in')
    portfile.write_text(text)

    manifest = destination / 'vcpkg.json'
    data = json.loads(manifest.read_text())
    data['version'] = semver.group(0)
    manifest.write_text(json.dumps(data, indent=2) + '\n')
    return destination


def publish(port, overlay, credentials):
    """Open a microsoft/vcpkg PR updating one port to the release version.

    ``port`` is the registry name (``krpc``, ``krpc-cnano``); ``overlay`` is the
    in-repo directory whose port files are published (``client/*/vcpkg-port``).
    """
    root, binary = vcpkg_root()
    lib.require('git', 'gh')
    token = credentials['GITHUB_TOKEN']
    # gh acts as the token from the credentials file, not as whoever 'gh auth
    # login' signed in as; the branch is pushed with your normal git setup.
    environment = dict(os.environ, GH_TOKEN=token)

    if lib.capture('git', '-C', root, 'status', '--porcelain'):
        raise lib.ReleaseError(
            f'{root} has uncommitted changes; the port is built on a fresh '
            f'branch of it, so it must start clean')

    url = download_url(overlay)
    lib.banner('Hashing the released archive')
    print(url)
    sha512 = archive_sha512(url)
    print(f'SHA512 {sha512}')

    branch = f'{port}-{lib.VERSION}'
    lib.banner(f'Preparing the {port} port on {branch}')
    lib.run('git', '-C', root, 'fetch', UPSTREAM_URL, UPSTREAM_BRANCH)
    lib.run('git', '-C', root, 'checkout', '-B', branch, 'FETCH_HEAD')
    destination = write_port(root, port, overlay, sha512)
    lib.run(binary, 'format-manifest', destination / 'vcpkg.json', cwd=root)
    lib.run('git', '-C', root, 'add', f'ports/{port}')
    lib.run('git', '-C', root, 'commit', '-m', f'[{port}] Update to {lib.VERSION}')
    # x-add-version reads the committed port and writes its version database
    # entry; the standard flow folds that into the same commit.
    lib.run(binary, 'x-add-version', port, cwd=root)
    lib.run('git', '-C', root, 'add', 'versions')
    lib.run('git', '-C', root, 'commit', '--amend', '--no-edit')

    login, _ = lib.github_user(token)
    fork = f'git@github.com:{login}/vcpkg.git'
    test_script = overlay.parent / 'test-vcpkg-linux.sh'

    lib.banner(f'Opening the pull request to {UPSTREAM}')
    print(f'To try the port first: {test_script}')
    lib.confirm(f'Push {branch} to {login}/vcpkg and open a PR to {UPSTREAM}?')

    # Idempotent: prints that the fork already exists and exits zero if so.
    lib.run('gh', 'repo', 'fork', UPSTREAM, '--clone=false', env=environment)
    # A dedicated per-release branch on your own fork, forced so re-running the
    # step after a fix replaces the previous attempt.
    lib.run('git', '-C', root, 'push', '--force', fork, branch)

    # Link the PR back to the same release the hashed archive came from.
    release = re.sub(r'/download/([^/]+)/[^/]+$', r'/tag/\1', url)
    body = (f'Updates the `{port}` port to {lib.VERSION}.\n\n'
            f'Release: {release}')
    pull = lib.capture('gh', 'pr', 'create', '-R', UPSTREAM,
                       '--base', UPSTREAM_BRANCH, '--head', f'{login}:{branch}',
                       '--title', f'[{port}] Update to {lib.VERSION}',
                       '--body', body, env=environment)

    lib.banner('Done')
    print(f'Pull request: {pull}')
