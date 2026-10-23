"""Check that a .csproj links the generated code of the services it should.

The generated files are linked from bazel-bin, which //:csproj-test leaves unchecked.
Each argument is a .csproj path, then "=" and the comma separated service names.
"""

import sys
import xml.etree.ElementTree as ET


def linked_services(csproj):
    """Return the names of the services whose generated code the .csproj links."""
    names = set()
    for element in ET.parse(csproj).iter():
        if element.tag.rsplit("}", 1)[-1] != "Link" or element.text is None:
            continue
        link = element.text.replace("\\", "/")
        if link.startswith("Services/") and link.endswith(".cs"):
            names.add(link[len("Services/") : -len(".cs")])
    return names


def main():
    failed = False
    for arg in sys.argv[1:]:
        csproj, names = arg.split("=")
        expected = set(names.split(","))
        actual = linked_services(csproj)
        for name in sorted(expected - actual):
            print(f"{csproj}: missing the generated code of {name}")
            failed = True
        for name in sorted(actual - expected):
            print(f"{csproj}: links {name}, which is not a service")
            failed = True
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
