from setuptools import setup
import sys

install_requires=['protobuf == 3.0.0-alpha-1']

if sys.version_info < (3, 4):
    install_requires.append('enum34 >= 1.0.4')

setup(
    name='krpc',
    version='0.1.10',
    author='djungelorm',
    author_email='djungelorm@users.noreply.github.com',
    packages=['krpc', 'krpc.schema', 'krpc.test'],
    url='https://djungelorm.github.io/krpc/docs',
    license='GNU GPL v3',
    description='Client library for kRPC, a Remote Procedure Call server for Kerbal Space Program',
    long_description=open('README.txt').read(),
    install_requires=install_requires,
    test_suite='krpc.test',
    use_2to3=True,
    classifiers=[
        'Development Status :: 3 - Alpha',
        'Intended Audience :: End Users/Desktop',
        'License :: OSI Approved :: GNU General Public License v3 (GPLv3)',
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
