def lookup_cref(cref, services):
    if lookup_cref.services_lookup is None:
        objs = []
        for service in services.values():
            objs.append(service)
            objs.extend(service.members.values())
            objs.extend(service.classes.values())
            objs.extend(service.enumerations.values())
            for cls in service.classes.values():
                objs.extend(cls.members.values())
            for enumeration in service.enumerations.values():
                objs.extend(enumeration.values.values())
                for value in enumeration.values.values():
                    objs.append(value)
        lookup_cref.services_lookup = dict([(x.cref, x) for x in objs])
    return lookup_cref.services_lookup[cref]

lookup_cref.services_lookup = None
