#!/usr/bin/env python2

import unittest
import testingtools
from testingtools import load_save
import krpc

class TestOrbit(testingtools.TestCase):

    def test_orbit_kerbin(self):
        load_save('orbit-kerbin')
        self.ksp = krpc.connect()
        self.assertEqual("Kerbin", self.ksp.Orbit.Body)
        self.assertClose(787616.854842, self.ksp.Orbit.Periapsis)
        self.assertClose(1039066.5139,  self.ksp.Orbit.Apoapsis)
        self.assertClose(787616.854842 - 600000 , self.ksp.Orbit.PeriapsisAltitude)
        self.assertClose(1039066.5139  - 600000,  self.ksp.Orbit.ApoapsisAltitude)
        self.assertClose(0.13765366421, self.ksp.Orbit.Eccentricity)
        self.assertClose(18.6979258534, self.ksp.Orbit.Inclination)
        self.assertClose(245.950438789, self.ksp.Orbit.LongitudeOfAscendingNode)
        self.assertClose(147.372378518, self.ksp.Orbit.ArgumentOfPeriapsis)
        self.assertClose(5.27122898283, self.ksp.Orbit.MeanAnomalyAtEpoch)

    def test_orbit_bop(self):
        load_save('orbit-bop')
        self.ksp = krpc.connect()
        self.assertEqual("Bop", self.ksp.Orbit.Body)
        self.assertClose(171597.08389707067 , self.ksp.Orbit.Periapsis)
        self.assertClose(244466.66085938932,  self.ksp.Orbit.Apoapsis)
        self.assertClose(171597.08389707067 - 65000, self.ksp.Orbit.PeriapsisAltitude)
        self.assertClose(244466.66085938932 - 65000,  self.ksp.Orbit.ApoapsisAltitude)
        self.assertClose(0.17514041509426, self.ksp.Orbit.Eccentricity)
        self.assertClose(27.4224603461332, self.ksp.Orbit.Inclination)
        self.assertClose(38.141690891514, self.ksp.Orbit.LongitudeOfAscendingNode)
        self.assertClose(241.186299925129, self.ksp.Orbit.ArgumentOfPeriapsis)
        self.assertClose(2.30395571797464, self.ksp.Orbit.MeanAnomalyAtEpoch)

    def test_orbit_mun_escape_soi(self):
        load_save('orbit-mun-escape-soi')
        self.ksp = krpc.connect()
        self.assertEqual("Mun", self.ksp.Orbit.Body)
        self.assertClose(828815.0308573832, self.ksp.Orbit.Periapsis)
        self.assertClose(2659937.0232935967, self.ksp.Orbit.Apoapsis)
        self.assertClose(628815.0308573832, self.ksp.Orbit.PeriapsisAltitude)
        self.assertClose(2459937.0232935967, self.ksp.Orbit.ApoapsisAltitude)
        self.assertClose(0.524864468444386, self.ksp.Orbit.Eccentricity)
        self.assertClose(0, self.ksp.Orbit.Inclination)
        self.assertClose(12.895673289558, self.ksp.Orbit.LongitudeOfAscendingNode)
        self.assertClose(67.3969257458513, self.ksp.Orbit.ArgumentOfPeriapsis)
        self.assertClose(6.21020183993513, self.ksp.Orbit.MeanAnomalyAtEpoch)

    def test_orbit_minmus_parabolic(self):
        load_save('orbit-minmus-parabolic')
        self.ksp = krpc.connect()
        self.assertEqual("Minmus", self.ksp.Orbit.Body)
        self.assertClose(87187.64537168786, self.ksp.Orbit.Periapsis)
        self.assertClose(-175327.32795440647, self.ksp.Orbit.Apoapsis)
        self.assertClose(27187.64537168786, self.ksp.Orbit.PeriapsisAltitude)
        self.assertClose(-235327.32795440647, self.ksp.Orbit.ApoapsisAltitude)
        self.assertClose(2.97839708101655, self.ksp.Orbit.Eccentricity)
        self.assertClose(168.280967855609, self.ksp.Orbit.Inclination)
        self.assertClose(181.171756205933, self.ksp.Orbit.LongitudeOfAscendingNode)
        self.assertClose(165.50774557981, self.ksp.Orbit.ArgumentOfPeriapsis)
        self.assertClose(-4.65482114687744, self.ksp.Orbit.MeanAnomalyAtEpoch)

if __name__ == "__main__":
    unittest.main()
