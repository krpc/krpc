#ifndef HEADER_KRPC_SERVICES_KRPC
#define HEADER_KRPC_SERVICES_KRPC

#include <krpc/services.hpp>
#include <krpc/encoder.hpp>
#include <krpc/decoder.hpp>

namespace krpc {
  namespace services{

class KRPC : public Service {
    public:
      KRPC(Client& client);
      schema::Status get_status();
      schema::Services get_services();
      unsigned int add_stream(schema::Request request);
      void remove_stream(unsigned int id);
    };

    inline KRPC::KRPC(Client& client):
      client(client) {}

    inline schema::Status KRPC::get_status() {
      schema::Status result;
      result.ParseFromString(client.invoke("KRPC", "GetStatus"));
      return result;
    }

    inline schema::Services KRPC::get_services() {
      schema::Services result;
      result.ParseFromString(client.invoke("KRPC", "GetServices"));
      return result;
    }

    inline unsigned int KRPC::add_stream(schema::Request request) {
      return 0;
    }

    inline void KRPC::remove_stream(unsigned int id) {
    }

  }
}

#endif
