#!/usr/bin/env python3
"""Confirms the release reached CKAN.

kRPC is indexed on CKAN automatically: its NetKAN metadata pulls from the GitHub
release (a github/krpc/krpc asset match, versioned by the KSP-AVC file inside the
zip), and the NetKAN bot inflates that into a KSP-CKAN/CKAN-meta entry on its
own. There is nothing to push here -- the inflation webhooks are firewalled to
KSP-CKAN's own servers, and a mod already in NetKAN needs no pull request -- so
this step only waits for the generated metadata to appear. That turns the CKAN
channel into a definite done rather than something to check back on hours later.

Run after the GitHub release is published (50-release-github.py); the bot has
nothing to pull until then. Reading public endpoints only, so it needs no
credentials, and re-running it just re-checks: interrupt the wait and run it
again later if the metadata has not landed yet.
"""

import os
import time
import urllib.error
import urllib.request

import lib

# The published-releases-by-tag endpoint. A draft is not returned here, and a
# draft is exactly the state that leaves the bot with nothing to pull.
RELEASE_API = 'https://api.github.com/repos/krpc/krpc/releases/tags/{tag}'

# Where the NetKAN bot writes kRPC's generated metadata, one file per version.
# The CKAN version comes from the KSP-AVC file, whose 3-part MAJOR.MINOR.PATCH
# matches the release version, so the file is named for the tag: kRPC-v0.6.0.ckan.
CKAN_META = ('https://raw.githubusercontent.com/KSP-CKAN/CKAN-meta/master/'
             'kRPC/kRPC-{tag}.ckan')

# How long to wait for the metadata, and how often to look. Inflation is usually
# minutes but can lag; override the wait with CKAN_POLL_MINUTES for a longer sit.
POLL_SECONDS = 30
DEFAULT_POLL_MINUTES = 20


def exists(url):
    """Whether a URL responds 200 to a HEAD, treating 404 as a plain no.

    Any other failure is a problem the operator needs to see, so it is raised
    rather than reported as 'not there yet'.
    """
    request = urllib.request.Request(url, method='HEAD')
    try:
        urllib.request.urlopen(request, timeout=30)
        return True
    except urllib.error.HTTPError as exception:
        if exception.code == 404:
            return False
        raise lib.ReleaseError(
            f'could not reach {url} '
            f'({exception.code} {exception.reason})') from exception
    except urllib.error.URLError as exception:
        raise lib.ReleaseError(
            f'could not reach {url}: {exception.reason}') from exception


def poll_minutes():
    """The wait in minutes, from CKAN_POLL_MINUTES if it is set and valid."""
    value = os.environ.get('CKAN_POLL_MINUTES')
    if value is None:
        return DEFAULT_POLL_MINUTES
    try:
        minutes = int(value)
    except ValueError:
        raise lib.ReleaseError(
            f'CKAN_POLL_MINUTES is not an integer: {value!r}') from None
    if minutes < 0:
        raise lib.ReleaseError('CKAN_POLL_MINUTES cannot be negative')
    return minutes


def main():
    release_url = RELEASE_API.format(tag=lib.TAG)
    if not exists(release_url):
        raise lib.ReleaseError(
            f'no published GitHub release for {lib.TAG}; publish it '
            f'(50-release-github.py) before CKAN can pull it')

    meta_url = CKAN_META.format(tag=lib.TAG)
    minutes = poll_minutes()
    lib.banner(f'Waiting for CKAN to index {lib.TAG}')
    print(f'Watching {meta_url}')

    deadline = time.monotonic() + minutes * 60
    while True:
        if exists(meta_url):
            lib.banner('Done')
            print(f'CKAN has {lib.TAG}: {meta_url}')
            return
        if time.monotonic() + POLL_SECONDS > deadline:
            break
        print(f'{lib.DIM}not indexed yet; checking again in '
              f'{POLL_SECONDS}s{lib.RESET}')
        time.sleep(POLL_SECONDS)

    lib.warning(
        f'{lib.TAG} has not appeared in CKAN-meta after {minutes} minutes')
    print('Inflation is automatic but can lag; it usually lands within a few '
          'hours.')
    print(f'Re-run this step later to confirm, or check: {meta_url}')


if __name__ == '__main__':
    lib.main(main)
