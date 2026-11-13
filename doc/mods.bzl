" the docs' mod links and tested versions, from the registry "

def _versions(mod):
    versions = [v.version for v in mod.versions]
    if len(versions) < 2:
        return "".join(versions)
    return ", ".join(versions[:-1]) + " and " + versions[-1]

def _documented(mods, services):
    """The mods the services wrap or support, in the order the services list them."""
    by_name = {m.name: m for m in mods}
    names = []
    for s in services:
        for name in s.mods + s.supports:
            if name not in names:
                names.append(name)
    return [by_name[name] for name in names]

def mods_rst(mods, services):
    """A link target and a 'Tested with' substitution for each mod a service wraps or supports.

    Args:
      mods: MODS from the registry.
      services: SERVICES from the registry.

    Returns:
      The rst, for a page to include.
    """
    lines = [".. Generated from service/registry.bzl", ""]
    for m in _documented(mods, services):
        lines.append(".. _%s: https://github.com/%s" % (m.title, m.repo))
        if m.versions:
            lines.append(".. |%s tested| replace:: Tested with %s %s." % (m.title, m.title, _versions(m)))
    return "\n".join(lines) + "\n"

def _table(header, rows):
    lines = [".. list-table::", "   :header-rows: 1", ""]
    for row in [header] + rows:
        lines.append("   * - " + row[0])
        lines.extend(["     - " + cell for cell in row[1:]])
    return lines

def supported_mods_rst(mods, services, name):
    """A table of the tested versions of the mods a service supports.

    Args:
      mods: MODS from the registry.
      services: SERVICES from the registry.
      name: The service.

    Returns:
      The rst, for the service's page to include. It links each mod through the targets in
      mods_rst.
    """
    by_name = {m.name: m for m in mods}
    service = [s for s in services if s.name == name][0]
    rows = [["`%s`_" % by_name[n].title, _versions(by_name[n])] for n in service.supports]
    lines = [".. Generated from service/registry.bzl", ""]
    lines.extend(_table(["Mod", "Tested versions"], rows))
    return "\n".join(lines) + "\n"

def mod_services_rst(mods, services):
    """A table of the services that wrap a mod, with the mod's tested versions.

    Args:
      mods: MODS from the registry.
      services: SERVICES from the registry.

    Returns:
      The rst, for a page in the same directory as the services' pages to include. A mod with
      no tested version is left out.
    """
    by_name = {m.name: m for m in mods}
    rows = [
        [":doc:`%s <%s>`" % (by_name[n].title, s.doc), _versions(by_name[n])]
        for s in services
        for n in s.mods
        if by_name[n].versions
    ]
    lines = [".. Generated from service/registry.bzl", ""]
    lines.extend(_table(["Mod", "Tested versions"], rows))
    return "\n".join(lines) + "\n"
