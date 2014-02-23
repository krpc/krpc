#!/usr/bin/env python2

import unittest
import os

suite = unittest.TestLoader().discover(os.path.dirname(__file__), pattern='test_*.py')
unittest.TextTestRunner(verbosity=2).run(suite)

