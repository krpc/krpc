#!/usr/bin/env python3
"""Tags HEAD as vx.x.x and pushes the branch and tags to GitHub.

Moves the latest-version tag to the same commit. Pushing the version tag
triggers the docs workflow, which freezes the release documentation under
/<version>/ and regenerates the version dropdown.
"""

import lib


def main():
    lib.require('git')
    lib.require_clean_tree()

    if lib.succeeds('git', 'rev-parse', '-q', '--verify',
                    f'refs/tags/{lib.TAG}'):
        if lib.capture('git', 'rev-parse', f'{lib.TAG}^{{commit}}') != \
                lib.capture('git', 'rev-parse', 'HEAD'):
            raise lib.ReleaseError(
                f'tag {lib.TAG} already exists but points elsewhere')
        print(f'Tag {lib.TAG} already exists and points at HEAD.')
    else:
        lib.banner(f'Tagging HEAD as {lib.TAG}')
        lib.run('git', 'tag', '-a', lib.TAG, '-m', f'kRPC {lib.TAG}')

    lib.banner('Moving the latest-version tag to HEAD')
    lib.run('git', 'tag', '-f', 'latest-version')

    lib.banner('Pushing to GitHub')
    lib.confirm(f'Push main, {lib.TAG} and latest-version to origin?')
    lib.run('git', 'push', 'origin', 'main')
    lib.run('git', 'push', 'origin', lib.TAG)
    lib.run('git', 'push', '-f', 'origin', 'latest-version')

    lib.banner('Done')
    print('Wait for the CI workflow to pass before continuing:')
    print("  gh run watch $(gh run list --branch main --workflow ci "
          "--limit 1 --json databaseId --jq '.[0].databaseId')")
    print('Next: tools/release/40-build-assets.py')


if __name__ == '__main__':
    lib.main(main)
