"""The profiles and protocols of the client specification, as tests, testees and profiles files
name them."""

PROFILES = frozenset(
    {
        "core",
        "error-types",
        "dynamic",
        "definitions",
        "declarations",
        "defaults",
        "named-arguments",
        "skipped-arguments",
        "streams",
        "callbacks",
        "conditions",
        "events",
        "functions",
        "function-types",
        "connections",
        "bound-classes",
        "concurrency",
        "ordering",
        "races",
    }
)

PROTOCOLS = frozenset({"tcpip", "localsocket", "serialio", "websockets"})


def unknown_names(names: list[str] | set[str], known: frozenset[str]) -> list[str]:
    """The names, without a leading "!", that are not in known."""
    return sorted(x for x in names if x.removeprefix("!") not in known)
