serviceincludedir=$(pkgincludedir)/services
include_HEADERS = krpc.hpp
pkginclude_HEADERS = krpc/client.hpp krpc/connection.hpp krpc/decoder.hpp krpc/encoder.hpp krpc/error.hpp krpc/event.hpp krpc/object.hpp krpc/platform.hpp krpc/service.hpp krpc/stream.hpp krpc/stream_impl.hpp krpc/stream_manager.hpp krpc/krpc.pb.hpp
serviceinclude_HEADERS = krpc/services/drawing.hpp krpc/services/infernal_robotics.hpp krpc/services/kerbal_alarm_clock.hpp krpc/services/krpc.hpp krpc/services/remote_tech.hpp krpc/services/space_center.hpp krpc/services/ui.hpp

if HAVE_PROTOC
krpc/krpc.pb.hpp:
	${MKDIR} -p krpc
	$(PROTOC) --proto_path=${top_srcdir}/protobuf --cpp_out=krpc ${top_srcdir}/protobuf/krpc.proto
	$(RM) krpc/krpc.pb.cc
	$(MV) krpc/krpc.pb.h krpc/krpc.pb.hpp
else
krpc/krpc.pb.hpp:
	${MKDIR} -p krpc
	$(CP) ${top_srcdir}/protobuf/krpc.pb.hpp krpc/krpc.pb.hpp
endif
