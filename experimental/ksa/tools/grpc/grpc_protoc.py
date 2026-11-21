"""Generate the Python gRPC modules: grpc_protoc.py OUT_DIR PROTO..."""

import os
import sys
from importlib.resources import files

from grpc_tools import protoc


def main() -> int:
    out, *protos = sys.argv[1:]
    # A proto imports the others by name, so each one's folder is on the include path
    includes = sorted({os.path.dirname(x) for x in protos})
    # The well-known types, such as google/protobuf/empty.proto
    well_known = str(files("grpc_tools") / "_proto")
    return protoc.main(
        [
            "grpc_tools.protoc",
            *("-I" + x for x in includes),
            "-I" + well_known,
            "--python_out=" + out,
            "--grpc_python_out=" + out,
            *protos,
        ]
    )


if __name__ == "__main__":
    sys.exit(main())
