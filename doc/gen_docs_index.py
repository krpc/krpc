#!/usr/bin/env python3
"""Regenerate switcher.json and the root index.html for the versioned docs site.

Scans a gh-pages working tree for per-version subdirectories, semver-sorts them
newest-first, marks the newest full release as preferred, and — if a dev/ build
is present — puts a dev entry at the top of the list (labelled with the version
it is working towards, e.g. `v0.6.0 (dev)`, without making it the default).
Writes, into the same directory:

  * switcher.json -- the runtime version list read by pydata-sphinx-theme's
                     version switcher (the `switcher` entry in html_theme_options)
  * index.html    -- a meta-refresh redirect from the site root to the stable
                     `latest/` alias (or dev, if no release is present yet)
  * latest/       -- a verbatim copy of the newest release, giving a stable
                     `/latest/` URL that always resolves to the current stable
                     docs (see sync_latest)
  * .nojekyll     -- created if missing, so GitHub Pages serves _static/ as-is

Run after unpacking a build into gh-pages/<subpath>/. Every other version's
files are left untouched.
"""

import argparse
import json
import os
import re
import shutil

# Absolute URL the site is served under. pydata-sphinx-theme's version switcher
# requires full URLs in switcher.json (it also uses them to probe whether the
# current page exists in the selected version before navigating).
SITE = "https://krpc.github.io/krpc"

# The stable alias directory holding a copy of the newest release (see
# sync_latest). Not a version, so it is kept out of switcher discovery.
LATEST = "latest"
SKIP_ENTRIES = {".git", ".nojekyll", "switcher.json", "index.html", LATEST}

_NUM = re.compile(r"^\d+$")
# A published release directory is a plain three-component version (e.g.
# `0.5.4`). Anything else under the gh-pages root - `dev`, a stray directory, a
# pre-release tag build - is not a release and is kept out of the switcher.
_RELEASE_RE = re.compile(r"^\d+\.\d+\.\d+$")


def version_key(version):
    """Semver-ish sort key; a higher tuple is a newer version. A release (no
    pre-release suffix) sorts above all of its own pre-releases."""
    core, _, pre = version.partition("-")
    nums = [int(p) if _NUM.match(p) else 0 for p in core.split(".")]
    while len(nums) < 3:
        nums.append(0)
    if pre == "":
        # Rank a release above its pre-releases and give it an empty pre-tuple.
        return (tuple(nums), 1, ())
    pre_parts = tuple(
        (0, int(p)) if _NUM.match(p) else (1, p) for p in pre.split(".")
    )
    return (tuple(nums), 0, pre_parts)


def planned_version():
    """The version the /dev/ build is working towards, read from config.bzl and
    stripped of any build-stamp suffix (e.g. `0.6.0-3-abcdef` -> `0.6.0`). It is
    known ahead of the release, so the dev entry can name it rather than showing
    a bare `dev`."""
    config = os.path.join(
        os.path.dirname(os.path.abspath(__file__)), os.pardir, "config.bzl"
    )
    with open(config, encoding="utf-8") as fp:
        for line in fp:
            match = re.match(r'^version\s*=\s*"([^"]+)"', line)
            if match:
                return match.group(1).partition("-")[0]
    raise RuntimeError("no version found in %s" % config)


def discover_versions(root):
    releases = []
    has_dev = False
    for name in sorted(os.listdir(root)):
        if name in SKIP_ENTRIES or name.startswith("."):
            continue
        if not os.path.isdir(os.path.join(root, name)):
            continue
        if name == "dev":
            has_dev = True
        elif _RELEASE_RE.match(name):
            releases.append(name)
    releases.sort(key=version_key, reverse=True)
    return releases, has_dev


def build_entries(releases, dev_version):
    """Entries in pydata-sphinx-theme's switcher.json format: `version` is
    matched against the theme's version_match (the subpath a build was
    published under), `name` is the dropdown label, and `preferred` marks the
    newest release (used by the old-version warning banner). The `name` label
    carries a `v` prefix for display; `version` and `url` stay bare because they
    are matched against the subpath each build was published under.

    `dev_version` is the version the /dev/ build is working towards, or None if
    there is no dev build. The dev entry leads the list as the newest line, but
    is not marked preferred, so the newest release stays the default."""
    entries = []
    if dev_version is not None:
        entries.append(
            {
                "name": "v%s (dev)" % dev_version,
                "version": "dev",
                "url": "%s/dev/" % SITE,
            }
        )
    for i, version in enumerate(releases):
        entry = {
            "name": "v%s" % version,
            "version": version,
            "url": "%s/%s/" % (SITE, version),
        }
        if i == 0:
            entry["name"] = "v%s (stable)" % version
            entry["preferred"] = True
        entries.append(entry)
    return entries


def preferred_url(entries):
    for entry in entries:
        if entry.get("preferred"):
            return entry["url"]
    return entries[0]["url"] if entries else "%s/" % SITE


def render_index(url):
    return (
        "<!DOCTYPE html>\n"
        '<html lang="en">\n'
        "<head>\n"
        '<meta charset="utf-8">\n'
        '<meta http-equiv="refresh" content="0; url=%s">\n'
        '<link rel="canonical" href="%s">\n'
        "<title>kRPC documentation</title>\n"
        "</head>\n"
        "<body>\n"
        'Redirecting to the <a href="%s">kRPC documentation</a>&hellip;\n'
        "</body>\n"
        "</html>\n"
    ) % (url, url, url)


def sync_latest(root, releases):
    """Mirror the newest release into `latest/`, giving a stable `/latest/` URL
    that always resolves to the current stable docs.

    GitHub Pages is static, so `latest/` is a verbatim copy of the newest
    release's frozen build rather than a server-side redirect. The copied pages
    keep that release's subpath baked into html_baseurl and the switcher's
    version_match, so their canonical links point back to the versioned copy
    (no duplicate-content penalty) and the switcher highlights the real version
    with no old-version banner.

    Rebuilt from scratch on every run, so it always tracks whichever release is
    newest on disk: a run that republishes an older patch, or a dev build, leaves
    `latest/` pointing at the newest release rather than at what was just built.
    Copying identical content produces no git diff, so this is a no-op commit-wise
    unless the newest release actually changed. Returns the mirrored version, or
    None when there is no release to mirror yet.
    """
    latest = os.path.join(root, LATEST)
    shutil.rmtree(latest, ignore_errors=True)
    if not releases:
        return None
    newest = releases[0]
    shutil.copytree(os.path.join(root, newest), latest)
    return newest


def main():
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument("root", help="Path to the gh-pages working tree")
    args = parser.parse_args()

    releases, has_dev = discover_versions(args.root)
    entries = build_entries(releases, planned_version() if has_dev else None)

    switcher_path = os.path.join(args.root, "switcher.json")
    with open(switcher_path, "w", encoding="utf-8") as fp:
        json.dump(entries, fp, indent=2)
        fp.write("\n")

    latest = sync_latest(args.root, releases)

    # Redirect the root at the stable latest/ alias when a release exists, so
    # the default landing URL tracks the newest release without changing. With
    # no release yet, fall back to the dev build (the sole switcher entry).
    url = "%s/%s/" % (SITE, LATEST) if latest else preferred_url(entries)
    with open(os.path.join(args.root, "index.html"), "w", encoding="utf-8") as fp:
        fp.write(render_index(url))

    nojekyll = os.path.join(args.root, ".nojekyll")
    if not os.path.exists(nojekyll):
        open(nojekyll, "w").close()

    print(
        "switcher.json: %d entries (%s); root redirects to %s; latest/ -> %s"
        % (
            len(entries),
            ", ".join(e["version"] for e in entries),
            url,
            latest or "(none)",
        )
    )


if __name__ == "__main__":
    main()
