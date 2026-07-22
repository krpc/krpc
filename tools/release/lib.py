"""Shared helpers for the release scripts. Import this, don't run it.

Importing this moves to the root of the repository, so that every step can use
paths relative to it however it was invoked.
"""

import json
import os
import re
import shutil
import subprocess
import sys
import tomllib
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
os.chdir(ROOT)


class ReleaseError(Exception):
    """A step cannot continue.

    Raised for anything the person running the release needs to fix, and
    reported as a plain error rather than a traceback.
    """


def _version():
    match = re.search(r'^version\s*=\s*"(.+)"$',
                      Path('config.bzl').read_text(), re.MULTILINE)
    if match is None:
        raise ReleaseError('no version found in config.bzl')
    return match.group(1)


VERSION = _version()
TAG = 'v' + VERSION


# Colors, dropped when the output is not going somewhere that renders them, so
# redirecting a release log to a file keeps it readable. NO_COLOR is the
# conventional opt-out (https://no-color.org).
if (sys.stdout.isatty() and not os.environ.get('NO_COLOR')
        and os.environ.get('TERM', 'dumb') != 'dumb'):
    RESET = '\033[0m'
    RED = '\033[31m'
    GREEN = '\033[32m'
    YELLOW = '\033[33m'
    BOLD = '\033[1m'
    DIM = '\033[2m'
else:
    RESET = RED = GREEN = YELLOW = BOLD = DIM = ''


def banner(message):
    print()
    print(f'{BOLD}==> {message}{RESET}')


def error(message):
    print(f'{RED}error:{RESET} {message}', file=sys.stderr)


def warning(message):
    print(f'{YELLOW}warning:{RESET} {message}', file=sys.stderr)


def confirm(question):
    """Ask before doing anything that publishes externally.

    Anything other than an explicit yes aborts the step.
    """
    if input(f'{question} [y/N] ').strip().lower() != 'y':
        raise ReleaseError('aborted')


def run(*command, env=None, stdin=None):
    """Run a command, failing the step if it does.

    The command is echoed first, so that a release log shows what was actually
    run and with which arguments.
    """
    command = [str(argument) for argument in command]
    print(f'{DIM}$ {" ".join(command)}{RESET}')
    status = subprocess.run(command, env=env, input=stdin, text=stdin is not None)
    if status.returncode != 0:
        raise ReleaseError(
            f'{command[0]} failed with exit status {status.returncode}')


def capture(*command, env=None):
    """Run a command and return its standard output, stripped."""
    command = [str(argument) for argument in command]
    status = subprocess.run(command, capture_output=True, text=True, env=env)
    if status.returncode != 0:
        raise ReleaseError(
            f'{command[0]} failed with exit status {status.returncode}: '
            f'{status.stderr.strip()}')
    return status.stdout.strip()


def succeeds(*command, env=None):
    """Whether a command exits zero, with its output discarded."""
    return subprocess.run([str(argument) for argument in command], env=env,
                          stdout=subprocess.DEVNULL,
                          stderr=subprocess.DEVNULL).returncode == 0


def require(*tools):
    for tool in tools:
        if shutil.which(tool) is None:
            raise ReleaseError(
                f"'{tool}' is required for this step but was not found")


def require_clean_tree():
    if capture('git', 'status', '--porcelain'):
        raise ReleaseError('the working tree has uncommitted changes')


def changelogs():
    """Every release-train component's CHANGELOG.md, in a stable order.

    The same set the release notes are built from: server, core, the services,
    the clients and krpctools. Components on their own version train (buildenv)
    and ones not shipped in the release notes (krpctest) are deliberately absent.
    """
    return (['server/CHANGELOG.md', 'core/CHANGELOG.md']
            + sorted(str(path) for path in Path('service').glob('*/CHANGELOG.md'))
            + sorted(str(path) for path in Path('client').glob('*/CHANGELOG.md'))
            + ['tools/krpctools/CHANGELOG.md'])


def strip_unreleased():
    """Drop the ' - unreleased' suffix from the current version's header in
    every component changelog, returning the paths that changed.

    While on main the in-development version's header reads
    ``## [vX.Y.Z] - unreleased``; releasing turns it into ``## [vX.Y.Z]``.
    """
    header = f'## [v{VERSION}] - unreleased'
    replacement = f'## [v{VERSION}]'
    changed = []
    for path in changelogs():
        file = Path(path)
        text = file.read_text()
        if header in text:
            file.write_text(text.replace(header, replacement))
            changed.append(path)
    return changed


# The single file every publish step takes its credentials from, so there is
# one place to keep up to date instead of one configuration per tool. Each step
# passes the value to its tool explicitly, overriding any account the tool is
# already configured with. Overridable, so that anything exercising these
# scripts can point at a file of its own rather than the real one.
CREDENTIALS_FILE = Path(os.environ.get('CREDENTIALS_FILE',
                                       'release-credentials.toml'))
CREDENTIALS_TEMPLATE = Path(
    'tools/release/release-credentials.toml.template')

# Every key the credentials file must define. Git is deliberately absent:
# pushes to krpc/krpc and krpc-arduino use the normal development git setup.
CREDENTIAL_KEYS = (
    'GITHUB_TOKEN',
    'PYPI_TOKEN_KRPC',
    'PYPI_TOKEN_KRPCTOOLS',
    'NUGET_API_KEY',
    'LUAROCKS_API_KEY',
    'AWS_ACCESS_KEY_ID',
    'AWS_SECRET_ACCESS_KEY',
)


def read_credentials_file():
    """Parse the credentials file, or return None if there isn't one.

    TOML, so that the quoting of a value holding punctuation is unambiguous
    and the file can carry comments saying what each key is.
    """
    if not CREDENTIALS_FILE.is_file():
        return None
    if CREDENTIALS_FILE.stat().st_mode & 0o077:
        warning(f'{CREDENTIALS_FILE} is readable by other users; '
                f"run 'chmod 600 {CREDENTIALS_FILE}'")
    try:
        with CREDENTIALS_FILE.open('rb') as credentials_file:
            credentials = tomllib.load(credentials_file)
    except tomllib.TOMLDecodeError as exception:
        raise ReleaseError(
            f'{CREDENTIALS_FILE} is not valid TOML: {exception}. Every value '
            f'must be quoted, as in GITHUB_TOKEN = "ghp_..."') from exception
    # A value left unquoted parses as a number, a boolean or a table rather
    # than failing, so it would otherwise reach the tool as the wrong type
    unquoted = sorted(key for key, value in credentials.items()
                      if not isinstance(value, str))
    if unquoted:
        raise ReleaseError(
            f'{CREDENTIALS_FILE}: not quoted: {" ".join(unquoted)}')
    return credentials


def load_credentials():
    """Read the credentials, requiring all of them.

    A release that stops halfway through for a missing credential is worse than
    one that refuses to start, so a step that needs any credential insists on
    the whole set being present, not just the keys it uses itself.
    """
    credentials = read_credentials_file()
    if credentials is None:
        raise ReleaseError(
            f'{CREDENTIALS_FILE} not found; copy {CREDENTIALS_TEMPLATE} to it '
            f'and fill it in')
    missing = [key for key in CREDENTIAL_KEYS if not credentials.get(key)]
    if missing:
        raise ReleaseError(
            f'{CREDENTIALS_FILE} does not define: {" ".join(missing)}')
    return credentials


def github_user(token):
    """Return the (login, scopes) of a GitHub token.

    Scopes are None for a fine-grained token, which does not report them.
    Raises for a token GitHub rejects, or if GitHub cannot be reached.
    """
    request = urllib.request.Request(
        'https://api.github.com/user',
        headers={'Authorization': f'Bearer {token}',
                 'Accept': 'application/vnd.github+json'})
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            login = json.load(response)['login']
            header = response.headers.get('X-OAuth-Scopes')
    except urllib.error.HTTPError as exception:
        raise ReleaseError(
            f'GitHub rejected the token ({exception.code} '
            f'{exception.reason})') from exception
    except urllib.error.URLError as exception:
        raise ReleaseError(
            f'could not reach GitHub: {exception.reason}') from exception
    if header is None:
        return login, None
    return login, {scope.strip() for scope in header.split(',') if scope.strip()}


def luarocks_key_works(key):
    """Whether luarocks.org accepts an API key.

    Asks the endpoint luarocks itself uses to test a key before storing it.
    """
    url = f'https://luarocks.org/api/1/{urllib.parse.quote(key, safe="")}/status'
    try:
        with urllib.request.urlopen(url, timeout=30) as response:
            return 'errors' not in json.load(response)
    except (urllib.error.HTTPError, json.JSONDecodeError):
        return False
    except urllib.error.URLError as exception:
        raise ReleaseError(
            f'could not reach luarocks.org: {exception.reason}') from exception


def aws_environment(credentials):
    """The environment for an AWS command, using the credentials given.

    A profile selected in the environment would otherwise take priority over
    the keys being passed here, so it is removed.
    """
    environment = dict(os.environ)
    environment.pop('AWS_PROFILE', None)
    environment.pop('AWS_DEFAULT_PROFILE', None)
    environment['AWS_ACCESS_KEY_ID'] = credentials['AWS_ACCESS_KEY_ID']
    environment['AWS_SECRET_ACCESS_KEY'] = credentials['AWS_SECRET_ACCESS_KEY']
    return environment


def twine_upload(token, distribution):
    """Upload a distribution to PyPI as the given API token.

    PyPI scopes a token to a single project, so each package has its own and
    the caller passes the one that matches. The empty config file keeps a
    ~/.pypirc from supplying a different account, and --non-interactive turns a
    credential problem into an error rather than a password prompt.
    """
    require('twine')
    environment = dict(os.environ,
                       TWINE_USERNAME='__token__', TWINE_PASSWORD=token)
    run('twine', 'upload', '--config-file', '/dev/null', '--non-interactive',
        distribution, env=environment)


def main(step):
    """Run a step, reporting a failure as an error rather than a traceback."""
    try:
        step()
    except ReleaseError as exception:
        error(str(exception))
        sys.exit(1)
    except KeyboardInterrupt:
        error('interrupted')
        sys.exit(130)
