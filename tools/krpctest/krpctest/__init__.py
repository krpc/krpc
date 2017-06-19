import unittest
import inspect
import math
import os
import shutil
import sys
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
    def new_save(cls, name='krpctest', always_load=False):
        # Return if the save is already loaded
        if not always_load and \
           cls.connect().testing_tools.current_save == name:
            return

        # Load a blank save with the given name
        blank_save = resource_filename(
            Requirement.parse('krpctest'), 'krpctest/' + name + '.sfs')
        save_path = os.path.join(_get_ksp_dir(), 'saves', name)
        if not os.path.exists(save_path):
            os.makedirs(save_path)
        shutil.copy(blank_save, os.path.join(save_path, 'persistent.sfs'))
        cls.connect().testing_tools.load_save(name, 'persistent')

    @classmethod
    def remove_other_vessels(cls):
        cls.connect().testing_tools.remove_other_vessels()

    @classmethod
    def launch_vessel_from_vab(cls, name, directory=None):
        # Copy craft file to save directory
        if directory is None:
            directory = os.path.join(
                os.getcwd(), os.path.dirname(sys.argv[0]), 'craft')
        fixtures_path = os.path.abspath(directory)
        save_path = os.path.join(
            _get_ksp_dir(), 'saves', cls.connect().testing_tools.current_save)
        if not os.path.exists(save_path):
            os.makedirs(save_path)
        ships_path = os.path.join(save_path, 'Ships', 'VAB')
        if not os.path.exists(ships_path):
            os.makedirs(ships_path)
        shutil.copy(os.path.join(fixtures_path, name + '.craft'),
                    os.path.join(ships_path, name + '.craft'))
        # Launch the craft
        cls.connect().space_center.launch_vessel_from_vab(name)

    @classmethod
    def set_orbit(cls, body, semi_major_axis, eccentricity, inclination,
                  longitude_of_ascending_node, argument_of_periapsis,
                  mean_anomaly_at_epoch, epoch):
        cls.connect().testing_tools.set_orbit(
            body, semi_major_axis, eccentricity, inclination,
            longitude_of_ascending_node, argument_of_periapsis,
            mean_anomaly_at_epoch, epoch)

    @classmethod
    def set_circular_orbit(cls, body, altitude):
        cls.connect().testing_tools.set_circular_orbit(body, altitude)

    @classmethod
    def wait(cls, timeout=0.1):
        time.sleep(timeout)

    def assertAlmostEqual(self, expected, actual,
                          places=7, msg=None, delta=None):
        """ Check that actual is equal to expected, within the given error """
        if not self._is_almost_equal(expected, actual, places, delta):
            if msg is None:
                msg = self._almost_equal_summary(
                    actual, expected, 'not almost equal')
                msg = self._almost_equal_error_msg(msg, places, delta)
            self.fail(msg)

    def assertNotAlmostEqual(self, expected, actual,
                             places=7, msg=None, delta=None):
        """ Check that actual is not equal to expected,
            within the given error """
        if self._is_almost_equal(expected, actual, places, delta):
            if msg is None:
                msg = self._almost_equal_summary(
                    actual, expected, 'almost equal')
                msg = self._almost_equal_error_msg(msg, places, delta)
            self.fail(msg)

    def assertDegreesAlmostEqual(self, expected, actual,
                                 places=7, msg=None, delta=None):
        """ Check that angle actual is equal to angle expected,
            within the given error.
            Uses clock arithmetic to compare angles, in range (0,360] """

        def clamp_degrees(angle):
            angle = angle % 360
            if angle < 0:
                angle += 360
            return angle

        expected_clamped = clamp_degrees(expected)
        actual_clamped = clamp_degrees(actual)

        if msg is None:
            msg = self._almost_equal_error_msg(
                'Angle %f is not close to %f' %
                (actual, expected), places, delta)

        if delta is not None:
            min_degrees = clamp_degrees(expected - delta)
            max_degrees = clamp_degrees(expected + delta)
            if max_degrees >= actual_clamped and \
               actual_clamped >= min_degrees:
                return
            if min_degrees > max_degrees and \
               max_degrees >= actual_clamped and \
               actual_clamped >= 0:
                return
            if min_degrees > max_degrees and \
               min_degrees <= actual_clamped and \
               actual_clamped <= 360:
                return
            self.fail(msg)

        else:
            self.assertAlmostEqual(expected_clamped, actual_clamped,
                                   msg=msg, places=places)

    def assertQuaternionsAlmostEqual(self, expected, actual,
                                     places=7, msg=None, delta=None):
        """ Check that a pair of quaternions represent the same orientation,
            within the given error. """
        for mult in [1, -1]:
            if self._is_almost_equal(
                    [x * mult for x in expected], actual, places, delta):
                return
        if msg is None:
            msg = self._almost_equal_summary(
                actual, expected, 'not almost equivalent orientations')
            msg = self._almost_equal_error_msg(msg, places, delta)
            self.fail(msg)

    @staticmethod
    def _is_value_almost_equal(expected, actual, places, delta=None):
        diff = abs(expected - actual)
        if delta is not None:
            return diff <= delta
        return round(diff, places) == 0

    def _is_almost_equal(self, expected, actual, places, delta=None):
        if isinstance(expected, list) or isinstance(expected, tuple):
            return self._list_is_almost_equal(expected, actual, places, delta)
        elif isinstance(expected, dict):
            return self._dict_is_almost_equal(expected, actual, places, delta)
        return self._is_value_almost_equal(expected, actual, places, delta)

    def _list_is_almost_equal(self, expected, actual, places, delta=None):
        if len(expected) != len(actual):
            return False
        for x, y in zip(expected, actual):
            if not self._is_almost_equal(x, y, places, delta):
                return False
        return True

    def _dict_is_almost_equal(self, expected, actual, places, delta=None):
        if set(expected.keys()) != set(actual.keys()):
            return False
        for k in expected.keys():
            if not self._is_almost_equal(
                    expected[k], actual[k], places, delta):
                return False
        return True

    @staticmethod
    def _almost_equal_summary(actual, expected, comparison):
        if isinstance(expected, list) or isinstance(expected, tuple):
            return '%s is %s to %s' % (str(actual), comparison, str(expected))
        elif isinstance(expected, dict):
            return '%s is %s to %s' % (str(actual), comparison, str(expected))
        else:
            return '%f is %s to %f' % (actual, comparison, expected)

    @staticmethod
    def _almost_equal_error_msg(msg, places, delta):
        if delta is not None:
            return '%s, within a delta of %f' % (msg, delta)
        else:
            return '%s, to %d places' % (msg, places)

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
