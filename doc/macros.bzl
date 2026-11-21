" macros "

load("@bazel_skylib//rules:write_file.bzl", "write_file")
load("//service:registry.bzl", "SERVICES")

def api_toctree(name, language):
    """Writes the toctree of a client guide's API reference, src/<language>/api.inc.

    The pages are in three groups: the core's services, the stock services, then the mod
    services. The services keep their order in the registry.

    Args:
      name: The target name.
      language: The language of the client guide.
    """
    groups = [
        ("Server", ["krpc", "stdlib"]),
        ("Game", [service.doc for service in SERVICES if not service.mods]),
        ("Mods", [service.doc for service in SERVICES if service.mods]),
    ]
    content = []
    for caption, pages in groups:
        if content:
            content.append("")
        content += [".. toctree::", "   :caption: " + caption, "   :maxdepth: 1", ""]
        content += ["   %s/api/%s" % (language, page) for page in pages]
    write_file(
        name = name,
        out = "src/%s/api.inc" % language,
        content = content,
    )
