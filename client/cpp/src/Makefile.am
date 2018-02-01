#TODO should not need -Iinclude/krpc
AM_CPPFLAGS = -I$(top_srcdir)/include -I$(top_srcdir)/include/krpc
lib_LTLIBRARIES = libkrpc.la
libkrpc_la_SOURCES = client.cpp connection.cpp decoder.cpp encoder.cpp event.cpp krpc.cpp krpc.pb.cpp platform.cpp stream_impl.cpp stream_manager.cpp
libkrpc_la_LDFLAGS = -release $(LT_RELEASE)
