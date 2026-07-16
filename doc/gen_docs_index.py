#!/usr/bin/env python3
"""Regenerate switcher.json and the root index.html for the versioned docs site.

Scans a gh-pages working tree for per-version subdirectories, semver-sorts them
newest-first, marks the newest full release as preferred, and appends a `dev`
entry if a dev/ build is present. Writes, into the same directory:

  * switcher.json -- the runtime version list read by pydata-sphinx-theme's
                     version switcher (the `switcher` entry in html_theme_options)
  * index.html    -- a meta-refresh redirect from the site root to the newest
                     released version (or dev, if no release is present yet)
  * .nojekyll     -- created if missing, so GitHub Pages serves _static/ as-is

Run after unpacking a build into gh-pages/<subpath>/. Every other version's
files are left untouched.
"""

import argparse
import json
import os
import re

# Absolute URL the site is served under. pydata-sphinx-theme's version switcher
# requires full URLs in switcher.json (it also uses them to probe whether the
# current page exists in the selected version before navigating).
SITE = "https://krpc.github.io/krpc"
SKIP_ENTRIES = {".git", ".nojekyll", "switcher.json", "index.html"}

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


def build_entries(releases, has_dev):
    """Entries in pydata-sphinx-theme's switcher.json format: `version` is
    matched against the theme's version_match (the subpath a build was
    published under), `name` is the dropdown label, and `preferred` marks the
    newest release (used by the old-version warning banner)."""
    entries = []
    for i, version in enumerate(releases):
        entry = {
            "name": version,
            "version": version,
            "url": "%s/%s/" % (SITE, version),
        }
        if i == 0:
            entry["name"] = "%s (stable)" % version
            entry["preferred"] = True
        entries.append(entry)
    if has_dev:
        entries.append(
            {"name": "dev", "version": "dev", "url": "%s/dev/" % SITE}
        )
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


def main():
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument("root", help="Path to the gh-pages working tree")
    args = parser.parse_args()

    releases, has_dev = discover_versions(args.root)
    entries = build_entries(releases, has_dev)

    switcher_path = os.path.join(args.root, "switcher.json")
    with open(switcher_path, "w", encoding="utf-8") as fp:
        json.dump(entries, fp, indent=2)
        fp.write("\n")

    url = preferred_url(entries)
    with open(os.path.join(args.root, "index.html"), "w", encoding="utf-8") as fp:
        fp.write(render_index(url))

    nojekyll = os.path.join(args.root, ".nojekyll")
    if not os.path.exists(nojekyll):
        open(nojekyll, "w").close()

    print(
        "switcher.json: %d entries (%s); root redirects to %s"
        % (len(entries), ", ".join(e["version"] for e in entries), url)
    )


if __name__ == "__main__":
    main()
