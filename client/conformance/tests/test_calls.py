"""Call requirements: calling the members of a service."""

from driver import section, values

section("Members")


def test_procedure(connected):
    """The client calls a service procedure."""
    assert connected.invoke("TestService", "Int32ToString", values.sint32(42)) == "42"
