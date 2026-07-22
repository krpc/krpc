#!/usr/bin/env python3
"""(Re)build the frozen documentation site for a released version, and
optionally stage it into a gh-pages checkout for publishing.

The frozen docs are built by grafting the version's documentation *content*
onto the *current* checkout's toolchain and theme:

  * content   -- the handwritten sources and the service API surfaces (doc/src,
    doc/api, doc/order.txt, the service/*/src trees and core/src/Service/KRPC),
    taken from the v<version>-docs branch if it exists, otherwise from the
    v<version> tag.
  * toolchain -- everything else (the Bazel setup, krpctools, the sphinx theme
    and static assets, the rest of core/ and server/) stays at the current
    checkout, so theme and doc-generator fixes made since the release apply to
    the rebuild automatically.

Content corrections for a released version (typos, rendering fixes that live in
the sources) are therefore made as commits on a long-lived v<version>-docs
branch based off the v<version> tag, leaving the version's API surface and prose
otherwise frozen. This is an explicit, opt-in maintenance action; the docs
workflow never rebuilds a released version.

Usage:
  tools/release/publish-docs.py [options] VERSION

Options:
  --publish DIR     stage the built site into gh-pages checkout DIR: replace
                    DIR/<version>/, regenerate the switcher and root redirect,
                    and commit. The commit is left for review; push it yourself
                    after checking the diff.
  --output FILE     without --publish, copy the built html.zip here
                    (default: krpc-doc-<version>.zip in the repo root).
  --worktree DIR    build worktree location
                    (default: ../krpc-worktree-docs-<version>).
  --keep-worktree   keep the build worktree on success (default: remove it).
"""

import argparse
import re
import shutil
import subprocess
import sys
import zipfile
from pathlib import Path

import lib

# The documentation content inputs, restored from the content ref onto the
# current toolchain. Pinning core/src/Service/KRPC to the release keeps later
# changes to the KRPC service API (e.g. the GameScene API, #897) out of the
# frozen build.
CONTENT_PATHS = (
    'doc/src',
    'doc/api',
    'doc/order.txt',
    'core/src/Service/KRPC',
    'service/SpaceCenter/src',
    'service/Drawing/src',
    'service/UI/src',
    'service/InfernalRobotics/src',
    'service/KerbalAlarmClock/src',
    'service/RemoteTech/src',
    'service/LiDAR/src',
    'service/DockingCamera/src',
)

# Grafting an old service API onto newer core/server can drop a type the current
# toolchain still references. The only such case to date is the namespace-level
# KRPC.Service.KRPC.GameScene enum (the GameScene API, #897/#936): core
# CallContext.LoadScene and the server GameSceneSwitcher / Addon wiring reference
# it, but a pre-#897 KRPC service dir doesn't define it. An attribute-less enum
# lets the graft compile; lacking [KRPCEnum] it is not scanned into the service
# definition, so the frozen docs keep the content ref's own GameScene. A version
# whose API already carries the type needs no shim.
GAMESCENE_TYPE = 'KRPC.Service.KRPC.GameScene'
GAMESCENE_SHIM = '''namespace KRPC.Service.KRPC
{
    /// <summary>
    /// Compatibility shim for the frozen documentation build, written by
    /// publish-docs.py. Carries no [KRPCEnum] attribute, so the scanner does
    /// not surface it in the frozen API. Doc comments are required because the
    /// build compiles with warnings-as-errors.
    /// </summary>
    public enum GameScene
    {
        /// <summary>The space center.</summary>
        SpaceCenter,
        /// <summary>A vessel in flight.</summary>
        Flight,
        /// <summary>The tracking station.</summary>
        TrackingStation,
        /// <summary>The Vehicle Assembly Building.</summary>
        EditorVAB,
        /// <summary>The Space Plane Hangar.</summary>
        EditorSPH,
        /// <summary>The mission builder.</summary>
        MissionBuilder,
        /// <summary>The astronaut complex.</summary>
        AstronautComplex,
        /// <summary>Mission control.</summary>
        MissionControl,
        /// <summary>Research and development.</summary>
        ResearchAndDevelopment,
        /// <summary>The administration facility.</summary>
        Administration,
    }
}
'''


def run_in(cwd, *command):
    """Run a command in a directory, failing the step if it does.

    lib.run always runs at the repository root; the build steps need to run in
    the build worktree instead.
    """
    command = [str(argument) for argument in command]
    print(f'{lib.DIM}$ (cd {cwd}) {" ".join(command)}{lib.RESET}')
    if subprocess.run(command, cwd=cwd).returncode != 0:
        raise lib.ReleaseError(f'{command[0]} failed')


def require_clean_tree():
    """Refuse to run with uncommitted changes to tracked files.

    The build takes its toolchain from the HEAD commit, so an uncommitted
    change would silently not be in the build; untracked files (a previous
    output zip) are ignored.
    """
    if lib.capture('git', 'diff-index', '--name-only', 'HEAD', '--'):
        raise lib.ReleaseError(
            'the working tree has uncommitted changes to tracked files')


def content_ref(version):
    """The ref the documentation content comes from: the fixup branch if there
    is one (local or on origin), otherwise the release tag."""
    branch = f'v{version}-docs'
    if lib.succeeds('git', 'show-ref', '--verify', '--quiet',
                    f'refs/heads/{branch}'):
        return branch
    if lib.succeeds('git', 'show-ref', '--verify', '--quiet',
                    f'refs/remotes/origin/{branch}'):
        return f'origin/{branch}'
    return f'v{version}'


def graft(worktree, base, ref):
    """Restore the version's documentation content onto the current toolchain.

    rm first, so files added since the content ref (which a plain checkout would
    leave behind) don't survive. doc/src/_static (theme static files) and
    doc/src/_templates (template overrides) are toolchain assets, not release
    content: drop whatever the content ref carried -- an older theme's
    custom.css and layout.html -- and restore the current checkout's versions so
    the frozen build uses the current theme and footer template.
    """
    lib.run('git', '-C', worktree, 'rm', '-rq', *CONTENT_PATHS)
    lib.run('git', '-C', worktree, 'checkout', ref, '--', *CONTENT_PATHS)
    lib.run('git', '-C', worktree, 'rm', '-rqf', '--ignore-unmatch',
            'doc/src/_static', 'doc/src/_templates')
    lib.run('git', '-C', worktree, 'checkout', base, '--', 'doc/src/_static')
    if lib.succeeds('git', 'cat-file', '-e', f'{base}:doc/src/_templates'):
        lib.run('git', '-C', worktree, 'checkout', base, '--',
                'doc/src/_templates')


def maybe_write_gamescene_shim(worktree):
    """Write the GameScene compatibility shim if the current toolchain needs it
    and the grafted content doesn't already provide the type."""
    shim = worktree / 'core/src/Service/KRPC/GameScene.cs'
    if shim.exists():
        return
    referenced = any(
        GAMESCENE_TYPE in path.read_text(encoding='utf-8')
        for directory in ('core/src', 'server/src')
        for path in (worktree / directory).rglob('*.cs'))
    if referenced:
        shim.write_text(GAMESCENE_SHIM, encoding='utf-8')


def exclude_changelog(worktree):
    """Drop the unified changelog page from the build; it postdates the early
    releases and carries newer entries."""
    build_file = worktree / 'doc/BUILD.bazel'
    lines = build_file.read_text(encoding='utf-8').splitlines(keepends=True)
    kept = [line for line in lines if '":changelog",' not in line]
    if len(kept) == len(lines):
        raise lib.ReleaseError(
            "doc/BUILD.bazel has no ':changelog,' staging entry to remove; "
            'update this script')
    build_file.write_text(''.join(kept), encoding='utf-8')


def pin_version(worktree, version):
    """Pin the version stamp (build subpath, html_baseurl, switcher match)."""
    config = worktree / 'config.bzl'
    text, count = re.subn(r'^version = ".*"$', f'version = "{version}"',
                          config.read_text(encoding='utf-8'), flags=re.MULTILINE)
    if count != 1:
        raise lib.ReleaseError('failed to pin the version in config.bzl')
    config.write_text(text, encoding='utf-8')


def check_no_changelog(zip_path):
    with zipfile.ZipFile(zip_path) as archive:
        if any(Path(name).name == 'changelog.html'
               for name in archive.namelist()):
            raise lib.ReleaseError(
                'changelog.html present in the built site; the exclusion failed')


def publish(zip_path, publish_dir, version):
    """Replace this version's subdirectory in the gh-pages checkout, regenerate
    the switcher and root redirect, and commit for review."""
    target = publish_dir / version
    shutil.rmtree(target, ignore_errors=True)
    target.mkdir(parents=True)
    with zipfile.ZipFile(zip_path) as archive:
        archive.extractall(target)
    lib.run(sys.executable, lib.ROOT / 'doc' / 'gen_docs_index.py', publish_dir)
    lib.run('git', '-C', publish_dir, 'add', '-A')
    if lib.succeeds('git', '-C', publish_dir, 'diff', '--cached', '--quiet'):
        print('\nNo documentation changes to publish.')
        return
    lib.run('git', '-C', publish_dir, 'commit', '-q', '-m',
            f'Refresh docs for {version}')
    print(f'\nStaged and committed into {publish_dir}. Review the diff, '
          'then push:')
    print(f'  git -C {publish_dir} push origin HEAD:gh-pages')


def parse_args():
    parser = argparse.ArgumentParser(
        description=__doc__,
        formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('version', help='release version to build, e.g. 0.5.4')
    parser.add_argument('--publish', metavar='DIR',
                        help='stage into gh-pages checkout DIR and commit')
    parser.add_argument('--output', metavar='FILE',
                        help='where to copy the built html.zip without --publish')
    parser.add_argument('--worktree', metavar='DIR',
                        help='build worktree location')
    parser.add_argument('--keep-worktree', action='store_true',
                        help='keep the build worktree on success')
    return parser.parse_args()


def main():
    args = parse_args()
    version = args.version
    tag = f'v{version}'

    lib.require('git', 'bazel')
    if not lib.succeeds('git', 'rev-parse', '-q', '--verify',
                        f'{tag}^{{commit}}'):
        raise lib.ReleaseError(f'tag {tag} not found')
    require_clean_tree()

    worktree = (Path(args.worktree) if args.worktree
                else lib.ROOT.parent / f'krpc-worktree-docs-{version}')
    if worktree.exists():
        raise lib.ReleaseError(f'{worktree} already exists')

    publish_dir = None
    if args.publish:
        publish_dir = Path(args.publish).resolve()
        if not (publish_dir / '.git').exists():
            raise lib.ReleaseError(
                f'--publish target {publish_dir} is not a git checkout')

    ref = content_ref(version)
    base = lib.capture('git', 'rev-parse', 'HEAD')
    branch = lib.capture('git', 'rev-parse', '--abbrev-ref', 'HEAD')
    print(f'Building {version} docs: content from {ref}, toolchain from '
          f'{branch}.')

    lib.run('git', 'worktree', 'add', '-q', '--detach', worktree, base)
    try:
        graft(worktree, base, ref)
        maybe_write_gamescene_shim(worktree)
        exclude_changelog(worktree)
        pin_version(worktree, version)

        lib.banner('Building the docs (this verifies the graft)')
        run_in(worktree, 'bazel', 'build', '//doc:html')
        run_in(worktree, 'bazel', 'test', '//doc:check-documented')

        zip_path = worktree / 'bazel-bin' / 'doc' / 'html.zip'
        check_no_changelog(zip_path)

        if publish_dir:
            publish(zip_path, publish_dir, version)
        else:
            output = (Path(args.output) if args.output
                      else lib.ROOT / f'krpc-doc-{version}.zip')
            # html.zip is a read-only Bazel output; copy its contents into a
            # fresh writable file, replacing any read-only one an earlier run
            # left behind, rather than cloning its mode.
            output.unlink(missing_ok=True)
            shutil.copyfile(zip_path, output)
            print(f'\nBuilt site: {output}')
            print(f'Inspect it with: unzip -l "{output}"')
    except BaseException:
        lib.error(f'the build worktree is left at {worktree} for inspection')
        raise

    if args.keep_worktree:
        print(f'\nBuild worktree kept at {worktree} (remove with: '
              f'git worktree remove {worktree}).')
    else:
        lib.run('git', 'worktree', 'remove', '--force', worktree)
    print('\nDone.')


if __name__ == '__main__':
    lib.main(main)
