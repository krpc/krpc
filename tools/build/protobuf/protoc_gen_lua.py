"""protoc plugin that generates lua code.

Wrapper that runs the protoc-gen-lua plugin script from @protoc_lua on the
hermetic python toolchain, with the protobuf package from the pip hub.
"""

import runpy

from python.runfiles import runfiles

if __name__ == "__main__":
    r = runfiles.Create()
    plugin = r.Rlocation("protoc_lua/protoc-plugin/protoc-gen-lua")
    runpy.run_path(plugin, run_name="__main__")
