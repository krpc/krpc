"""Benchmarks over the reference craft: the ``Parts.craft`` the SpaceCenter tests use, a small
craft carrying one of most things, in orbit so nothing on the ground perturbs the frame.

This is the scenario for per-access cost. One small craft is the cheapest scene to reason
about, and every case is here; what it cannot show is how a lookup scales with the number of
loaded parts, which is what test_station.py is for.

Run it with::

    bazel run //tools/benchmarks:server
"""

import os

from tools.benchmarks.server.harness import BenchmarkTestCase

# The reference craft belongs to the SpaceCenter tests. The benchmarks use the same one so
# that a part count and a part mix everything else is already written against does not have
# to be maintained twice.
CRAFT_DIRECTORY = os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    os.pardir,
    os.pardir,
    os.pardir,
    "service",
    "SpaceCenter",
    "test",
    "craft",
)


class ReferenceCraft(BenchmarkTestCase):
    """Puts the reference craft in orbit and picks out the parts the cases need."""

    # Set from the scene in setUpClass, so a report can never claim a part count the craft
    # does not have.
    scenario = None

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        active_vessel = cls.connect().space_center.active_vessel
        if active_vessel is None or active_vessel.name != "Parts":
            cls.launch_vessel_from_vab("Parts", directory=CRAFT_DIRECTORY)
            cls.remove_other_vessels()
            cls.set_circular_orbit("Kerbin", 100000)
        cls.vessel = cls.connect().space_center.active_vessel
        cls.parts = cls.vessel.parts
        cls.part = cls.parts.root
        cls.module = cls.part.modules[-1]
        cls.engine_part = next(x for x in cls.parts.all if x.engine is not None)
        cls.parachute_part = next(x for x in cls.parts.all if x.parachute is not None)
        cls.scenario = "Parts (%d parts)" % len(cls.parts.all)


class TestEndToEnd(ReferenceCraft):
    """What the server pays for a remote procedure: argument decode, dispatch, the procedure
    itself and encoding the result. Everything a request costs but the socket.

    Each case is an ordinary procedure call, named here and run in a loop server side, so
    measuring a different one is a line in this file rather than a change to the mod."""

    def measure_getter(self, name, obj, attribute):
        """Measure the getter for an attribute, under the name the report should show."""
        return self.measure_call(name, self.connect().get_call(getattr, obj, attribute))

    def test_part_getters(self):
        # A trivial getter, where finding the part again dominates, against one whose answer
        # KSP does the work for.
        self.measure_getter("part.name", self.part, "name")
        self.measure_getter("part.mass", self.part, "mass")

    def test_module_getters(self):
        self.measure_getter("module.name", self.module, "name")
        self.measure_getter("module.part", self.module, "part")

    def test_concrete_module_proxies(self):
        # Reading through a concrete proxy that already exists, which is what a stream over
        # engine.thrust or parachute.state does.
        self.measure_getter("engine.thrust", self.engine_part.engine, "thrust")
        self.measure_getter("parachute.state", self.parachute_part.parachute, "state")
        # Constructing one. This is where the module list gets walked.
        self.measure_getter("part.engine", self.engine_part, "engine")

    def test_bulk(self):
        # Work proportional to the size of the vessel rather than to one access: one Part
        # proxy per part, constructed, deduplicated against the object store and encoded.
        self.measure_getter("vessel.parts.all", self.parts, "all")


class TestObjectAccess(ReferenceCraft):
    """The primitives a proxy can use to get from a stable identifier back to a game object.

    No remote procedure exposes one on its own, so these are the cases that have to live in
    the mod. They are measured against each other within one session, which is the only way a
    comparison between them holds: absolute timings drift between sessions by more than the
    differences here."""

    def test_part_resolution(self):
        # From the cheapest - what a proxy that simply captured its part would pay - through
        # the two shapes of weak-reference cache, to the linear scan over every loaded part
        # that a cache miss falls back to.
        self.measure_part(self.part, "resolve.captured")
        self.measure_part(self.part, "resolve.cached")
        self.measure_part(self.part, "resolve.cached_bare")
        self.measure_part(self.part, "resolve.find_part_by_id")

    def test_module_resolution(self):
        # Going straight to a position in the module list, against the two walks of it. All
        # three are measured on the part's last module, the worst case for a walk.
        self.measure_part(self.part, "module.indexed")
        self.measure_part(self.part, "module.by_persistent_id")
        self.measure_part(self.part, "module.ref")
        self.measure_part(self.part, "module.by_name_scan")

    def test_of_type_to_list(self):
        # The pattern a concrete module proxy can use to collect the modules it wraps. It
        # allocates an enumerator and a list every time, which is why it is measured rather
        # than assumed cheap.
        self.measure_part(self.part, "module.of_type_to_list")

    def test_store_dedup(self):
        # What returning an already-known proxy costs: the object store hashes it and
        # compares it against what it holds. Every proxy a call returns pays this.
        self.measure_vessel(self.vessel, "store.dedup")


class TestStreams(ReferenceCraft):
    """The realistic workload: one stream per part, re-evaluated every fixed update."""

    def test_stream_update(self):
        self.measure_stream_update(self.parts.all)
