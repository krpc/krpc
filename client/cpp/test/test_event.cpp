#include <gtest/gtest-message.h>
#include <gtest/gtest-test-part.h>

#include <type_traits>

#include "gtest/gtest.h"
#include "krpc/event.hpp"
#include "server_test.hpp"
#include "services/test_service.hpp"

class test_event : public server_test {};

static_assert(std::is_default_constructible_v<krpc::Event>);
static_assert(std::is_copy_assignable_v<krpc::Event>);

TEST_F(test_event, test_a_default_constructed_event_is_false_until_assigned) {
  krpc::Event e;
  ASSERT_FALSE(e);
  e = test_service.on_timer(10000);
  ASSERT_TRUE(e);
  e.remove();
}
