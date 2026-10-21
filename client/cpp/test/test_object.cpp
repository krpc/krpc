#include <gtest/gtest-message.h>
#include <gtest/gtest-test-part.h>

#include <stdexcept>

#include "gtest/gtest.h"
#include "krpc/object.hpp"
#include "krpc/services/krpc.hpp"
#include "server_test.hpp"
#include "services/test_service.hpp"

class test_object : public server_test {};

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

TEST_F(test_object, test_get_object_id) {
  krpc::services::TestService::TestClass obj(&conn, 42);
  ASSERT_EQ(42u, krpc::get_object_id(obj));
}

TEST_F(test_object, test_memory_allocation) {
  krpc::services::TestService::TestClass obj1 = test_service.create_test_object("jeb");
  krpc::services::TestService::TestClass obj1a = test_service.create_test_object("jeb");
  krpc::services::TestService::TestClass obj2 = test_service.create_test_object("bob");
  ASSERT_EQ(krpc::get_object_id(obj1), krpc::get_object_id(obj1a));
  ASSERT_NE(krpc::get_object_id(obj1), krpc::get_object_id(obj2));
}

TEST_F(test_object, test_get_object_id_null) {
  krpc::services::TestService::TestClass obj;
  ASSERT_THROW(krpc::get_object_id(obj), std::invalid_argument);
}
