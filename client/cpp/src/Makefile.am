AM_CPPFLAGS = -I$(top_srcdir)/include -I$(top_builddir)/include
lib_LTLIBRARIES = libkrpc.la
libkrpc_la_SOURCES = client.cpp connection.cpp decoder.cpp encoder.cpp event.cpp krpc.cpp platform.cpp stream_impl.cpp stream_manager.cpp krpc.pb.cpp
libkrpc_la_LDFLAGS = -release $(LT_RELEASE)

if HAVE_PROTOC
krpc.pb.cpp:
	$(PROTOC) --proto_path=${top_srcdir}/protobuf --cpp_out=. ${top_srcdir}/protobuf/krpc.proto
	$(SED) "s|krpc.pb.h|krpc/krpc.pb.hpp|" krpc.pb.cc > krpc.pb.cpp
	$(RM) krpc.pb.h krpc.pb.cc
else
krpc.pb.cpp:
	$(CP) ${top_srcdir}/protobuf/krpc.pb.cpp krpc.pb.cpp
endif
