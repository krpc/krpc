"""Writes a protobuf descriptor set as a lua module returning it as a string.

Every byte is written as a decimal escape, which each lua version reads the same way.
"""

import sys

LINE_BYTES = 24


def main() -> int:
    descriptor_path, output_path = sys.argv[1:]
    with open(descriptor_path, "rb") as f:
        data = f.read()
    lines = []
    for start in range(0, len(data), LINE_BYTES):
        chunk = data[start : start + LINE_BYTES]
        lines.append("  '" + "".join("\\%d" % byte for byte in chunk) + "'")
    with open(output_path, "w", newline="\n") as f:
        f.write("-- Generated from krpc.proto: its descriptor set, loaded by pb.load\n")
        f.write("return table.concat({\n")
        f.write(",\n".join(lines))
        f.write("\n})\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
