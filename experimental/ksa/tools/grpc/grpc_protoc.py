"""Generate the Python gRPC modules: grpc_protoc.py OUT_DIR PROTO_DIR PROTO..."""

import sys
from importlib.resources import files

from grpc_tools import protoc


def main() -> int:
    out, proto_dir, *protos = sys.argv[1:]
    # The well-known types, such as google/protobuf/empty.proto
    well_known = str(files("grpc_tools") / "_proto")
    return protoc.main(
        [
            "grpc_tools.protoc",
            "-I" + proto_dir,
            "-I" + well_known,
            "--python_out=" + out,
            "--grpc_python_out=" + out,
            *protos,
        ]
    )


if __name__ == "__main__":
    sys.exit(main())
