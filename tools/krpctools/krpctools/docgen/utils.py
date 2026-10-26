# The services the lookup was last built from, and the lookup
_services_lookup = (None, {})


def lookup_cref(cref, services):
    global _services_lookup  # noqa: PLW0603
    if _services_lookup[0] is not services:
        objs = []
        for service in services.values():
            objs.append(service)
            objs.extend(service.members.values())
            objs.extend(service.classes.values())
            objs.extend(service.enumerations.values())
            objs.extend(service.structs.values())
            objs.extend(service.exceptions.values())
            for struct in service.structs.values():
                objs.extend(struct.fields.values())
            for cls in service.classes.values():
                objs.extend(cls.members.values())
            for enumeration in service.enumerations.values():
                objs.extend(enumeration.values.values())
                for value in enumeration.values.values():
                    objs.append(value)
        _services_lookup = (services, {x.cref: x for x in objs})
    return _services_lookup[1][cref]
