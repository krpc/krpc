#!/usr/bin/env python

import subprocess
import threading
import time

def build_api():
    while True:
        subprocess.check_call(['make', 'wait-api-changed'])
        subprocess.check_call(['make', 'python-api'])

def serve():
    subprocess.check_call(['make', 'sphinx-autobuild'])

t1 = threading.Thread(target=build_api)
t1.start()
time.sleep(0.25)
t2 = threading.Thread(target=serve)
t2.start()

try:
    t1.join()
    t2.join()
except KeyboardInterrupt:
    print 'Shutting down...'
