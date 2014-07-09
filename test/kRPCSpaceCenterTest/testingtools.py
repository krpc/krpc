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

class TestCase(unittest.TestCase):

    def assertBetween(self, min_value, max_value, value):
        if min_value < value and value < max_value:
            return
        self.fail('%f is not between %f and %f' % (value,min_value,max_value))

    def assertNotBetween(self, min_value, max_value, value):
        if value < min_value or max_value < value:
            return
        self.fail('%f is between %f and %f' % (value,min_value,max_value))

    def assertClose(self, expected, actual, error=0.001):
        if type(expected) in (list,tuple):
            for x,y in itertools.izip(expected, actual):
                if len(expected) != len(actual):
                    self.fail(str(actual) + ' is not close to ' + str(expected))
                self.assertBetween(x-error, x+error, y)
        else:
            self.assertBetween(expected-error, expected+error, actual)

    def assertCloseDegrees(self, expected, actual, error=0.001):
        def _clamp_degrees(a):
            a = a % 360
            if a < 0:
                a += 360
            return a

        min, max = _clamp_degrees(expected - error), _clamp_degrees(expected + error)
        actual = _clamp_degrees(actual)

        if min < actual and actual < max:
            return
        if (min > max) and (0 <= actual) and (actual < max):
            return
        if (min > max) and (min < actual) and (actual <= 360):
            return

        self.fail('Angle %.2f is not close to %.2f' % (actual, expected))
