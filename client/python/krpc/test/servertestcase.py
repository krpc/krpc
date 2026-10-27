import os
from collections.abc import Callable
from typing import Any

import krpc
import krpc.client
import krpc.functioncompiler
import krpc.schema.KRPC_pb2 as KRPC
from krpc.client import Client
from krpc.functioncompiler import NO_VALUE, Parameters

_compile_function_with_type = krpc.functioncompiler.compile_function_with_type


def _checked_compile_function_with_type(
    client: Client,
    func: Callable,
    streamed: bool = False,
) -> tuple[Any, KRPC.Type | None, Parameters]:
    """Compile a function, and check that the types the compiler tracks for its
    value and its parameters are the types the server gives them. The client
    decodes the value and encodes the arguments with the compiler's types, so
    the two must agree on where a value can be None."""
    function, ptype, parameters = _compile_function_with_type(client, func, streamed)
    if ptype is NO_VALUE:
        if client._expression_return_type(function, cache=False) is not None:
            raise AssertionError("The compiler gives a function with a value no type")
    elif ptype is not None:
        server_ptype = client._expression_return_type(function, cache=False)
        if server_ptype != ptype:
            raise AssertionError(
                "The compiler gives the function the type\n%s\nand the server\n%s"
                % (ptype, server_ptype)
            )
    if parameters.ptypes:
        server_ptypes = client._parameter_types(function)
        if server_ptypes != parameters.ptypes:
            raise AssertionError(
                "The compiler gives the parameters the types\n%s\nand the server\n%s"
                % (parameters.ptypes, server_ptypes)
            )
    return function, ptype, parameters


class ServerTestCase:
    conn: Client = None  # pyright: ignore[reportAssignmentType]

    @classmethod
    def setUpClass(cls) -> None:
        if cls.conn is None:
            cls.conn = cls.connect()
        # Every function a test compiles and runs is checked
        krpc.client.compile_function_with_type = _checked_compile_function_with_type

    @staticmethod
    def connect() -> Client:
        return krpc.connect(
            name="python_client_test",
            address="localhost",
            port=ServerTestCase.tcpip_port(),
        )

    @staticmethod
    def tcpip_port() -> int:
        return int(os.getenv("KRPC_TEST_TCPIP_PORT", "50000"))
