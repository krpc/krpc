"""Benchmarks over a 321-part station, in orbit.

Part lookup by flight id is a linear scan over every part of every loaded vessel, so its cost
depends on the size of the scene rather than on the accessor being measured. The reference
craft cannot show that at 60-odd parts; this scenario runs the same cases against a scene five
times the size, so what is linear in loaded parts separates from what is flat.

``Station300.craft`` is a command pod carrying 320 cubic octagonal struts. It is a fixture, not
a spacecraft: the struts are there to be looked up.
"""

from tools.benchmarks.server.harness import BenchmarkTestCase


class Station(BenchmarkTestCase):
    """Puts the station in orbit."""

    # Set from the scene in setUpClass, so a report can never claim a part count the craft
    # does not have.
    scenario = None

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        active_vessel = cls.connect().space_center.active_vessel
        if active_vessel is None or active_vessel.name != "Station300":
            cls.launch_vessel_from_vab("Station300")
            cls.remove_other_vessels()
            cls.set_circular_orbit("Kerbin", 100000)
        cls.vessel = cls.connect().space_center.active_vessel
        cls.parts = cls.vessel.parts
        cls.part = cls.parts.root
        cls.scenario = "Station300 (%d parts)" % len(cls.parts.all)


class TestEndToEnd(Station):
    def test_part_getters(self):
        conn = self.connect()
        self.measure_call("part.name", conn.get_call(getattr, self.part, "name"))
        self.measure_call("part.mass", conn.get_call(getattr, self.part, "mass"))

    def test_bulk(self):
        self.measure_call(
            "vessel.parts.all", self.connect().get_call(getattr, self.parts, "all")
        )


class TestObjectAccess(Station):
    """The same primitives as the reference craft, in a scene with five times the parts. Only
    the scan should have moved."""

    def test_part_resolution(self):
        self.measure_part(self.part, "resolve.captured")
        self.measure_part(self.part, "resolve.cached")
        self.measure_part(self.part, "resolve.find_part_by_id")

    def test_store_dedup(self):
        # The store holds one entry per part here, so this is the dedup path against a store
        # five times the size of the reference craft's.
        self.measure_vessel(self.vessel, "store.dedup")


class TestStreams(Station):
    """A stream per part over a 321-part vessel: the workload the access path exists for."""

    def test_stream_update(self):
        self.measure_stream_update(self.parts.all)
