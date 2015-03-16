kRPC Documentation
==================

kRPC allows you to control Kerbal Space Program using remote procedure calls,
sent from an external script run outside of the game. It comes with a `Python
client library <https://pypi.python.org/pypi/krpc>`_. Client libraries for other
languages may be added in the future.

Its design is programming language and runtime agnostic. This means you can
interact with it from any programming language (as long as it can communicate
over a TCP connection) and you can run your program using any
compiler/interpreter/virtual machine you like.

.. toctree::
   :includehidden:
   :maxdepth: 2

   getting-started
   tutorials
   python-api
   compiling
   extending
   communication-protocol

:ref:`genindex`
