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

install_requires=['krpc', 'jinja2']
setup(
    name='krpctools',
    version=re.search(r'\'(.+)\'', open(os.path.join(dirpath, 'krpctools/version.py')).read()).group(1),
    author='djungelorm',
    author_email='djungelorm@users.noreply.github.com',
    url='https://krpc.github.io/krpc',
    license='GNU GPL v3',
    description='Development tools and scripts for kRPC.',
    long_description=open(os.path.join(dirpath, 'README.txt')).read(),
    packages=[
        'krpctools',
        'krpctools.clientgen',
        'krpctools.docgen',
        'krpctools.servicedefs'
    ],
    entry_points={
        'console_scripts': [
            'krpc-clientgen = krpctools.clientgen:main',
            'krpc-docgen = krpctools.docgen:main',
            'krpc-servicedefs = krpctools.servicedefs:main'
        ]
    },
    package_data={'': ['*.txt', '*.tmpl', 'bin/*.exe', 'bin/*.dll', 'bin/*.xml']},
    install_requires=install_requires,
    use_2to3=True,
    classifiers=[
        'Development Status :: 5 - Production/Stable',
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
    ]
)
