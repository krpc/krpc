#include <iostream>
#include <krpc.hpp>
#include <krpc/services/krpc.hpp>

int main() {
  krpc::Client conn = krpc::connect();
  krpc::services::KRPC krpc(&conn);
  krpc::schema::Status status = krpc.get_status();
  std::cout << "Data in = "
            << (status.bytes_read_rate()/1024.0) << " KB/s" << std::endl;
  std::cout << "Data out = "
            << (status.bytes_written_rate()/1024.0) << " KB/s" << std::endl;
}
