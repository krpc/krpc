import unittest
import os
import krpctest
import krpc

def main():
    try:
        conn = krpctest.connect()
    except krpc.error.NetworkError:
        print 'kRPC server not running'
        exit(1)
    conn.close()

    suite = unittest.TestLoader().discover(os.path.dirname(__file__), pattern='test_*.py')
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    if not result.wasSuccessful():
        exit(1)

if __name__ == '__main__':
    main()
