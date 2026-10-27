"""Building server side functions through the client's KRPC.Expression and KRPC.Type stubs."""

from __future__ import annotations

import testee_pb2 as pb

from driver import values
from driver.testee import Client


class Functions:
    """Calls the static methods of KRPC.Expression and KRPC.Type, each of which returns an
    object the testee holds. An argument is a Value, or an object handle."""

    def __init__(self, client: Client) -> None:
        self.client = client

    def expression(self, name: str, *args: pb.Value | values.Object) -> values.Object:
        return self._build("Expression", name, args)

    def type(self, name: str, *args: pb.Value | values.Object) -> values.Object:
        return self._build("Type", name, args)

    def lambda_(
        self, parameters: list[values.Object], body: values.Object
    ) -> values.Object:
        return self.expression(
            "Lambda", values.list_of(values.obj(x) for x in parameters), body
        )

    def parameter(self, name: str, typ: values.Object) -> values.Object:
        return self.expression("Parameter", values.string(name), typ)

    def value(self, name: str) -> values.Object:
        """A function that reads the named ConformanceService value."""
        return self.client.call_function(
            "ConformanceService", "Value", values.string(name)
        )

    def _build(
        self, cls: str, name: str, args: tuple[pb.Value | values.Object, ...]
    ) -> values.Object:
        arguments = [values.obj(x) if isinstance(x, values.Object) else x for x in args]
        result = self.client.invoke("KRPC", "%s_static_%s" % (cls, name), *arguments)
        assert isinstance(result, values.Object), result
        return result
