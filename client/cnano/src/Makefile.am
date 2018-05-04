#TODO should not need -Iinclude/krpc
AM_CPPFLAGS = -I$(top_srcdir)/include -I$(top_srcdir)/include/krpc_cnano
lib_LTLIBRARIES = libkrpc_cnano.la
libkrpc_cnano_la_SOURCES = communication.c decoder.c encoder.c error.c krpc.c memory.c utils.c pb_common.c pb_decode.c pb_encode.c krpc.pb.c
libkrpc_cnano_la_LDFLAGS = -release $(LT_RELEASE)
