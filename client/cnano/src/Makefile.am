#TODO should not need -Iinclude/krpc
AM_CPPFLAGS = -I$(top_srcdir)/include -I$(top_srcdir)/include/krpc
lib_LTLIBRARIES = libkrpc.la
libkrpc_la_SOURCES = communication.c decoder.c encoder.c error.c krpc.c memory.c utils.c pb_common.c pb_decode.c pb_encode.c
libkrpc_la_LDFLAGS = -release $(LT_RELEASE)
