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

install_requires = ['protobuf >= 3']
if sys.version_info < (3, 4):
    install_requires.append('enum34 >= 0.9')

setup(
    name='krpc',
    version=re.search(r'\'(.+)\'', open(os.path.join(dirpath, 'krpc/version.py')).read()).group(1),
    author='djungelorm',
    author_email='djungelorm@users.noreply.github.com',
    packages=['krpc', 'krpc.schema', 'krpc.test', 'krpc.test.schema'],
    url='https://krpc.github.io/krpc',
    license='GNU LGPL v3',
    description='Client library for kRPC, a Remote Procedure Call server for Kerbal Space Program',
    long_description=open(os.path.join(dirpath, 'README.txt')).read(),
    install_requires=install_requires,
    test_suite='krpc.test',
    use_2to3=True,
    classifiers=[
        'Development Status :: 5 - Production/Stable',
        'Intended Audience :: End Users/Desktop',
        'License :: OSI Approved :: GNU Lesser General Public License v3 (LGPLv3)',
        'Natural Language :: English',
        'Programming Language :: Python :: 2.7',
        'Programming Language :: Python :: 3',
        'Operating System :: Microsoft :: Windows',
        'Operating System :: Unix',
        'Topic :: Communications',
        'Topic :: Games/Entertainment :: Simulation',
        'Topic :: Internet'
    ],
)
