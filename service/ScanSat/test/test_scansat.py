import math
import unittest

from krpctools.game.images import assert_jpeg, assert_png, png_size
from krpctools.game.ksp import TestCase

# The scanners on the test craft: part name, scan types, and minimum, best and maximum
# altitudes from the part configs
SCANNERS = {
    "scansat-radar-poseidon-3b-1": ("altimetry_lo_res", 5000, 70000, 250000),
    "scansat-sar-radarsat-2-1": ("altimetry_hi_res", 500000, 700000, 750000),
    "scansat-multi-modis-1": ("visual_lo_res|biome", 20000, 70000, 250000),
    "scansat-exomars-1": ("anomaly_detail", 0, 0, 2000),
    "SurveyScanner": ("resource_lo_res", 15000, 500000, 7500000),
}

ALTITUDE = 100000
HIGH_ALTITUDE = 700000


class TestScanSat(TestCase):
    mods = ["SCANsat"]

    @classmethod
    def setUpClass(cls):
        cls.vessel = cls.launch("ScanSat", always_load=True)
        cls.set_circular_orbit("Kerbin", ALTITUDE)
        conn = cls.connect()
        cls.scansat = conn.scan_sat
        cls.sc = conn.space_center
        cls.kerbin = cls.sc.bodies["Kerbin"]
        cls.mun = cls.sc.bodies["Mun"]

    def scanner(self, name):
        return self.scansat.scanner(self.vessel.parts.with_name(name)[0])

    def set_active(self, scanner, value):
        scanner.active = value
        self.wait_until(lambda: scanner.active == value)

    def test_settings(self):
        self.assertTrue(self.scansat.background_scanning)
        self.assertTrue(self.scansat.daylight_check)
        self.assertTrue(self.scansat.instant_resource_scan)
        self.assertTrue(self.scansat.stock_resource_scan)
        self.assertIsInstance(self.scansat.biome_lock, bool)
        self.assertIsInstance(self.scansat.require_narrow_band, bool)

    def test_resources(self):
        self.assertIn("Ore", self.scansat.resources)

    def covered_longitude(self, body, scan_type):
        """A longitude on the equator that the scan type covers."""
        for longitude in range(-180, 180):
            if self.scansat.is_covered(body, 0, longitude, scan_type):
                return longitude
        self.fail("No position on the equator is covered")

    def scan_types(self, names):
        """The combination of the scan types named, separated by |."""
        result = self.scansat.ScanType.none
        for name in names.split("|"):
            result |= getattr(self.scansat.ScanType, name)
        return result

    def test_unscanned_body(self):
        ScanType = self.scansat.ScanType
        for scan_type in [*ScanType, ScanType.altimetry, ScanType.all]:
            with self.subTest(scan_type=scan_type):
                self.assertEqual(0, self.scansat.coverage(self.mun, scan_type))
                self.assertFalse(self.scansat.is_covered(self.mun, 0, 0, scan_type))
        self.assertIsNone(self.scansat.elevation(self.mun, 0, 0))
        self.assertIsNone(self.scansat.slope(self.mun, 0, 0))
        self.assertIsNone(self.scansat.biome(self.mun, 0, 0))
        self.assertIsNone(self.scansat.resource_abundance(self.mun, 0, 0, "Ore"))
        self.assertEqual([], self.scansat.anomalies(self.mun))

    def test_invalid_arguments(self):
        with self.assertRaises(ValueError):
            self.scansat.is_covered(self.mun, 91, 0, self.scansat.ScanType.biome)
        with self.assertRaises(ValueError):
            self.scansat.resource_abundance(self.mun, 0, 0, "NotAResource")
        with self.assertRaises(ValueError):
            self.scansat.coverage(self.mun, self.scansat.ScanType.none)
        with self.assertRaises(ValueError):
            self.scansat.is_covered(self.mun, 0, 0, self.scansat.ScanType.none)
        with self.assertRaises(RuntimeError):
            self.scansat.biome(self.sc.bodies["Sun"], 0, 0)

    def test_scanners(self):
        scanners = self.scansat.scanners(self.vessel)
        self.assertCountEqual(
            SCANNERS.keys(), [scanner.part.name for scanner in scanners]
        )
        for scanner in scanners:
            types, minimum, best, maximum = SCANNERS[scanner.part.name]
            with self.subTest(part=scanner.part.name):
                self.assertEqual(scanner, self.scanner(scanner.part.name))
                self.assertEqual(self.scan_types(types), scanner.scan_types)
                self.assertEqual(minimum, scanner.min_altitude)
                self.assertEqual(best, scanner.best_altitude)
                self.assertEqual(maximum, scanner.max_altitude)
                self.assertGreater(scanner.field_of_view, 0)
                self.assertIsInstance(scanner.requires_light, bool)

    def test_not_a_scanner(self):
        with self.assertRaises(ValueError):
            self.scansat.scanner(self.vessel.parts.root)

    def test_state(self):
        ScannerState = self.scansat.ScannerState
        scanner = self.scanner("scansat-radar-poseidon-3b-1")
        self.assertEqual(ScannerState.off, scanner.state)
        self.set_active(scanner, True)
        try:
            # 100 km is between the best and maximum altitudes of the radar
            self.wait_until(lambda: scanner.state == ScannerState.in_best_range)
        finally:
            self.set_active(scanner, False)
        self.assertEqual(ScannerState.off, scanner.state)

    def test_state_out_of_range(self):
        scanner = self.scanner("scansat-sar-radarsat-2-1")
        self.set_active(scanner, True)
        try:
            self.wait()
            self.assertEqual(self.scansat.ScannerState.out_of_range, scanner.state)
        finally:
            self.set_active(scanner, False)

    def test_resource_scanner_with_instant_scan(self):
        # With instant resource scanning, the stock orbital survey fills the resource scans
        scanner = self.scanner("SurveyScanner")
        scanner.active = True
        self.wait()
        self.assertFalse(scanner.active)

    def test_scan(self):
        radar = self.scanner("scansat-radar-poseidon-3b-1")
        multispectral = self.scanner("scansat-multi-modis-1")
        self.set_active(radar, True)
        self.set_active(multispectral, True)
        try:
            self.sc.warp_to(self.sc.ut + 3600)
        finally:
            self.set_active(radar, False)
            self.set_active(multispectral, False)
        ScanType = self.scansat.ScanType
        altimetry = ScanType.altimetry_lo_res
        coverage = self.scansat.coverage(self.kerbin, altimetry)
        self.assertGreater(coverage, 0)
        self.assertLess(coverage, 1)
        self.assertEqual(
            0, self.scansat.coverage(self.kerbin, ScanType.altimetry_hi_res)
        )
        # The coverage of a combination is the mean of its scan types
        self.assertAlmostEqual(
            coverage / 2,
            self.scansat.coverage(self.kerbin, ScanType.altimetry),
            places=3,
        )

        # The equatorial orbit covers the equator, and not the poles
        flight = self.vessel.flight(self.kerbin.reference_frame)
        latitude, longitude = flight.latitude, flight.longitude
        self.assertTrue(
            self.scansat.is_covered(self.kerbin, latitude, longitude, altimetry)
        )
        self.assertFalse(self.scansat.is_covered(self.kerbin, 80, longitude, altimetry))
        self.assertTrue(
            self.scansat.is_covered(
                self.kerbin, latitude, longitude, altimetry | ScanType.altimetry_hi_res
            )
        )
        self.assertFalse(
            self.scansat.is_covered(
                self.kerbin, latitude, longitude, ScanType.altimetry_hi_res
            )
        )

        # Low resolution altimetry rounds the height toward zero, to 500 m
        self.assertIsNone(self.scansat.elevation(self.kerbin, 80, longitude))
        self.assertIsNone(self.scansat.slope(self.kerbin, latitude, longitude))
        for lon in range(-180, 180, 5):
            if not self.scansat.is_covered(self.kerbin, 0, lon, altimetry):
                continue
            with self.subTest(longitude=lon):
                height = self.kerbin.bedrock_height(0, lon)
                self.assertEqual(
                    math.trunc(height / 500) * 500,
                    self.scansat.elevation(self.kerbin, 0, lon),
                )

        # The multispectral scanner covers the dayside it passes over
        self.assertGreater(self.scansat.coverage(self.kerbin, ScanType.biome), 0)
        self.assertGreater(
            self.scansat.coverage(self.kerbin, ScanType.visual_lo_res), 0
        )
        self.assertIsNone(self.scansat.biome(self.kerbin, 80, longitude))
        biome_longitude = self.covered_longitude(self.kerbin, ScanType.biome)
        self.assertEqual(
            self.kerbin.biome_at(0, biome_longitude),
            self.scansat.biome(self.kerbin, 0, biome_longitude),
        )

    def test_map(self):
        image = self.scansat.map(self.kerbin, self.scansat.MapType.altimetry)
        assert_png(image, (720, 360))

    def test_map_jpeg(self):
        image = self.scansat.map(
            self.kerbin,
            self.scansat.MapType.altimetry,
            format=self.scansat.ImageFormat.jpeg,
            quality=50,
        )
        assert_jpeg(image)

    def test_map_options(self):
        cases = [
            (map_type, projection)
            for map_type in self.scansat.MapType
            for projection in self.scansat.MapProjection
        ]
        for map_type, projection in cases:
            with self.subTest(map_type=map_type, projection=projection):
                image = self.scansat.map(
                    self.kerbin, map_type, width=360, projection=projection, color=False
                )
                assert_png(image, (360, 180))

    def test_map_resource(self):
        image = self.scansat.map(
            self.kerbin, self.scansat.MapType.altimetry, width=1440, resource="Ore"
        )
        self.assertEqual((1440, 720), png_size(image))

    def test_map_invalid_arguments(self):
        with self.assertRaises(ValueError):
            self.scansat.map(self.kerbin, self.scansat.MapType.altimetry, width=100)
        with self.assertRaises(ValueError):
            self.scansat.map(
                self.kerbin, self.scansat.MapType.altimetry, resource="NotAResource"
            )
        with self.assertRaises(ValueError):
            self.scansat.map(
                self.kerbin,
                self.scansat.MapType.altimetry,
                format=self.scansat.ImageFormat.jpeg,
                quality=0,
            )


class TestScanSatHighOrbit(TestCase):
    mods = ["SCANsat"]

    @classmethod
    def setUpClass(cls):
        cls.vessel = cls.launch("ScanSat", always_load=True)
        conn = cls.connect()
        cls.scansat = conn.scan_sat
        cls.sc = conn.space_center
        cls.kerbin = cls.sc.bodies["Kerbin"]
        # Polar, for the stock orbital survey
        radius = cls.kerbin.equatorial_radius + HIGH_ALTITUDE
        cls.set_orbit("Kerbin", radius, 0, 90, 0, 0, 0, cls.sc.ut)

    def test_high_resolution_altimetry(self):
        scanner = self.scansat.scanner(
            self.vessel.parts.with_name("scansat-sar-radarsat-2-1")[0]
        )
        scanner.active = True
        try:
            ScannerState = self.scansat.ScannerState
            scanning = (ScannerState.in_range, ScannerState.in_best_range)
            self.wait_until(lambda: scanner.state in scanning)
            self.sc.warp_to(self.sc.ut + self.vessel.orbit.period)
        finally:
            scanner.active = False
        hi_res = self.scansat.ScanType.altimetry_hi_res
        self.assertGreater(self.scansat.coverage(self.kerbin, hi_res), 0)

        # High resolution altimetry gives the height to 0.1 m, and the slope
        longitudes = [
            longitude
            for longitude in range(-180, 180)
            if self.scansat.is_covered(self.kerbin, 0, longitude, hi_res)
        ]
        self.assertNotEqual([], longitudes)
        for longitude in longitudes:
            with self.subTest(longitude=longitude):
                self.assertAlmostEqual(
                    self.kerbin.bedrock_height(0, longitude),
                    self.scansat.elevation(self.kerbin, 0, longitude),
                    delta=0.1,
                )
                slope = self.scansat.slope(self.kerbin, 0, longitude)
                assert slope is not None
                self.assertGreaterEqual(slope, 0)
                self.assertLessEqual(slope, 90)

    def test_resource_survey(self):
        # With instant resource scanning, the stock orbital survey fills the high
        # resolution resource scan. The survey sends its data over the antenna.
        ScanType = self.scansat.ScanType
        antenna = self.vessel.parts.with_name("longAntenna")[0].antenna
        assert antenna is not None
        antenna.deployed = True
        self.wait_until(lambda: self.vessel.comms.can_transmit_science)
        # The transmission takes about 700 EC, more than a scan leaves
        self.fill_all_resources()
        part = self.vessel.parts.with_name("SurveyScanner")[0]
        module = next(m for m in part.modules if m.name == "ModuleOrbitalSurveyor")
        next(e for e in module.event_list if e.name == "PerformSurvey").trigger()
        self.wait_until(
            lambda: self.scansat.coverage(self.kerbin, ScanType.resource_hi_res) == 1,
            timeout=120,
        )
        self.assertEqual(
            0, self.scansat.coverage(self.kerbin, ScanType.resource_lo_res)
        )
        abundances = []
        for latitude in range(-60, 61, 30):
            for longitude in range(-180, 180, 30):
                abundance = self.scansat.resource_abundance(
                    self.kerbin, latitude, longitude, "Ore"
                )
                assert abundance is not None
                abundances.append(abundance)
        for abundance in abundances:
            self.assertGreaterEqual(abundance, 0)
            self.assertLessEqual(abundance, 1)
        self.assertGreater(max(abundances), 0)
        # The high resolution scan does not round to 5%
        self.assertTrue(any(abs(x * 20 - round(x * 20)) > 1e-6 for x in abundances))


class TestScanSatLanded(TestCase):
    mods = ["SCANsat"]

    @classmethod
    def setUpClass(cls):
        cls.vessel = cls.launch("ScanSat", always_load=True)
        conn = cls.connect()
        cls.scansat = conn.scan_sat
        cls.sc = conn.space_center
        cls.kerbin = cls.sc.bodies["Kerbin"]

    def test_anomalies(self):
        # The anomaly detail scan finds the space center from the launchpad
        self.assertEqual([], self.scansat.anomalies(self.kerbin))
        scanner = self.scansat.scanner(
            self.vessel.parts.with_name("scansat-exomars-1")[0]
        )
        scanner.active = True
        try:
            self.wait_until(lambda: len(self.scansat.anomalies(self.kerbin)) > 0)
        finally:
            scanner.active = False
        flight = self.vessel.flight(self.kerbin.reference_frame)
        anomaly = min(
            self.scansat.anomalies(self.kerbin),
            key=lambda a: (
                abs(a.latitude - flight.latitude) + abs(a.longitude - flight.longitude)
            ),
        )
        self.assertEqual("KSC", anomaly.name)
        self.assertAlmostEqual(flight.latitude, anomaly.latitude, delta=0.1)
        self.assertAlmostEqual(flight.longitude, anomaly.longitude, delta=0.1)


if __name__ == "__main__":
    unittest.main()
