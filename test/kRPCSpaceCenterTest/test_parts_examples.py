import unittest
import testingtools
import krpc
import math

class TestParts(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        if krpc.connect().space_center.active_vessel.name != 'Parts':
            testingtools.new_save()
            testingtools.launch_vessel_from_vab('Parts')
            testingtools.remove_other_vessels()
        cls.conn = krpc.connect(name='TestParts')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_calculate_deltav(self):
        total_deltav = 0
        total_mass = 0

        # Get the vessels parts as a dict keyed by stage number
        stages = {}
        for part in self.parts.all:
            stage = part.decouple_stage
            if stage not in stages:
                stages[stage] = []
            stages[stage].append(part)

        # Compute deltav in each stage, top to bottom
        for stage, parts in sorted(stages.items(), key=lambda (x,y): x):

            # Get masses
            stage_mass = sum(part.mass for part in parts)
            total_mass += stage_mass
            lfo_mass = sum(part.resources.amount('LiquidFuel')*5.0 for part in parts)
            ox_mass = sum(part.resources.amount('Oxidizer')*5.0 for part in parts)
            solidfuel_mass = sum(part.resources.amount('SolidFuel')*7.5 for part in parts)
            fuel_mass = lfo_mass + ox_mass + solidfuel_mass
            dry_mass = total_mass - fuel_mass

            # Skip the stage if it's massless
            if dry_mass == 0:
                continue

            # Compute specific impule of stage
            engines = filter(None, [part.engine for part in parts])
            if len(engines) == 0:
                isp = 0
            else:
                thrust = [e.max_thrust for e in engines]
                fuelflow = [e.max_thrust / e.vacuum_specific_impulse for e in engines]
                isp = sum(thrust) / sum(fuelflow)

            # Compute stage deltav
            deltav = isp * 9.82 * math.log(total_mass / dry_mass)

            print
            print 'Stage %d' % stage
            print '---------------'
            print
            print 'Delta-V    = % 5d m/s' % deltav
            print 'Stage mass = % 5d T' % (stage_mass/1000.)
            print 'Sotal mass = % 5d T' % (total_mass/1000.)
            print
            print 'Parts:'
            print '  ' + '\n  '.join(p.title for p in parts)

            total_deltav += deltav

        print
        print 'Total Delta-V = %d m/s' % total_deltav

if __name__ == "__main__":
    unittest.main()
