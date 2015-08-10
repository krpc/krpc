#include <gtest/gtest.h>
#include <gmock/gmock.h>
#include <krpc/services/krpc.hpp>

#include "server_test.hpp"

class test_client: public server_test {
};

TEST_F(test_client, test_version) {
  krpc::services::KRPC krpc(conn);
  krpc::Status status = krpc.get_status();
  EXPECT_EQ("0.1.11", status.version());
}

TEST_F(test_client, test_get_services) {
  krpc::services::KRPC krpc(conn);
  krpc::Services services = krpc.get_services();
}
