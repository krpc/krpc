"""Runs Doxygen over a set of headers, failing on any warning or unlinked reference.

  run_doxygen.py [--c] [--unlinked NAME...] --doxygen DOXYGEN... -- HEADER...

DOXYGEN names the files of the Doxygen release, of which the executable is the
one run, and HEADER the headers to document, all runfiles-root-relative. --c
reads the headers as C rather than C++.

Doxygen writes a name it cannot link as plain text, without a warning. So the
documentation Doxygen writes is also searched for the names of the client's API,
and each has to be a link. A NAME given to --unlinked may stay plain text.
"""

import argparse
import glob
import os
import re
import subprocess
import sys
import tempfile
from xml.etree import ElementTree

# Every entity is documented, so a comment Doxygen cannot read fails the test
# whether or not the entity it describes has documentation of its own. The C
# client is static inline functions, which Doxygen leaves out by default.
CONFIG = """
INPUT = {inputs}
OUTPUT_DIRECTORY = {output}
OPTIMIZE_OUTPUT_FOR_C = {c}
EXTRACT_ALL = YES
EXTRACT_STATIC = YES
GENERATE_HTML = NO
GENERATE_LATEX = NO
GENERATE_XML = YES
QUIET = YES
WARN_AS_ERROR = FAIL_ON_WARNINGS_PRINT
"""

# The names of the API, as the documentation refers to them: a function, a type, a
# structure field and an enumeration value in C, and a qualified name in C++
C_NAMES = re.compile(
    r"(?<![\w#])(?:krpc_\w+\(\)|krpc_\w+_t\.\w+|krpc_\w+_t|#?KRPC_[A-Z0-9_]+)(?!\w)"
)
CPP_NAMES = re.compile(r"(?<![\w:])krpc::services::\w+(?:::\w+)*(?:\(\))?")

# The elements of the XML output that hold documentation
DESCRIPTIONS = ("briefdescription", "detaileddescription", "inbodydescription")

# The elements within it whose text is no reference: a link, and code
SKIPPED = ("ref", "computeroutput", "programlisting", "verbatim")


def executable(files):
    for path in files:
        if os.path.basename(path) in ("doxygen", "doxygen.exe"):
            # Run by absolute path, as a relative one is interpreted against the
            # search path rather than the working directory on some platforms
            return os.path.abspath(path)
    raise RuntimeError("No Doxygen executable among %s" % files)


def config_path(path):
    """A path as the Doxygen configuration holds it. A backslash escapes a quote there,
    and Doxygen accepts forward slashes on Windows."""
    return os.path.abspath(path).replace("\\", "/")


def text(element):
    """The text of an element, leaving out the elements whose text is no reference"""
    parts = [element.text or ""]
    for child in element:
        if child.tag not in SKIPPED:
            parts.append(text(child))
        parts.append(child.tail or "")
    return "".join(parts)


def unlinked(xml_dir, names):
    """The names of the API the documentation refers to without a link"""
    found = set()
    for path in glob.glob(os.path.join(xml_dir, "*.xml")):
        root = ElementTree.parse(path).getroot()
        for tag in DESCRIPTIONS:
            for description in root.iter(tag):
                found.update(names.findall(text(description)))
    return found


def run(doxygen, headers, c, allowed):
    with tempfile.TemporaryDirectory(dir=os.environ.get("TEST_TMPDIR")) as output:
        config = CONFIG.format(
            inputs=" ".join('"%s"' % config_path(h) for h in headers),
            output='"%s"' % config_path(output),
            c="YES" if c else "NO",
        )
        path = os.path.join(output, "Doxyfile")
        with open(path, "w", encoding="utf-8") as config_file:
            config_file.write(config)
        print("+ %s Doxyfile, over %d headers" % (doxygen, len(headers)))
        sys.stdout.flush()
        result = subprocess.run([doxygen, path], check=False).returncode
        if result != 0:
            return result
        found = unlinked(os.path.join(output, "xml"), C_NAMES if c else CPP_NAMES)
    for name in sorted(found - allowed):
        print("error: %s is not a link" % name)
    return 1 if found - allowed else 0


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--c", action="store_true")
    parser.add_argument("--unlinked", nargs="*", default=[])
    parser.add_argument("--doxygen", nargs="+", required=True)
    parser.add_argument("headers", nargs="+")
    options = parser.parse_args()
    return run(
        executable(options.doxygen), options.headers, options.c, set(options.unlinked)
    )


if __name__ == "__main__":
    sys.exit(main())
