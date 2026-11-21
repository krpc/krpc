"""The examples a game's tests cannot run, listed in its unsupported.txt.

Each line is "<language> <key>  # reason", where the language is "*" for every language that
has the example. Blank lines and lines starting with "#" are ignored."""

from __future__ import annotations

import pathlib
import re
from dataclasses import dataclass

from tools.doc_examples.spec import CLIENT_FOLDERS

_LINE = re.compile(r"^(?P<language>\S+)\s+(?P<key>\S+)\s+#\s*(?P<reason>\S.*)$")


@dataclass(frozen=True)
class Entry:
    language: str
    key: str
    reason: str
    line: int

    def matches(self, language: str, key: str) -> bool:
        return self.key == key and self.language in ("*", language)


class ListingError(Exception):
    pass


def read(listing: pathlib.Path) -> list[Entry]:
    name = listing.name
    entries = []
    for number, text in enumerate(listing.read_text().splitlines(), 1):
        text = text.strip()
        if not text or text.startswith("#"):
            continue
        match = _LINE.match(text)
        if not match:
            raise ListingError(
                f"{name}:{number}: expected '<language> <key>  # reason'"
            )
        language = match["language"]
        if language != "*" and language not in CLIENT_FOLDERS:
            raise ListingError(f"{name}:{number}: unknown language {language}")
        entries.append(Entry(language, match["key"], match["reason"], number))
    return entries


def find(entries: list[Entry], language: str, key: str) -> Entry | None:
    return next((e for e in entries if e.matches(language, key)), None)
