import krpc
import time
import unittest
import os
import shutil
import itertools

def get_ksp_dir():
    path = os.getenv('KSP_DIR')
    if path == None or not os.path.exists(path):
        raise RuntimeError ('KSP_DIR not found at %s' % path)
    return path

def new_save(name='test'):
    conn = krpc.connect(name='testingtools.new_save')

    # Return if the save is already running
    if conn.testing_tools.current_save == name:
        conn.close()
        return

    # Load a new save using template from fixtures directory
    fixtures_path = os.path.join(os.path.dirname(os.path.realpath(__file__)), 'fixtures')
    save_path = os.path.join(get_ksp_dir(), 'saves', name)
    if not os.path.exists(save_path):
        os.makedirs(save_path)
    shutil.copy(os.path.join(fixtures_path, 'blank.sfs'), os.path.join(save_path, 'persistent.sfs'))
    conn.testing_tools.load_save('test', 'persistent')
    conn.close()

    # Wait until server comes back up
    time.sleep(1)
    while True:
        try:
            conn = krpc.connect(name='testingtools.new_save')
            conn.close()
            break
        except:
            time.sleep(0.2)

    #TODO: remove sleep
    time.sleep(3)

def load_save(name):
    # Copy save file to save directory
    fixtures_path = os.path.join(os.path.dirname(os.path.realpath(__file__)), 'fixtures')
    save_path = os.path.join(get_ksp_dir(), 'saves', 'test')
    if not os.path.exists(save_path):
        os.makedirs(save_path)
    shutil.copy(os.path.join(fixtures_path, name + '.sfs'), os.path.join(save_path, name + '.sfs'))

    # Load the save file
    conn = krpc.connect(name='testingtools.load_save')
    conn.testing_tools.load_save('test', name)
    conn.close()

    # Wait until server comes back up
    time.sleep(1)
    while True:
        try:
            conn = krpc.connect(name='testingtools.load_save')
            conn.close()
            break
        except:
            time.sleep(0.2)

    #TODO: remove sleep
    time.sleep(3)

def remove_other_vessels():
    with krpc.connect(name='testingtools.remove_other_vessels') as conn:
        conn.testing_tools.remove_other_vessels()

def launch_vessel_from_vab(name):
    # Copy craft file to save directory
    with krpc.connect(name='testingtools.launch_vessel_from_vab') as conn:
       fixtures_path = os.path.join(os.path.dirname(os.path.realpath(__file__)), 'fixtures')
       save_path = os.path.join(get_ksp_dir(), 'saves', conn.testing_tools.current_save)
       if not os.path.exists(save_path):
           os.makedirs(save_path)
       ships_path = os.path.join(save_path, 'Ships', 'VAB')
       if not os.path.exists(ships_path):
           os.makedirs(ships_path)
       shutil.copy(os.path.join(fixtures_path, name + '.craft'), os.path.join(ships_path, name + '.craft'))

    # Launch the craft
    with krpc.connect(name='testingtools.launch_vessel_from_vab') as conn:
        conn.testing_tools.launch_vessel_from_vab(name)

    # Wait until server comes back up
    time.sleep(1)
    while True:
        try:
            conn = krpc.connect(name='testingtools.launch_vessel_from_vab')
            conn.close()
            break
        except:
            time.sleep(0.2)

    #TODO: remove sleep
    time.sleep(10)

def set_orbit(body, sma, e, inc, lan, w, mEp, epoch):
    with krpc.connect(name='testingtools.set_orbit') as conn:
        conn.testing_tools.set_orbit(body, sma, e, inc, lan, w, mEp, epoch)
    #TODO: remove sleep
    time.sleep(3)

def set_circular_orbit(body, altitude):
    with krpc.connect(name='testingtools.set_circular_orbit') as conn:
        conn.testing_tools.set_circular_orbit(body, altitude)
    #TODO: remove sleep
    time.sleep(3)

class TestCase(unittest.TestCase):

    def _isInRange(self, min_value, max_value, value):
        return min_value <= value and value <= max_value

    def _isClose(self, expected, actual, error=0.001):
        if type(expected) in (list,tuple):
            ok = True
            if len(expected) != len(actual):
                ok = False
            else:
                for x,y in zip(expected, actual):
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
