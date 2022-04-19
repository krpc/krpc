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

    def assertAlmostEqual(self, first, second,
                          places=7, msg=None, delta=None):
        """ Check that first is equal to second, within the given error """
        if not self._is_almost_equal(first, second, places, delta):
            if msg is None:
                msg = self._almost_equal_summary(
                    first, second, 'not almost equal')
                msg = self._almost_equal_error_msg(msg, places, delta)
            self.fail(msg)

    def assertNotAlmostEqual(self, first, second,
                             places=7, msg=None, delta=None):
        """ Check that first is not equal to second,
            within the given error """
        if self._is_almost_equal(first, second, places, delta):
            if msg is None:
                msg = self._almost_equal_summary(
                    first, second, 'almost equal')
                msg = self._almost_equal_error_msg(msg, places, delta)
            self.fail(msg)

    def assertDegreesAlmostEqual(self, first, second,
                                 places=7, msg=None, delta=None):
        """ Check that angle first is equal to angle second,
            within the given error.
            Uses clock arithmetic to compare angles, in range (0,360] """

        def clamp_degrees(angle):
            angle = angle % 360
            if angle < 0:
                angle += 360
            return angle

        first_clamped = clamp_degrees(first)
        second_clamped = clamp_degrees(second)

        if msg is None:
            msg = self._almost_equal_error_msg(
                'Angle %f is not close to %f' %
                (first, second), places, delta)

        if delta is not None:
            min_degrees = clamp_degrees(first - delta)
            max_degrees = clamp_degrees(first + delta)
            if max_degrees >= second_clamped and \
               second_clamped >= min_degrees:
                return
            if min_degrees > max_degrees and \
               max_degrees >= second_clamped and \
               second_clamped >= 0:
                return
            if min_degrees > max_degrees and \
               min_degrees <= second_clamped and \
               second_clamped <= 360:
                return
            self.fail(msg)

        else:
            self.assertAlmostEqual(first_clamped, second_clamped,
                                   msg=msg, places=places)

    def assertQuaternionsAlmostEqual(self, second, first,
                                     places=7, msg=None, delta=None):
        """ Check that a pair of quaternions represent the same orientation,
            within the given error. """
        for mult in [1, -1]:
            if self._is_almost_equal(
                    [x * mult for x in second], first, places, delta):
                return
        if msg is None:
            msg = self._almost_equal_summary(
                first, second, 'not almost equivalent orientations')
            msg = self._almost_equal_error_msg(msg, places, delta)
            self.fail(msg)

    @staticmethod
    def _is_value_almost_equal(second, first, places, delta=None):
        diff = abs(second - first)
        if delta is not None:
            return diff <= delta
        return round(diff, places) == 0

    def _is_almost_equal(self, second, first, places, delta=None):
        if isinstance(second, (list, tuple)):
            return self._list_is_almost_equal(second, first, places, delta)
        elif isinstance(second, dict):
            return self._dict_is_almost_equal(second, first, places, delta)
        return self._is_value_almost_equal(second, first, places, delta)

    def _list_is_almost_equal(self, second, first, places, delta=None):
        if len(second) != len(first):
            return False
        for x, y in zip(second, first):
            if not self._is_almost_equal(x, y, places, delta):
                return False
        return True

    def _dict_is_almost_equal(self, second, first, places, delta=None):
        if set(second.keys()) != set(first.keys()):
            return False
        for k in list(second.keys()):
            if not self._is_almost_equal(
                    second[k], first[k], places, delta):
                return False
        return True

    @staticmethod
    def _almost_equal_summary(first, second, comparison):
        if isinstance(second, (list, tuple)):
            return '%s is %s to %s' % (str(first), comparison, str(second))
        elif isinstance(second, dict):
            return '%s is %s to %s' % (str(first), comparison, str(second))
        return '%f is %s to %f' % (first, comparison, second)

    @staticmethod
    def _almost_equal_error_msg(msg, places, delta):
        if delta is not None:
            return '%s, within a delta of %f' % (msg, delta)
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
