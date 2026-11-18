"""The cases for timing each step of a call on its own, shared by TestServer and the game.

Each case calls a procedure of the Benchmark service, so the same calls run in both. A case
passes a value of one type and returns it, so its decode and encode steps time that type.
"""

# The number of elements in each collection a case passes
SIZE = 100


def cases(conn):
    """The calls whose steps to time, one per type kind, with what each passes."""
    benchmark = conn.benchmark
    obj = benchmark.new_object()
    objects = [benchmark.new_object() for _ in range(SIZE)]
    return [
        ("three scalars", conn.get_call(benchmark.add, 3.14159, 1, 2)),
        ("string of 10", conn.get_call(benchmark.echo_string, "x" * 10)),
        ("string of 1000", conn.get_call(benchmark.echo_string, "x" * 1000)),
        (
            "list of %d ints" % SIZE,
            conn.get_call(benchmark.echo_list, list(range(SIZE))),
        ),
        (
            "dictionary of %d" % SIZE,
            conn.get_call(benchmark.echo_dictionary, {str(i): i for i in range(SIZE)}),
        ),
        ("object", conn.get_call(benchmark.echo_object, obj)),
        ("list of %d objects" % SIZE, conn.get_call(benchmark.echo_objects, objects)),
    ]
