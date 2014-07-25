import krpc
import time
import unittest
import os
import shutil
import itertools

def load_save(name):
    save_dir = os.getenv('KSP_DIR') + '/saves/test'
    if not os.path.exists(save_dir):
        os.makedirs(save_dir)
    shutil.copy(os.path.dirname(os.path.realpath(__file__)) + '/fixtures/' + name + '.sfs', save_dir + '/' + name + '.sfs')
    # Connect and issue load save RPC
    ksp = krpc.connect(name='testingtools.load_save')
    ksp.testing_tools.load_save('test', name)
    time.sleep(1)
    # Wait until server comes back up
    while True:
        try:
            ksp = krpc.connect(name='testingtools.load_save')
            break
        except:
            time.sleep(0.2)
            pass
    # Wait until the vessel is loaded properly
    time.sleep(0.2)

def new_save(name='test'):
    save_dir = os.getenv('KSP_DIR') + '/saves/' + name
    if not os.path.exists(save_dir):
        os.makedirs(save_dir)
    shutil.copy(os.path.dirname(os.path.realpath(__file__)) + '/fixtures/blank.sfs', save_dir + '/persistent.sfs')

    # Connect and issue load save RPC
    ksp = krpc.connect()
    ksp.testing_tools.load_save('test', 'persistent')
    time.sleep(1)
    # Wait until server comes back up
    while True:
        try:
            ksp = krpc.connect()
            del ksp
            break
        except:
            time.sleep(0.2)
            pass
    # Wait until the vessel is loaded properly
    time.sleep(0.2)

def launch_vessel_from_vab(name):
    #TODO: assumes test is the currently loaded save
    save_dir = os.getenv('KSP_DIR') + '/saves/test'
    if not os.path.exists(save_dir):
        os.makedirs(save_dir)
    ships_dir = save_dir + '/Ships/VAB'
    if not os.path.exists(ships_dir):
        os.makedirs(ships_dir)
    shutil.copy(os.path.dirname(os.path.realpath(__file__)) + '/fixtures/' + name + '.craft', ships_dir + '/' + name + '.craft')

    # Connect and issue load save RPC
    ksp = krpc.connect(name='testingtools.launch_vessel_from_vab')
    ksp.testing_tools.launch_vessel_from_vab(name)
    time.sleep(1)
    # Wait until server comes back up
    while True:
        try:
            ksp = krpc.connect(name='testingtools.launch_vessel_from_vab')
            break
        except:
            time.sleep(0.2)
            pass
    # Wait until the vessel is loaded properly
    time.sleep(1)

def set_orbit(body, sma, e, inc, lan, w, mEp, epoch):
    conn = krpc.connect()
    conn.testing_tools.set_orbit(body, sma, e, inc, lan, w, mEp, epoch)
    del conn

def set_circular_orbit(body, altitude):
    conn = krpc.connect()
    conn.testing_tools.set_circular_orbit(body, altitude)
    del conn

class TestCase(unittest.TestCase):

    def _isInRange(self, min_value, max_value, value):
        return min_value <= value and value <= max_value

    def _isClose(self, expected, actual, error=0.001):
        if type(expected) in (list,tuple):
            ok = True
            if len(expected) != len(actual):
                ok = False
            else:
                for x,y in itertools.izip(expected, actual):
                    if not self._isInRange(x-error, x+error, y):
                        ok = False
                        break
            if not ok:
                return False
        elif not self._isInRange(expected-error, expected+error, actual):
            return False
        return True

    def assertNotClose(self, expected, actual, error=0.01):
        """ Check that actual is not equal to expected, within the given absolute error
            i.e. actual is not in the range (expected-error,expected+error) """
        if self._isClose(expected, actual, error):
            if type(expected) in (list,tuple):
                args = [str(tuple(x)) for x in (actual,expected)] + [error]
                self.fail('%s is close to %s, within an absolute error of %f' % tuple(args))
            else:
                self.fail('%f is close to %f, within an absolute error of %f' % (actual,expected,error))

    def assertClose(self, expected, actual, error=0.01):
        """ Check that actual is equal to expected, within the given absolute error
            i.e. actual is in the range (expected-error,expected+error) """
        if not self._isClose(expected, actual, error):
            if type(expected) in (list,tuple):
                args = [str(tuple(x)) for x in (actual,expected)] + [error]
                self.fail('%s is not close to %s, within an absolute error of %f' % tuple(args))
            else:
                self.fail('%f is not close to %f, within an absolute error of %f' % (actual,expected,error))

    def assertCloseDegrees(self, expected, actual, error=0.001):
        """ Check that angle actual is equal to angle expected, within the given absolute error.
            Uses clock arithmetic to compare angles, in range (0,360] """
        def _clamp_degrees(a):
            a = a % 360
            if a < 0:
                a += 360
            return a

        min, max = _clamp_degrees(expected - error), _clamp_degrees(expected + error)
        actual_clamped = _clamp_degrees(actual)

        if min <= actual_clamped and actual_clamped <= max:
            return
        if (min > max) and (0 <= actual_clamped) and (actual_clamped <= max):
            return
        if (min > max) and (min <= actual_clamped) and (actual_clamped <= 360):
            return

        self.fail('Angle %.2f is not close to %.2f, within an absolute error of %f' % (actual, expected, error))
