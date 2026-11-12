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
    """The error is of one of the kinds. A client without error types reports only server and
    connection errors, so for it the check is only that the call raised."""
    if error_types(client):
        assert error.kind in kinds, error


def assert_argument_error(client: Client, error: pb.Error) -> None:
    """The error is the client's argument error, or the server's ``KRPC.ArgumentException``
    for a client that maps it to its argument error."""
    server = (error.service, error.name) == ("KRPC", "ArgumentException")
    if not server:
        assert_error_kind(client, error, pb.Error.ARGUMENT)
        if error_types(client) and not error_codes(client):
            assert remote_error(client) not in error.native_types, error


def is_removed_error(client: Client, error: pb.Error) -> bool:
    """Whether an error is the client's stream error, the one for a removed stream. A client
    without error types can report it as a server error."""
    if error_types(client):
        return error.kind == pb.Error.STREAM
    return error.kind in (pb.Error.STREAM, pb.Error.SERVER)


def error_codes(client: Client) -> bool:
    """Whether the client's errors are codes, with no remote error type. Such a client starts a
    server error's message with ``Service.Name:``, as a client without error types does."""
    return error_types(client) and client.conventions.remote_error is None


def message_prefix(client: Client) -> bool:
    """Whether the client starts a server error's message with ``Service.Name:``."""
    return not error_types(client) or error_codes(client)


def remote_error(client: Client) -> str:
    """The base type of the client's own errors, from its conventions."""
    remote = client.conventions.remote_error
    assert remote is not None
    return remote


def server_error(client: Client) -> str:
    """The type the client's exception types derive, from its conventions."""
    server = client.conventions.server_error
    assert server is not None
    return server


def assert_server_error_type(client: Client, error: pb.Error) -> None:
    """For a client with error types, the error derives the client's server error type."""
    if error_types(client) and not error_codes(client):
        assert server_error(client) in error.native_types, error


def assert_remote_error(client: Client, error: pb.Error) -> None:
    """For a client with error types, the error derives the client's remote error type."""
    if error_types(client):
        assert remote_error(client) in error.native_types, error


def assert_interrupt_error(client: Client, error: pb.Error) -> None:
    """The error is the language's error for an interrupted thread."""
    assert_error_kind(client, error, pb.Error.INTERRUPT)


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
    """The error for the KRPC exception of the given name is a server error. The native type
    of an argument exception can also be the client's argument error type."""
    kinds = [pb.Error.SERVER]
    if name.startswith("Argument"):
        kinds.append(pb.Error.ARGUMENT)
    assert error.kind in kinds, error


def assert_native_error(
    client: Client, control: Any, error: pb.Error, name: str
) -> None:
    """The error is the native error type for the KRPC exception of the given name, and does
    not derive the client's remote error type. An exception with no native type is the
    client's type for it, deriving the remote error type. A client without error types starts
    the message with ``KRPC.Name:``."""
    assert_server_error_kind(error, name)
    if error_types(client):
        native = native_type(client, control, name)
        if native is None:
            assert (error.service, error.name) == ("KRPC", name), error
            assert_server_error_type(client, error)
        else:
            assert native in error.native_types, error
            assert remote_error(client) not in error.native_types, error
    else:
        assert error.message.startswith("KRPC.%s: " % name)
