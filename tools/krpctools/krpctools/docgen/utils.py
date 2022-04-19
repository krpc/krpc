def lookup_cref(cref, services):
    if lookup_cref.services_lookup is None:
        objs = []
        for service in list(services.values()):
            objs.append(service)
            objs.extend(list(service.members.values()))
            objs.extend(list(service.classes.values()))
            objs.extend(list(service.enumerations.values()))
            objs.extend(list(service.exceptions.values()))
            for cls in list(service.classes.values()):
                objs.extend(list(cls.members.values()))
            for enumeration in list(service.enumerations.values()):
                objs.extend(list(enumeration.values.values()))
                for value in list(enumeration.values.values()):
                    objs.append(value)
        lookup_cref.services_lookup = dict([(x.cref, x) for x in objs])
    return lookup_cref.services_lookup[cref]


lookup_cref.services_lookup = None
