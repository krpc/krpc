"""Checks of the errors a client reports, against its profiles and conventions."""

from __future__ import annotations

import functools
from typing import TYPE_CHECKING, Any

import testee_pb2 as pb

if TYPE_CHECKING:
    from driver.testee import Client


def error_types(client: Client) -> bool:
    """Whether the client implements the error-types profile."""
    return client.has_profile("error-types")


def assert_error_kind(client: Client, error: pb.Error, *kinds: int) -> None:
    """The error is of one of the kinds. A client without error types reports only RPC and
    connection errors, so for it the check is only that the call raised."""
    if error_types(client):
        assert error.kind in kinds, error


def assert_argument_error(client: Client, error: pb.Error) -> None:
    """The error is the client's argument error, or the server's ``KRPC.ArgumentException``
    for a client that maps it to its argument error."""
    server = (error.service, error.name) == ("KRPC", "ArgumentException")
    if not server:
        assert_error_kind(client, error, pb.Error.ARGUMENT)


def is_removed_error(client: Client, error: pb.Error) -> bool:
    """Whether an error is the one for a removed stream: the KRPC exception for it, or the
    client's stream error. A client without error types reports it as an RPC error."""
    if error.name:
        return (error.service, error.name) == ("KRPC", "StreamRemovedException")
    if error_types(client):
        return error.kind == pb.Error.STREAM
    return error.kind == pb.Error.RPC


@functools.cache
def mapped_type(control: Any, name: str) -> str:
    """The mapped type of a KRPC exception, as the server reports it."""
    services = control.krpc.get_services()
    (service,) = [x for x in services.services if x.name == "KRPC"]
    (exception,) = [x for x in service.exceptions if x.name == name]
    return exception.mapped_type


def native_type(client: Client, control: Any, name: str) -> str | None:
    """The native error type the client's conventions give for a mapped KRPC exception, or
    none for an exception the client raises only as a type of its own."""
    mapped = mapped_type(control, name)
    natives = client.conventions.native_types
    assert natives is not None
    assert mapped in natives, "The conventions file has no entry for %s" % mapped
    return natives[mapped]


def assert_server_error_kind(error: pb.Error, name: str) -> None:
    """The error for the KRPC exception of the given name is an RPC error. The native type of
    an argument exception can also be the client's argument error type."""
    kinds = [pb.Error.RPC]
    if name.startswith("Argument"):
        kinds.append(pb.Error.ARGUMENT)
    assert error.kind in kinds, error


def assert_native_error(
    client: Client, control: Any, error: pb.Error, name: str
) -> None:
    """The error is the native error type for the KRPC exception of the given name. A client
    without error types starts the message with ``KRPC.Name:``."""
    assert_server_error_kind(error, name)
    if error_types(client):
        native = native_type(client, control, name)
        if native is None:
            assert (error.service, error.name) == ("KRPC", name), error
        else:
            assert native in error.native_types, error
    else:
        assert error.message.startswith("KRPC.%s: " % name)
