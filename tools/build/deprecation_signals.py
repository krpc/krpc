"""Compiles a C or C++ source that uses the members of a service, and writes a header holding the
compiler's deprecation warnings for each use, for a conformance testee to report.

A line of the source ending in "// use: <service>.<member>" is the use of that member. A warning on
a line ending in "// setup", which holds what the uses need, is left out. A warning on any other
line fails the build. Usage:

    deprecation_signals.py <out> <source> <compiler kind> <compiler> [<flag>...]

The compiler kind is the toolchain's, such as "msvc-cl".
"""

import json
import os
import re
import subprocess
import sys

# A toolchain can turn on colors in the compiler's output
_COLOR = re.compile(r"\x1b\[[0-9;]*m")
_USE = re.compile(r"//\s*use:\s*(\S+)\s*$")
_SETUP = re.compile(r"//\s*setup\s*$")
# A warning in the form GCC and Clang print, then in the form MSVC prints
_GNU_WARNING = re.compile(
    r"^(.*?):(\d+):\d+: warning: (.*?) \[-Wdeprecated[^\]]*\]$", re.MULTILINE
)
_MSVC_WARNING = re.compile(
    r"^(.*?)\((\d+)(?:,\d+)?\): warning C4996: (.*)$", re.MULTILINE
)


def _warnings(output: str) -> list[tuple[str, int, str]]:
    """The deprecation warnings a compiler printed, as the file, line and text."""
    found = [*_GNU_WARNING.finditer(output), *_MSVC_WARNING.finditer(output)]
    return [(x[1], int(x[2]), x[3]) for x in found]


def _signals(source: str, output: str) -> dict[str, list[str]]:
    with open(source, encoding="utf-8") as lines:
        tags = dict(enumerate(lines, start=1))
    signals = {x[1]: [] for x in map(_USE.search, tags.values()) if x}
    for path, line, text in _warnings(output):
        if os.path.normcase(os.path.normpath(path)) != os.path.normcase(
            os.path.normpath(source)
        ):
            raise ValueError(
                "A warning in generated code: %s:%d: %s" % (path, line, text)
            )
        use = _USE.search(tags[line])
        if use:
            signals[use[1]].append(text)
        elif not _SETUP.search(tags[line]):
            raise ValueError(
                "A warning on line %d, which uses no member: %s" % (line, text)
            )
    return signals


def _literal(text: str) -> str:
    """A C++ string literal. A JSON string of ASCII characters only is also a valid one."""
    return json.dumps(text)


def _check_flags(kind: str, flags: list[str]) -> list[str]:
    """The flags that check the source for warnings, with no output. The MSVC toolchain turns
    off C4996, a level 3 warning, and the default warning level is 1."""
    if kind != "msvc-cl":
        return [*flags, "-fsyntax-only"]
    flags = [x for x in flags if x.lower() not in ("/wd4996", "-wd4996", "/c", "-c")]
    return [*flags, "/W3", "/Zs"]


def main() -> int:
    out, source, kind, compiler, *flags = sys.argv[1:]
    result = subprocess.run(
        [compiler, *_check_flags(kind, flags)],
        capture_output=True,
        encoding="utf-8",
        errors="replace",
        check=False,
    )
    output = _COLOR.sub("", result.stdout + result.stderr)
    if result.returncode != 0:
        sys.stderr.write(output)
        return result.returncode
    try:
        signals = _signals(source, output)
    except ValueError as exn:
        sys.stderr.write("%s\n%s" % (exn, output))
        return 1
    with open(out, "w", encoding="utf-8", newline="\n") as header:
        header.write(
            "// The compiler's deprecation warnings for %s, generated.\n" % source
        )
        header.write(
            "#pragma once\n\n#include <map>\n#include <string>\n#include <vector>\n\n"
        )
        header.write("static const std::map<std::string, std::vector<std::string>>\n")
        header.write("    kDeprecationSignals = {\n")
        for member, texts in sorted(signals.items()):
            items = ", ".join(_literal(x) for x in texts)
            header.write("        {%s, {%s}},\n" % (_literal(member), items))
        header.write("};\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
