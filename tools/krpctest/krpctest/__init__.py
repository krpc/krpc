import unittest
import inspect
import math
import os
import shutil
import krpc

def connect(test_suite=None, test_case=None):
    address = '127.0.0.1'
    if 'KRPC_ADDRESS' in os.environ:
        address = os.environ['KRPC_ADDRESS']
    if test_suite and inspect.isclass(test_suite):
        name = test_suite.__name__
    elif test_suite:
        name = test_suite.__class__.__name__
    else:
        name = 'krpctest'
    if test_case:
        name += '.'+test_case
    return krpc.connect(name=name, address=address)

def get_ksp_dir():
    path = None
    if 'KSP_DIR' in os.environ:
        path = os.environ['KSP_DIR']
    if not path or not os.path.exists(path):
        raise RuntimeError('KSP dir not found at %s' % path)
    return path

def _connect():
    if not _connect.conn:
        _connect.conn = connect()
    return _connect.conn
_connect.conn = None

def new_save(name='test'):
    # Return if the save is already running
    if _connect().testing_tools.current_save == name:
        return

    # Load a new save using template from fixtures directory
    fixtures_path = os.path.abspath('fixtures')
    save_path = os.path.join(get_ksp_dir(), 'saves', name)
    if not os.path.exists(save_path):
        os.makedirs(save_path)
    shutil.copy(os.path.join(fixtures_path, 'blank.sfs'), os.path.join(save_path, 'persistent.sfs'))
    _connect().testing_tools.load_save('test', 'persistent')

def load_save(name):
    # Copy save file to save directory
    fixtures_path = os.path.join(os.path.dirname(os.path.realpath(__file__)), 'fixtures')
    save_path = os.path.join(get_ksp_dir(), 'saves', 'test')
    if not os.path.exists(save_path):
        os.makedirs(save_path)
    shutil.copy(os.path.join(fixtures_path, name + '.sfs'), os.path.join(save_path, name + '.sfs'))

    # Load the save file
    _connect().testing_tools.load_save('test', name)

def remove_other_vessels():
    _connect().testing_tools.remove_other_vessels()

def launch_vessel_from_vab(name):
    # Copy craft file to save directory
    fixtures_path = os.path.abspath('fixtures')
    save_path = os.path.join(get_ksp_dir(), 'saves', _connect().testing_tools.current_save)
    if not os.path.exists(save_path):
        os.makedirs(save_path)
    ships_path = os.path.join(save_path, 'Ships', 'VAB')
    if not os.path.exists(ships_path):
        os.makedirs(ships_path)
    shutil.copy(os.path.join(fixtures_path, name + '.craft'), os.path.join(ships_path, name + '.craft'))

    # Launch the craft
    _connect().space_center.launch_vessel_from_vab(name)

def set_orbit(body, semi_major_axis, eccentricity, inclination, longitude_of_ascending_node,
              argument_of_periapsis, mean_anomaly_at_epoch, epoch):
    _connect().testing_tools.set_orbit(
        body, semi_major_axis, eccentricity, inclination, longitude_of_ascending_node,
        argument_of_periapsis, mean_anomaly_at_epoch, epoch)

def set_circular_orbit(body, altitude):
    _connect().testing_tools.set_circular_orbit(body, altitude)

class TestCase(unittest.TestCase):

    @staticmethod
    def _is_in_range(min_value, max_value, value):
        return min_value <= value and value <= max_value

    def _is_close(self, expected, actual, error=0.001):
        if isinstance(expected, list) or isinstance(expected, tuple):
            return self._list_is_close(expected, actual, error)
        elif isinstance(expected, dict):
            return self._dict_is_close(expected, actual, error)
        else:
            return self._is_in_range(expected-error, expected+error, actual)

    def _list_is_close(self, expected, actual, error):
        if len(expected) != len(actual):
            return False
        for x, y in zip(expected, actual):
            if not self._is_in_range(x - error, x + error, y):
                return False
        return True

    def _dict_is_close(self, expected, actual, error):
        if set(expected.keys()) != set(actual.keys()):
            return False
        for k in expected.keys():
            x = expected[k]
            y = actual[k]
            if not self._is_in_range(x - error, x + error, y):
                return False
        return True

    def assertNotClose(self, expected, actual, error=0.01):
        """ Check that actual is not equal to expected, within the given absolute error
            i.e. actual is not in the range (expected-error, expected+error) """
        if self._is_close(expected, actual, error):
            if isinstance(expected, list) or isinstance(expected, tuple):
                args = [str(tuple(x)) for x in (actual, expected)] + [error]
                self.fail('%s is close to %s, within an absolute error of %f' % tuple(args))
            else:
                self.fail('%f is close to %f, within an absolute error of %f' % (actual, expected, error))

    def assertClose(self, expected, actual, error=0.01):
        """ Check that actual is equal to expected, within the given absolute error
            i.e. actual is in the range (expected-error, expected+error) """
        if not self._is_close(expected, actual, error):
            if isinstance(expected, list) or isinstance(expected, tuple):
                args = [str(tuple(x)) for x in (actual, expected)] + [error]
                self.fail('%s is not close to %s, within an absolute error of %f' % tuple(args))
            elif isinstance(expected, dict):
                args = [str(dict(x)) for x in (actual, expected)] + [error]
                self.fail('%s is not close to %s, within an absolute error of %f' % tuple(args))
            else:
                self.fail('%f is not close to %f, within an absolute error of %f' % (actual, expected, error))

    def assertCloseDegrees(self, expected, actual, error=0.001):
        """ Check that angle actual is equal to angle expected, within the given absolute error.
            Uses clock arithmetic to compare angles, in range (0,360] """
        def _clamp_degrees(angle):
            angle = angle % 360
            if angle < 0:
                angle += 360
            return angle

        min_degrees, max_degrees = _clamp_degrees(expected - error), _clamp_degrees(expected + error)
        actual_clamped = _clamp_degrees(actual)

        if max_degrees >= actual_clamped and actual_clamped >= min_degrees:
            return
        if min_degrees > max_degrees and max_degrees >= actual_clamped and actual_clamped >= 0:
            return
        if min_degrees > max_degrees and min_degrees <= actual_clamped and actual_clamped <= 360:
            return

        self.fail('Angle %.2f is not close to %.2f, within an absolute error of %f' % (actual, expected, error))

    def assertIsNaN(self, value):
        """ Check that the value is nan """
        msg = '%s is not nan' % str(value)
        try:
            if not math.isnan(value):
                self.fail(msg)
        except TypeError:
            self.fail(msg)

    def assertIsNotNaN(self, value):
        """ Check that the value is nan """
        msg = '%s is nan' % str(value)
        try:
            if math.isnan(value):
                self.fail(msg)
        except TypeError:
            self.fail(msg)
