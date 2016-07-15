#include <gmock/gmock.h>
#include <gtest/gtest.h>

#include <krpc/platform.hpp>
#include <krpc/services/krpc.hpp>

#include "server_test.hpp"
#include "services/test_service.hpp"

class test_object: public server_test {
};

TEST_F(test_object, test_equality) {
  krpc::services::TestService::TestClass obj1 = test_service.create_test_object("jeb");
  krpc::services::TestService::TestClass obj1a = test_service.create_test_object("jeb");
  krpc::services::TestService::TestClass obj2 = test_service.create_test_object("bob");
  ASSERT_TRUE(obj1 == obj1);
  ASSERT_TRUE(obj1 == obj1a);
  ASSERT_FALSE(obj1 == obj2);
  ASSERT_TRUE(obj1 != obj2);
  ASSERT_TRUE(obj2 != obj1);
  ASSERT_FALSE(obj2 != obj2);
}

TEST_F(test_object, test_ordering) {
  krpc::services::TestService::TestClass obj1 = test_service.create_test_object("test_ordering_1");
  krpc::services::TestService::TestClass obj2 = test_service.create_test_object("test_ordering_2");
  ASSERT_TRUE(obj1 < obj2);
  ASSERT_FALSE(obj2 < obj1);
  ASSERT_TRUE(obj2 > obj1);
  ASSERT_FALSE(obj1 > obj2);
  ASSERT_TRUE(obj1 <= obj2);
  ASSERT_TRUE(obj1 <= obj1);
  ASSERT_FALSE(obj2 <= obj1);
  ASSERT_TRUE(obj2 >= obj1);
  ASSERT_TRUE(obj1 >= obj1);
  ASSERT_FALSE(obj1 >= obj2);
}
