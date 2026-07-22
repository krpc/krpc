#!/usr/bin/env python3
"""Sanity-checks the repository and environment before starting a release.

Read-only: performs no builds and publishes nothing.
"""

import os
import shutil
from pathlib import Path

import lib

# Reported as a failure if either is missing: the first to create the release,
# the second to push the TestServer image to ghcr.io
REQUIRED_GITHUB_SCOPES = ('repo', 'write:packages')

GREEN_BOLD = lib.GREEN + lib.BOLD
RED_BOLD = lib.RED + lib.BOLD


class Report:
    """The check report.

    One line per check, as against lib's error and warning, which report a step
    going wrong. A failure is the thing to spot when scanning a long report, so
    it is colored throughout rather than only on its marker.
    """

    def __init__(self):
        self.failures = 0

    def ok(self, message):
        print(f' {lib.GREEN}✓{lib.RESET} {message}')

    def warn(self, message):
        print(f' {lib.YELLOW}!{lib.RESET} {message}')

    def fail(self, message):
        print(f' {lib.RED}✗ {message}{lib.RESET}')
        self.failures += 1


def check_repository(report):
    lib.banner('Repository state')
    if lib.capture('git', 'status', '--porcelain'):
        report.fail('working tree has uncommitted changes')
    else:
        report.ok('working tree is clean')

    branch = lib.capture('git', 'rev-parse', '--abbrev-ref', 'HEAD')
    if branch == 'main':
        report.ok('on branch main')
    else:
        report.fail(f"on branch '{branch}', expected main")

    # Quietly, and reporting a failure rather than raising: a check that
    # cannot be carried out is one line of the report, not the end of it
    if not lib.succeeds('git', 'fetch', '-q', 'origin', 'main'):
        report.fail('could not fetch origin/main to compare against')
    elif lib.capture('git', 'rev-parse', 'HEAD') == \
            lib.capture('git', 'rev-parse', 'origin/main'):
        report.ok('HEAD matches origin/main')
    else:
        report.fail('HEAD does not match origin/main '
                    '(unpushed or missing commits)')

    if lib.succeeds('git', 'rev-parse', '-q', '--verify', f'refs/tags/{lib.TAG}'):
        if lib.capture('git', 'rev-parse', f'{lib.TAG}^{{commit}}') == \
                lib.capture('git', 'rev-parse', 'HEAD'):
            report.ok(f'tag {lib.TAG} exists and points at HEAD')
        else:
            report.fail(f'tag {lib.TAG} exists but does not point at HEAD')
    else:
        report.ok(f'tag {lib.TAG} not created yet (30-tag.py will create it)')


def check_changelogs(report):
    lib.banner(f'Changelogs (a component with no user-facing changes has no '
               f'{lib.TAG} section and is omitted from the release notes)')
    # Matches '## [vX.Y.Z]' with or without the ' - unreleased' suffix.
    header = f'## [v{lib.VERSION}]'
    for changelog in lib.changelogs():
        if any(line.startswith(header)
               for line in Path(changelog).read_text().splitlines()):
            report.ok(changelog)
        else:
            print(f' - {changelog} has no {lib.TAG} section '
                  f'(omitted; intentional if it has no user-facing changes)')


def check_tools(report):
    lib.banner('Tools')
    for tool in ('bazel', 'git', 'gh'):
        if shutil.which(tool):
            report.ok(tool)
        else:
            report.fail(f'{tool} not found (required)')
    # Needed by the individual publish steps; missing ones only matter for the
    # steps that use them, so they warn rather than fail.
    for tool in ('twine', 'dotnet', 'docker', 'luarocks', 'aws'):
        if shutil.which(tool):
            report.ok(tool)
        else:
            report.warn(f'{tool} not found (needed by one of the publish steps)')
    # vcpkg lives at $VCPKG_ROOT/vcpkg rather than on PATH, so it is checked
    # separately; only the two vcpkg publish steps need it.
    root = os.environ.get('VCPKG_ROOT')
    if root and (Path(root) / 'vcpkg').is_file():
        report.ok('vcpkg (VCPKG_ROOT)')
    else:
        report.warn('vcpkg not found (set VCPKG_ROOT for the vcpkg publish steps)')


def check_github(report, token):
    try:
        login, scopes = lib.github_user(token)
    except lib.ReleaseError as exception:
        report.fail(f'GITHUB_TOKEN: {exception}')
        return
    report.ok(f'GITHUB_TOKEN authenticates as {login}')
    if scopes is None:
        report.warn('GITHUB_TOKEN does not report its scopes; check it can '
                    'create releases and push packages')
        return
    for scope in REQUIRED_GITHUB_SCOPES:
        if scope in scopes:
            report.ok(f'GITHUB_TOKEN has the {scope} scope')
        else:
            report.fail(f'GITHUB_TOKEN is missing the {scope} scope '
                        f'(has {", ".join(sorted(scopes))})')


def check_aws(report, credentials):
    if not shutil.which('aws'):
        report.warn('cannot check the AWS credentials without the aws command')
        return
    environment = lib.aws_environment(credentials)
    try:
        arn = lib.capture('aws', 'sts', 'get-caller-identity',
                          '--query', 'Arn', '--output', 'text',
                          env=environment)
    except lib.ReleaseError:
        report.fail('the AWS credentials were rejected')
        return
    report.ok(f'AWS credentials authenticate as {arn}')
    # A key allowed to write only under s3://krpc/lua/ cannot necessarily read
    # the bucket itself, so this failing does not mean the upload will
    if lib.succeeds('aws', 's3api', 'head-bucket', '--bucket', 'krpc',
                    env=environment):
        report.ok('the AWS credentials can reach s3://krpc')
    else:
        report.warn('the AWS credentials cannot read s3://krpc; '
                    'check they may write s3://krpc/lua/')


def check_credentials(report):
    lib.banner('Credentials')
    credentials = lib.read_credentials_file()
    if credentials is None:
        report.fail(f'{lib.CREDENTIALS_FILE} not found '
                    f'(copy {lib.CREDENTIALS_TEMPLATE} and fill it in)')
        return
    report.ok(f'{lib.CREDENTIALS_FILE} found')
    for key in lib.CREDENTIAL_KEYS:
        if credentials.get(key):
            report.ok(f'{key} is set')
        else:
            report.fail(f'{key} is not set in {lib.CREDENTIALS_FILE}')

    # GitHub, luarocks.org and AWS can all be asked whether a credential works
    # without publishing anything, so a bad one is a failure. PyPI and
    # nuget.org offer no such check: the only thing that exercises those keys
    # is an upload, which cannot be undone. They get a check of the format the
    # services issue, which catches a truncated or misplaced value but cannot
    # tell whether the key is live, so a surprise there is a warning.
    if credentials.get('GITHUB_TOKEN'):
        check_github(report, credentials['GITHUB_TOKEN'])

    for key in ('PYPI_TOKEN_KRPC', 'PYPI_TOKEN_KRPCTOOLS'):
        if credentials.get(key) and not credentials[key].startswith('pypi-'):
            report.warn(f"{key} does not look like a PyPI token "
                        f"(they start with 'pypi-')")
    if credentials.get('NUGET_API_KEY') and \
            not credentials['NUGET_API_KEY'].startswith('oy2'):
        report.warn("NUGET_API_KEY does not look like a nuget.org key "
                    "(they start with 'oy2')")

    if credentials.get('LUAROCKS_API_KEY'):
        if lib.luarocks_key_works(credentials['LUAROCKS_API_KEY']):
            report.ok('LUAROCKS_API_KEY is accepted by luarocks.org')
        else:
            report.fail('LUAROCKS_API_KEY was rejected by luarocks.org')

    if credentials.get('AWS_ACCESS_KEY_ID') and \
            credentials.get('AWS_SECRET_ACCESS_KEY'):
        check_aws(report, credentials)


def main():
    lib.banner(f'Releasing kRPC {lib.VERSION} (tag {lib.TAG})')
    report = Report()
    check_repository(report)
    check_changelogs(report)
    check_tools(report)
    check_credentials(report)

    lib.banner('Result')
    if report.failures == 0:
        print(f'{GREEN_BOLD}All checks passed.{lib.RESET} '
              f'Next: tools/release/20-build-and-test.py')
    else:
        print(f'{RED_BOLD}{report.failures} check(s) failed.{lib.RESET}')
        raise SystemExit(1)


if __name__ == '__main__':
    lib.main(main)
