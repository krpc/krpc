from setuptools import setup
import sys

if sys.version_info.major != 2:
    raise Exception("kRPC only works with python version 2.x")

setup(
    name='krpc',
    version='0.1.6',
    author='djungelorm',
    author_email='djungelorm@users.noreply.github.com',
    packages=['krpc','krpc.schema','krpc.test'],
    url='https://djungelorm.github.io/krpc/docs',
    license='GNU GPL v3',
    description='Client library for kRPC, a Remote Procedure Call server for Kerbal Space Program',
    long_description=open('README.txt').read(),
    install_requires=[
        'protobuf >= 2.4.1'
    ],
    test_suite='krpc.test',
    classifiers=[
        'Development Status :: 3 - Alpha',
        'Intended Audience :: End Users/Desktop',
        'License :: OSI Approved :: GNU General Public License v3 (GPLv3)',
        'Natural Language :: English',
        'Programming Language :: Python',
        'Operating System :: Microsoft :: Windows',
        'Operating System :: Unix',
        'Topic :: Communications',
        'Topic :: Games/Entertainment :: Simulation',
        'Topic :: Internet'
    ],
)
