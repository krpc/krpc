#!/usr/bin/env python3
"""Creates a draft GitHub release for the pushed vx.x.x tag.

The changelog becomes the release notes and everything in assets/ is attached.
Review the draft on GitHub and publish it from there.
"""

import os
from pathlib import Path

import changes
import lib


def main():
    lib.require('gh', 'git')
    credentials = lib.load_credentials()

    # Act as the token from the credentials file, not as whoever 'gh auth
    # login' signed in as
    environment = dict(os.environ, GH_TOKEN=credentials['GITHUB_TOKEN'])

    if not lib.capture('git', 'ls-remote', 'origin', f'refs/tags/{lib.TAG}'):
        raise lib.ReleaseError(
            f'tag {lib.TAG} has not been pushed to origin (run 30-tag.py first)')
    assets = sorted(Path('assets').iterdir()) if Path('assets').is_dir() else []
    if not assets:
        raise lib.ReleaseError(
            'assets/ is empty (run 40-build-assets.py first)')
    if lib.succeeds('gh', 'release', 'view', lib.TAG, env=environment):
        raise lib.ReleaseError(
            f'a GitHub release for {lib.TAG} already exists')

    lib.banner('Release notes')
    notes = changes.render('github', lib.VERSION)
    print(notes)

    lib.banner(f'Creating draft release {lib.TAG}')
    lib.confirm(f'Create the draft release with {len(assets)} assets?')
    url = lib.capture('gh', 'release', 'create', lib.TAG, '--verify-tag',
                      '--draft', '--title', lib.TAG, '--notes', notes,
                      *assets, env=environment)

    lib.banner('Done')
    print(f'Draft created: {url}')
    print('Review it on GitHub and publish it from there.')
    print('Next: the release-* scripts, in any order')


if __name__ == '__main__':
    lib.main(main)
