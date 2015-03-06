import re
from krpc.attributes import _Attributes

_regex_multi_uppercase = re.compile(r'([A-Z]+)([A-Z][a-z0-9])')
_regex_single_uppercase = re.compile(r'([a-z0-9])([A-Z])')
_regex_underscores = re.compile(r'(.)_')

def _to_snake_case(camel_case):
    """ Convert camel case to snake case, e.g. GetServices -> get_services """
    result = re.sub(_regex_underscores, r'\1__', camel_case)
    result = re.sub(_regex_single_uppercase, r'\1_\2', result)
    return re.sub(_regex_multi_uppercase, r'\1_\2', result).lower()


class BaseService(object):
    """ Abstract base class for all services """

    def __init__(self, client, name):
        self._client = client
        self._name = name

    def _invoke(self, procedure, args=[], kwargs={}, param_names=[], param_types=[], return_type=None):
        return self._client._invoke(self._name, procedure, args, kwargs,
                                    param_names, param_types, return_type)

    def _build_request(self, procedure, args=[], kwargs={}, param_names=[], param_types=[], return_type=None):
        return self._client._build_request(self._name, procedure, args, kwargs,
                                           param_names, param_types, return_type)


def _create_service(client, service):
    """ Create a new class type for a service and instantiate it """
    cls = type(str('_Service' + service.name), (_Service,), {})
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
        for cls in service.classes:
            self._add_class(cls)

        # Add enumeration types to service
        for enum in service.enumerations:
            self._add_enumeration(enum)

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

    def _add_class(self, cls):
        """ Add a class type to this service, and the type store """
        name = cls.name
        class_type = self._types.as_type('Class(' + self._name + '.' + name + ')')
        setattr(self, name, class_type.python_type)

    def _add_enumeration(self, enum):
        """ Add an enumeration to this service """
        name = enum.name
        setattr(self, name, type(str(name), (object,),
            dict((_to_snake_case(x.name), x.value) for x in enum.values)))

    def _add_procedure(self, procedure):
        """ Add a plain procedure to this service """
        param_names = [_to_snake_case(param.name) for param in procedure.parameters]
        param_types = [self._types.get_parameter_type(i, param.type, procedure.attributes) for i,param in enumerate(procedure.parameters)]
        return_type = None
        if procedure.HasField('return_type'):
            return_type = self._types.get_return_type(procedure.return_type, procedure.attributes)
        func = lambda *args, **kwargs: self._invoke(
            procedure.name, args=args, kwargs=kwargs,
            param_names=param_names, param_types=param_types, return_type=return_type)
        setattr(func, '_build_request',
                lambda *args, **kwargs: self._build_request(
                    procedure.name, args=args, kwargs=kwargs,
                    param_names=param_names, param_types=param_types, return_type=return_type))
        setattr(func, '_return_type', return_type)
        setattr(self, _to_snake_case(procedure.name), func)

    def _add_property(self, name, getter=None, setter=None):
        """ Add a property to the service, with a getter and/or setter procedure """
        fget = fset = None
        if getter:
            self._add_procedure(getter)
            fget = lambda s: getattr(self, _to_snake_case(getter.name))()
            fget_request = lambda s: getattr(self, _to_snake_case(getter.name))._build_request()
            fget_return_type = getattr(self, _to_snake_case(getter.name))._return_type
            setattr(fget, '_build_request', fget_request)
            setattr(fget, '_return_type', fget_return_type)
        if setter:
            self._add_procedure(setter)
            fset = lambda s, value: getattr(self, _to_snake_case(setter.name))(value)
        setattr(self._cls, _to_snake_case(name), property(fget, fset))

    def _add_class_method(self, class_name, method_name, procedure):
        """ Add a class method to the service """
        cls = getattr(self, class_name)
        param_names = [_to_snake_case(param.name) for param in procedure.parameters]
        param_types = [self._types.get_parameter_type(i, param.type, procedure.attributes) for i,param in enumerate(procedure.parameters)]
        return_type = None
        if procedure.HasField('return_type'):
            return_type = self._types.get_return_type(procedure.return_type, procedure.attributes)
        func = lambda s, *args, **kwargs: self._invoke(procedure.name, args=[s] + list(args), kwargs=kwargs,
                                                       param_names=param_names, param_types=param_types,
                                                       return_type=return_type)
        setattr(func, '_build_request',
                lambda s, *args, **kwargs: self._build_request(procedure.name, args=[s] + list(args), kwargs=kwargs,
                                                               param_names=param_names, param_types=param_types,
                                                               return_type=return_type))
        setattr(func, '_return_type', return_type)
        setattr(cls, _to_snake_case(method_name), func)

    def _add_class_property(self, class_name, property_name, getter=None, setter=None):
        """ Add a class property to the service """
        fget = fset = None
        if getter:
            self._add_class_method(class_name, getter.name, getter)
            fget = lambda s: getattr(s, _to_snake_case(getter.name))()
            setattr(fget, '_build_request',
                    lambda s: getattr(s, _to_snake_case(getter.name))._build_request(s))
            setattr(fget, '_return_type',
                    getattr(getattr(self, class_name), _to_snake_case(getter.name))._return_type)
        if setter:
            self._add_class_method(class_name, setter.name, setter)
            fset = lambda s, value: getattr(s, _to_snake_case(setter.name))(value)
        class_type = getattr(self, class_name)
        setattr(class_type, _to_snake_case(property_name), property(fget, fset))
