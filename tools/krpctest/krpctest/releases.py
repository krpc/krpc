"""Finding a kRPC release archive on a game's release train.

Each game releases on its own train of GitHub tags, such as ``ksp-v1.0.3``. A lookup is
cached, so a release is listed once and an install from the cache needs no network.
"""

import json
import os
import urllib.request

from . import install
from .version import __version__

REPOSITORY = "krpc/krpc"


def _get(url):
    headers = {
        "Accept": "application/vnd.github+json",
        "User-Agent": "krpctools/" + __version__,
    }
    # A token lifts the rate limit, which is 60 requests an hour without one
    token = os.environ.get("GITHUB_TOKEN")
    if token:
        headers["Authorization"] = "Bearer " + token
    request = urllib.request.Request(url, headers=headers)

    def get():
        with urllib.request.urlopen(request, timeout=60) as response:
            return json.load(response)

    return install.with_retries(get, "list the kRPC releases")


def github_releases(repository=REPOSITORY):
    """The published releases of the repository, newest first."""
    releases = []
    page = 1
    while True:
        batch = _get(
            "https://api.github.com/repos/%s/releases?per_page=100&page=%d"
            % (repository, page)
        )
        releases.extend(batch)
        if len(batch) < 100:
            return releases
        page += 1


def find(releases, prefixes, assets, version):
    """The URL and SHA-256 of the archive of a release on the train. prefixes are the
    train's tag prefixes, in order of preference, and assets(version) the names its archive
    goes by."""
    published = {
        release["tag_name"]: release
        for release in releases
        if not release.get("draft") and not release.get("prerelease")
    }
    tags = [prefix + version for prefix in prefixes]
    release = next((published[tag] for tag in tags if tag in published), None)
    if release is None:
        raise RuntimeError(
            "No kRPC release %s on the %s train. Install an archive with --krpc."
            % (version, " or ".join(prefixes))
        )
    names = [name.lower() for name in assets(version)]
    for asset in release.get("assets", []):
        if asset["name"].lower() in names:
            digest = asset.get("digest") or ""
            sha256 = digest[len("sha256:") :] if digest.startswith("sha256:") else None
            return asset["browser_download_url"], sha256
    raise RuntimeError(
        "Release %s has no kRPC archive (%s)" % (release["tag_name"], ", ".join(names))
    )


def release(name, prefixes, assets, version):
    """The URL and SHA-256 of a release's archive, as find gives them, looked up once and
    then read from the cache. name is the game's, which keys the cache."""
    path = os.path.join(install.cache_dir(), "releases", "%s-%s.json" % (name, version))
    try:
        with open(path, encoding="utf-8") as f:
            url, sha256 = json.load(f)
        return url, sha256
    except (OSError, ValueError):
        pass
    url, sha256 = find(github_releases(), prefixes, assets, version)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        json.dump([url, sha256], f)
    return url, sha256
