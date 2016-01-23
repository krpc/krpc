import unittest
import os

def main():
    # Check that a kRPC server is running
    import krpc
    try:
        conn = krpc.connect()
    except:
        print('kRPC server not running; skipping tests')
        exit(0)

    suite = unittest.TestLoader().discover(os.path.dirname(__file__), pattern='test_*.py')
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    if not result.wasSuccessful():
        exit(1)

if __name__ == '__main__':
    main()
