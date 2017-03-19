from setuptools import setup
import sys
import os
import re

dirpath = os.path.dirname(os.path.realpath(__file__))

# Dirty hack to make setuptools use file copies instead of hardlinks which do not play well with Bazel
# http://bugs.python.org/issue8876
if os.getenv('BAZEL_BUILD') and hasattr(os, 'link'):
    del os.link

# Fix dirpath when running Bazel in standalone mode
if os.getenv('BAZEL_BUILD') and not os.path.exists(os.path.join(dirpath, 'VERSION.txt')):
    dirpath = os.getcwd()

install_requires=['krpc']
setup(
    name='krpctest',
    version=re.search(r'\'(.+)\'', open(os.path.join(dirpath, 'krpctest/version.py')).read()).group(1),
    author='djungelorm',
    author_email='djungelorm@users.noreply.github.com',
    url='https://krpc.github.io/krpc',
    license='GNU GPL v3',
    description='Utilities for running service tests for kRPC.',
    long_description=open(os.path.join(dirpath, 'README.txt')).read(),
    packages=['krpctest', 'krpctest.test'],
    package_data={'': ['krpctest.sfs', 'krpctest_career.sfs']},
    install_requires=install_requires,
    test_suite='krpctest.test',
    use_2to3=True,
    classifiers=[
        'Development Status :: 5 - Production/Stable',
        'Intended Audience :: End Users/Desktop',
        'License :: OSI Approved :: GNU General Public License v3 (GPLv3)',
        'Natural Language :: English',
        'Programming Language :: Python :: 2.7',
        'Programming Language :: Python :: 3',
        'Operating System :: Microsoft :: Windows',
        'Operating System :: Unix'
    ]
)
