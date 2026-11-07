"Generated"

load(":paket.tools.bzl", _tools = "tools")

def _tools_impl(module_ctx):
    _tools()
    return module_ctx.extension_metadata(reproducible = True)

tools_extension = module_extension(
    implementation = _tools_impl,
)
