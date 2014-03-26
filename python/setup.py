from distutils.core import setup

setup(
    name='krpc',
    version='0.1.0',
    description='Remote Procedure Call server for Kerbal Space Program',
    author='djungelorm',
    author_email='djungelorm@users.noreply.github.com',
    url='https://github.com/djungelorm/krpc',
    packages=['krpc', 'krpc.schema'],
    requires=['protobuf'],
    classifiers=[
        'Development Status :: 3 - Alpha',
        'Intended Audience :: End Users/Desktop',
        'License :: OSI Approved :: MIT License',
        'Natural Language :: English',
        'Programming Language :: Python',
        'Operating System :: Microsoft :: Windows',
        'Operating System :: Unix',
        'Topic :: Communications',
        'Topic :: Games/Entertainment :: Simulation',
        'Topic :: Internet'
    ]
)
