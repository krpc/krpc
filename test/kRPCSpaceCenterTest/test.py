#!/usr/bin/env python2

import unittest
import os

def main():
    suite = unittest.TestLoader().discover(os.path.dirname(__file__), pattern='test_*.py')
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    if not result.wasSuccessful():
        exit(1)

if __name__ == '__main__':
    main()
