from __future__ import annotations

import re

# The words of a name, as the client design page specifies them. Each alternative is a rule, tried
# in order: a compound word, a plural acronym, an acronym, a word, digits, and any other character.
# A word takes the digits after it and the lower case letters after those, unless an acronym follows
# the digits.
_DIGITS = r"(?:[0-9]+(?![0-9])(?![A-Z]+(?![a-z]))[a-z]*)?"
_WORD = re.compile(
    r"(?:UInt|ULong|SInt|FoV)(?![a-z])" + _DIGITS
    + r"|[A-Z]{2,}s(?![a-z])" + _DIGITS
    + r"|[A-Z]+(?![a-z])" + _DIGITS
    + r"|[A-Z]?[a-z]+" + _DIGITS
    + r"|[0-9]+(?:[A-Z]+(?![a-z])|[a-z]+)?"
    + r"|."
)  # fmt: skip


def words(name: str) -> list[str]:
    """The words of a name with no underscore, e.g. UInt64ToString -> UInt64, To, String"""
    return _WORD.findall(name)


def snake_case(name: str) -> str:
    """Convert a name to snake case, e.g. GetServices -> get_services"""
    return "__".join("_".join(words(part)).lower() for part in name.split("_"))


def upper_snake_case(name: str) -> str:
    """Convert a name to upper snake case, e.g. GetServices -> GET_SERVICES"""
    return snake_case(name).upper()


def lower_camel_case(name: str) -> str:
    """Convert a name to lower camel case, e.g. GetServices -> getServices"""
    parts = []
    for part in name.split("_"):
        part_words = words(part)
        if part_words:
            part_words[0] = part_words[0].lower()
        parts.append("".join(part_words))
    return "_".join(parts)
