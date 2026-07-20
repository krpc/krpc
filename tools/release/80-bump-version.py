#!/usr/bin/env python3
"""Sets the version in config.bzl to the one to work towards next.

Run once the release is out. Between releases config.bzl names the version
being worked towards rather than the last one released, so that a build from
main is a pre-release of it: CI stamps such a build as x.y.z-<commits>-<sha>,
which orders after the release just made and below the one set here.
"""

import re
from pathlib import Path

import lib

CONFIG = Path('config.bzl')


def release_parts(version):
    """The (x, y, z) of a plain release version, or None if it isn't one."""
    match = re.fullmatch(r'(\d+)\.(\d+)\.(\d+)', version)
    return tuple(int(part) for part in match.groups()) if match else None


def ask(current):
    """Ask which version to work towards, suggesting the next patch.

    Only the maintainer knows whether what comes next is a patch or a larger
    step, so the suggestion is the conservative one and is there to be typed
    over.
    """
    suggestion = '%d.%d.%d' % (current[0], current[1], current[2] + 1)
    version = input(f'Version to work towards [{suggestion}] ').strip() \
        or suggestion
    parts = release_parts(version)
    if parts is None:
        raise lib.ReleaseError(f"'{version}' is not an x.y.z version")
    if parts <= current:
        raise lib.ReleaseError(f'{version} does not come after {lib.VERSION}')
    return version


def main():
    lib.require('git')
    lib.require_clean_tree()

    current = release_parts(lib.VERSION)
    if current is None:
        raise lib.ReleaseError(
            f'config.bzl is at {lib.VERSION}, which is not a release version')

    branch = lib.capture('git', 'rev-parse', '--abbrev-ref', 'HEAD')
    if branch != 'main':
        raise lib.ReleaseError(f"on branch '{branch}', expected main")

    # Guards against bumping past a release that was never made: the tag is
    # created by 30-tag.py, so its absence means this is being run too early
    if not lib.succeeds('git', 'rev-parse', '-q', '--verify',
                        f'refs/tags/{lib.TAG}'):
        raise lib.ReleaseError(
            f'tag {lib.TAG} does not exist; release {lib.VERSION} first')

    version = ask(current)

    lib.banner(f'Setting the version in {CONFIG} to {version}')
    text = CONFIG.read_text()
    bumped = re.sub(r'^version = ".+"$', f'version = "{version}"', text,
                    count=1, flags=re.MULTILINE)
    if bumped == text:
        raise lib.ReleaseError(f'no version line found in {CONFIG}')
    CONFIG.write_text(bumped)
    lib.run('git', 'commit', '-m', f'Work towards {version}', str(CONFIG))

    lib.banner('Pushing to GitHub')
    lib.confirm('Push main to origin?')
    lib.run('git', 'push', 'origin', 'main')

    lib.banner('Done')
    print(f'main is working towards {version}. Changelog entries for it go '
          f'under a v{version} header.')


if __name__ == '__main__':
    lib.main(main)
