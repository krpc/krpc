"""Installing kRPC, the test add-on and the requested mods into a game.

Sources are local paths or URLs. A download goes to a user cache keyed by its hash, so kRPC
and each mod archive are downloaded once.
"""

import hashlib
import http.client
import os
import shutil
import sys
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request

from .version import __version__


def cache_dir():
    """The directory downloads are cached in."""
    if sys.platform == "win32":
        base = os.environ.get("LOCALAPPDATA") or os.path.expanduser("~")
    elif sys.platform == "darwin":
        base = os.path.expanduser("~/Library/Caches")
    else:
        base = os.environ.get("XDG_CACHE_HOME") or os.path.expanduser("~/.cache")
    return os.path.join(base, "krpctools")


def is_url(source):
    return urllib.parse.urlparse(str(source)).scheme in ("http", "https", "file")


def _sha256(path):
    digest = hashlib.sha256()
    with open(path, "rb") as f:
        for block in iter(lambda: f.read(1 << 20), b""):
            digest.update(block)
    return digest.hexdigest()


# The errors a read can fail with partway through, besides those urllib raises
_DROPPED = (TimeoutError, ConnectionError, http.client.IncompleteRead)


def with_retries(action, description, attempts=4):
    """The result of action(), retried after a server error, a failed connection or a
    dropped read. description names what action does, for the error raised after the last
    attempt."""
    for attempt in range(attempts):
        try:
            return action()
        except (urllib.error.URLError, *_DROPPED) as ex:
            transient = not isinstance(ex, urllib.error.HTTPError) or ex.code >= 500
            if not transient or attempt == attempts - 1:
                raise RuntimeError("Failed to %s: %s" % (description, ex)) from ex
            time.sleep(2**attempt)
    raise AssertionError("unreachable")


def _copy_url(request, f):
    """Copy the response to request into f."""

    def copy():
        f.seek(0)
        f.truncate()
        with urllib.request.urlopen(request, timeout=60) as response:
            shutil.copyfileobj(response, f)

    with_retries(copy, "download " + request.full_url)


def download(url, sha256=None):
    """The path of the file at url, downloaded into the cache unless it is there already.
    Without a sha256 the file is keyed by its URL, and is downloaded once."""
    key = sha256 or hashlib.sha256(url.encode()).hexdigest()
    name = os.path.basename(urllib.parse.urlparse(url).path) or "download"
    directory = os.path.join(cache_dir(), key)
    path = os.path.join(directory, urllib.parse.unquote(name))
    if os.path.exists(path):
        return path
    os.makedirs(directory, exist_ok=True)
    request = urllib.request.Request(
        url, headers={"User-Agent": "krpctools/" + __version__}
    )
    with tempfile.NamedTemporaryFile(dir=directory, delete=False) as f:
        partial = f.name
        try:
            _copy_url(request, f)
        except BaseException:
            f.close()
            os.remove(partial)
            raise
    if sha256 is not None and _sha256(partial) != sha256:
        os.remove(partial)
        raise RuntimeError(
            "Downloaded %s does not match its SHA-256 %s" % (url, sha256)
        )
    os.replace(partial, path)
    return path


def fetch(source, sha256=None):
    """The local path of a source: a path as given, or a URL downloaded into the cache."""
    if source is None:
        raise RuntimeError("No source to install from")
    if is_url(source):
        url = urllib.parse.urlparse(str(source))
        # A local file with no hash to key it by is read in place, so a rebuilt file is
        # installed
        if url.scheme != "file" or sha256 is not None:
            return download(str(source), sha256)
        source = urllib.request.url2pathname(url.path)
    path = os.path.abspath(source)
    if not os.path.exists(path):
        raise RuntimeError("%s not found" % path)
    return path


def requirements(game, mods):
    """The names of the given mods and every mod they require, each listed once."""
    names = []

    def add(name):
        if name in names:
            return
        if name not in game.mods:
            raise ValueError(
                "Unknown mod(s): %s (known: %s)" % (name, ", ".join(sorted(game.mods)))
            )
        names.append(name)
        for required in game.mods[name].requires:
            add(required)

    for name in mods:
        add(name)
    return names


def install(game, mods=()):
    """Install kRPC, the game's test add-on and exactly the given mods and those they
    require. The other mods the game knows are removed."""
    names = requirements(game, mods)
    game.install_krpc(fetch(game.krpc_source()))
    game.install_addon()
    if game.install_mods:
        for name, mod in game.mods.items():
            if name not in names:
                mod.remove(game)
        for name in names:
            mod = game.mods[name]
            if mod.source is None:
                raise RuntimeError(
                    "No source for mod %s. Give one with --mod %s=PATH." % (name, name)
                )
            mod.install(game, fetch(mod.source, mod.sha256))
    if game.check_install:
        game.validate_install(names)
