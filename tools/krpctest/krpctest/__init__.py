import unittest
import inspect
import math
import os
import shutil
import time
from pkg_resources import Requirement, resource_filename
import krpc

def _connect(use_cached=True):
    if _connect.connection is not None and use_cached:
        return _connect.connection
    address = '127.0.0.1'
    if 'KRPC_ADDRESS' in os.environ:
        address = os.environ['KRPC_ADDRESS']
    connection = krpc.connect(name='krpctest', address=address)
    if use_cached:
        _connect.connection = connection
    return connection

_connect.connection = None

def _get_ksp_dir():
    path = None
    if 'KSP_DIR' in os.environ:
        path = os.environ['KSP_DIR']
    if not path or not os.path.exists(path):
        raise RuntimeError('KSP dir not found at %s' % path)
    return path

class TestCase(unittest.TestCase):

    @classmethod
    def connect(cls, use_cached=True):
        return _connect(use_cached)

    @classmethod
    def new_save(cls, name='krpctest'):
        # Return if the save is already loaded
        if cls.connect().testing_tools.current_save == name:
            return

        # Load a blank save with the given name
        blank_save = resource_filename(Requirement.parse('krpctest'), 'krpctest/krpctest.sfs')
        save_path = os.path.join(_get_ksp_dir(), 'saves', name)
        if not os.path.exists(save_path):
            os.makedirs(save_path)
        shutil.copy(blank_save, os.path.join(save_path, 'persistent.sfs'))
        cls.connect().testing_tools.load_save(name, 'persistent')

    @classmethod
    def remove_other_vessels(cls):
        cls.connect().testing_tools.remove_other_vessels()

    @classmethod
    def launch_vessel_from_vab(cls, name, directory='craft'):
        # Copy craft file to save directory
        fixtures_path = os.path.abspath(directory)
        save_path = os.path.join(_get_ksp_dir(), 'saves', cls.connect().testing_tools.current_save)
        if not os.path.exists(save_path):
            os.makedirs(save_path)
        ships_path = os.path.join(save_path, 'Ships', 'VAB')
        if not os.path.exists(ships_path):
            os.makedirs(ships_path)
        shutil.copy(os.path.join(fixtures_path, name + '.craft'), os.path.join(ships_path, name + '.craft'))
        # Launch the craft
        cls.connect().space_center.launch_vessel_from_vab(name)

    @classmethod
    def set_orbit(cls, body, semi_major_axis, eccentricity, inclination, longitude_of_ascending_node,
                  argument_of_periapsis, mean_anomaly_at_epoch, epoch):
        cls.connect().testing_tools.set_orbit(
            body, semi_major_axis, eccentricity, inclination, longitude_of_ascending_node,
            argument_of_periapsis, mean_anomaly_at_epoch, epoch)

    @classmethod
    def set_circular_orbit(cls, body, altitude):
        cls.connect().testing_tools.set_circular_orbit(body, altitude)

    @classmethod
    def wait(cls, timeout=0.1):
        time.sleep(timeout)

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
