"""Connection requirements: connecting, refusals and closing."""

from driver import protocol, section, values

section("Connecting")


def test_connect(client, server):
    """The client connects over TCP/IP to an address and a port. The defaults are
    ``127.0.0.1`` and ``50000``."""
    client.connect(server)
    assert client.invoke("TestService", "Int32ToString", values.sint32(42)) == "42"


@protocol("localsocket")
def test_connect_localsocket(client, server):
    """The client connects to a local socket path."""
    client.connect(server)
    assert client.invoke("TestService", "Int32ToString", values.sint32(42)) == "42"
