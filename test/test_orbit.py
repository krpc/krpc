#!/usr/bin/env python2

import unittest
import testingtools
from testingtools import load_save
import krpc

class TestOrbit(testingtools.TestCase):

    def test_orbit_kerbin(self):
        load_save('orbit-kerbin')
        ksp = krpc.connect()
        vessel = ksp.Flight.ActiveVessel
        orbit = vessel.Orbit
        self.assertEqual("Kerbin", orbit.Body)
        self.assertClose(1039066.5139,  orbit.Apoapsis)
        self.assertClose(787616.854842, orbit.Periapsis)
        self.assertClose(1039066.5139  - 600000,  orbit.ApoapsisAltitude)
        self.assertClose(787616.854842 - 600000 , orbit.PeriapsisAltitude)
        self.assertClose(1929.1, orbit.TimeToApoapsis, error=0.5)
        self.assertClose(469.9, orbit.TimeToPeriapsis, error=0.5)
        self.assertClose(0.13765366421, orbit.Eccentricity)
        self.assertClose(18.6979258534, orbit.Inclination)
        self.assertClose(245.950438789, orbit.LongitudeOfAscendingNode)
        self.assertClose(147.372378518, orbit.ArgumentOfPeriapsis)
        self.assertClose(5.27122898283, orbit.MeanAnomalyAtEpoch)

    def test_orbit_bop(self):
        load_save('orbit-bop')
        ksp = krpc.connect()
        vessel = ksp.Flight.ActiveVessel
        orbit = vessel.Orbit
        self.assertEqual("Bop", orbit.Body)
        self.assertClose(244466.66085938932,  orbit.Apoapsis)
        self.assertClose(171597.08389707067 , orbit.Periapsis)
        self.assertClose(244466.66085938932 - 65000,  orbit.ApoapsisAltitude)
        self.assertClose(171597.08389707067 - 65000, orbit.PeriapsisAltitude)
        self.assertClose(1593.33, orbit.TimeToApoapsis, error=0.5)
        self.assertClose(7570.88, orbit.TimeToPeriapsis, error=0.5)
        self.assertClose(0.17514041509426, orbit.Eccentricity)
        self.assertClose(27.4224603461332, orbit.Inclination)
        self.assertClose(38.141690891514, orbit.LongitudeOfAscendingNode)
        self.assertClose(241.186299925129, orbit.ArgumentOfPeriapsis)
        self.assertClose(2.30395571797464, orbit.MeanAnomalyAtEpoch)

    def test_orbit_mun_escape_soi(self):
        load_save('orbit-mun-escape-soi')
        ksp = krpc.connect()
        vessel = ksp.Flight.ActiveVessel
        orbit = vessel.Orbit
        self.assertEqual("Mun", orbit.Body)
        self.assertClose(2659937.0232935967, orbit.Apoapsis)
        self.assertClose(828815.0308573832, orbit.Periapsis)
        self.assertClose(2459937.0232935967, orbit.ApoapsisAltitude)
        self.assertClose(628815.0308573832, orbit.PeriapsisAltitude)
        self.assertClose(29017.6, orbit.TimeToApoapsis, error=0.5)
        self.assertClose(658.6, orbit.TimeToPeriapsis, error=0.5)
        self.assertClose(0.524864468444386, orbit.Eccentricity)
        self.assertClose(0, orbit.Inclination)
        self.assertClose(12.895673289558, orbit.LongitudeOfAscendingNode)
        self.assertClose(67.3969257458513, orbit.ArgumentOfPeriapsis)
        self.assertClose(6.21020183993513, orbit.MeanAnomalyAtEpoch)

    def test_orbit_minmus_parabolic(self):
        load_save('orbit-minmus-parabolic')
        ksp = krpc.connect()
        vessel = ksp.Flight.ActiveVessel
        orbit = vessel.Orbit
        self.assertEqual("Minmus", orbit.Body)
        self.assertClose(-175327.32795440647, orbit.Apoapsis)
        self.assertClose(87187.64537168786, orbit.Periapsis)
        self.assertClose(-235327.32795440647, orbit.ApoapsisAltitude)
        self.assertClose(27187.64537168786, orbit.PeriapsisAltitude)
        self.assertClose(0, orbit.TimeToApoapsis, error=0.5)
        self.assertClose(1024.43, orbit.TimeToPeriapsis, error=0.5)
        self.assertClose(2.97839708101655, orbit.Eccentricity)
        self.assertClose(168.280967855609, orbit.Inclination)
        self.assertClose(181.171756205933, orbit.LongitudeOfAscendingNode)
        self.assertClose(165.50774557981, orbit.ArgumentOfPeriapsis)
        self.assertClose(-4.65482114687744, orbit.MeanAnomalyAtEpoch)

if __name__ == "__main__":
    unittest.main()
