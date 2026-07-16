"""protoc plugin that generates nanopb code.

Wrapper that runs the nanopb generator from @protoc_nanopb as a protoc
plugin on the hermetic python toolchain, with the protobuf package from the
pip hub.
"""

import os
import runpy
import sys

from python.runfiles import runfiles

if __name__ == "__main__":
    r = runfiles.Create()
    generator = r.Rlocation("protoc_nanopb/generator/nanopb_generator.py")
    # The generator imports its bundled proto package from its own directory
    sys.path.insert(0, os.path.dirname(generator))
    sys.argv = [generator, "--protoc-plugin"]
    runpy.run_path(generator, run_name="__main__")
