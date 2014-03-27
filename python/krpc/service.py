from krpc.attributes import _Attributes


class BaseService(object):
    """ Abstract base class for all services """

    def __init__(self, client, name):
        self._client = client
        self._name = name

    def _invoke(self, procedure, args=[], kwargs={}, param_names=[], param_types=[], return_type=None):
        return self._client._invoke(self._name, procedure, args, kwargs, param_names, param_types, return_type)


def _create_service(client, service):
    """ Create a new class type for a service and instantiate it """
    cls = type(str('_Service_' + service.name), (_Service,), {})
    return cls(cls, client, service)


class _Service(BaseService):
    """ A dynamically created service, created using information received from the server.
        Should not be instantiated directly. Use _create_service instead. """

    def __init__(self, cls, client, service):
        """ Create a service from the dynamically created class type for the service, the client,
            and a KRPC.Service object received from a call to KRPC.GetServices()
            Should not be instantiated directly. Use _create_service instead. """
        super(_Service, self).__init__(client, service.name)
        self._cls = cls
        self._name = service.name
        self._types = client._types

        # Add class types to service
        for procedure in service.procedures:
            try:
                name = _Attributes.get_class_name(procedure.attributes)
                self._add_class(name)
            except ValueError:
                pass

        # Create plain procedures
        for procedure in service.procedures:
            if _Attributes.is_a_procedure(procedure.attributes):
                self._add_procedure(procedure)

        # Create static service properties
        properties = {}
        for procedure in service.procedures:
            if _Attributes.is_a_property_accessor(procedure.attributes):
                name = _Attributes.get_property_name(procedure.attributes)
                if name not in properties:
                    properties[name] = [None,None]
                if _Attributes.is_a_property_getter(procedure.attributes):
                    properties[name][0] = procedure
                else:
                    properties[name][1] = procedure
        for name, procedures in properties.items():
            self._add_property(name, procedures[0], procedures[1])

        # Create class methods
        for procedure in service.procedures:
            if _Attributes.is_a_class_method(procedure.attributes):
                class_name = _Attributes.get_class_name(procedure.attributes)
                method_name = _Attributes.get_class_method_name(procedure.attributes)
                self._add_class_method(class_name, method_name, procedure)

        # Create class properties
        properties = {}
        for procedure in service.procedures:
            if _Attributes.is_a_class_property_accessor(procedure.attributes):
                class_name = _Attributes.get_class_name(procedure.attributes)
                property_name = _Attributes.get_class_property_name(procedure.attributes)
                key = (class_name, property_name)
                if key not in properties:
                    properties[key] = [None,None]
                if _Attributes.is_a_class_property_getter(procedure.attributes):
                    properties[key][0] = procedure
                else:
                    properties[key][1] = procedure
        for (class_name, property_name), procedures in properties.items():
            self._add_class_property(class_name, property_name, procedures[0], procedures[1])

    def _add_class(self, name):
        """ Add a class type with the given name to this service, and the type store """
        class_type = self._types.as_type('Class(' + self._name + '.' + name + ')')
        setattr(self, name, class_type.python_type)

    def _add_procedure(self, procedure):
        """ Add a plain procedure to this service """
        param_names = [param.name for param in procedure.parameters]
        param_types = [self._types.get_parameter_type(i, param.type, procedure.attributes) for i,param in enumerate(procedure.parameters)]
        return_type = None
        if procedure.HasField('return_type'):
            return_type = self._types.get_return_type(procedure.return_type, procedure.attributes)
        setattr(self, procedure.name,
                lambda *args, **kwargs: self._invoke(
                    procedure.name, args=args, kwargs=kwargs,
                    param_names=param_names, param_types=param_types, return_type=return_type))

    def _add_property(self, name, getter=None, setter=None):
        """ Add a property to the service, with a getter and/or setter procedure """
        fget = fset = None
        if getter:
            self._add_procedure(getter)
            fget = lambda s: getattr(self, getter.name)()
        if setter:
            self._add_procedure(setter)
            fset = lambda s, value: getattr(self, setter.name)(value)
        setattr(self._cls, name, property(fget, fset))

    def _add_class_method(self, class_name, method_name, procedure):
        """ Add a class method to the service """
        cls = getattr(self, class_name)
        param_names = [param.name for param in procedure.parameters]
        param_types = [self._types.get_parameter_type(i, param.type, procedure.attributes) for i,param in enumerate(procedure.parameters)]
        return_type = None
        if procedure.HasField('return_type'):
            return_type = self._types.get_return_type(procedure.return_type, procedure.attributes)
        setattr(cls, method_name,
                lambda s, *args, **kwargs: self._invoke(procedure.name, args=[s] + list(args), kwargs=kwargs,
                                                        param_names=param_names, param_types=param_types,
                                                        return_type=return_type))

    def _add_class_property(self, class_name, property_name, getter=None, setter=None):
        fget = fset = None
        if getter:
            self._add_procedure(getter)
            fget = lambda s: getattr(self, getter.name)(s)
        if setter:
            self._add_procedure(setter)
            fset = lambda s, value: getattr(self, setter.name)(s, value)
        class_type = getattr(self, class_name)
        setattr(class_type, property_name, property(fget, fset))
