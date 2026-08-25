#include <gtest/gtest.h>

#include <map>
#include <optional>
#include <set>
#include <string>
#include <tuple>
#include <unordered_map>
#include <unordered_set>
#include <vector>

#include "krpc/hash.hpp"

namespace {

TEST(test_hash, values) {
  ASSERT_EQ(krpc::hash_value(42), krpc::hash_value(42));
  ASSERT_EQ(krpc::hash_value(std::string("jeb")), krpc::hash_value(std::string("jeb")));
  ASSERT_NE(krpc::hash_value(std::string("jeb")), krpc::hash_value(std::string("bob")));
}

TEST(test_hash, tuple) {
  std::tuple<int32_t, std::string, bool> x(1, "jeb", false);
  std::tuple<int32_t, std::string, bool> y(1, "jeb", false);
  std::tuple<int32_t, std::string, bool> z(2, "jeb", false);
  ASSERT_EQ(krpc::hash_value(x), krpc::hash_value(y));
  ASSERT_NE(krpc::hash_value(x), krpc::hash_value(z));
}

TEST(test_hash, collections) {
  ASSERT_EQ(krpc::hash_value(std::vector<int32_t>{1, 2, 3}),
            krpc::hash_value(std::vector<int32_t>{1, 2, 3}));
  ASSERT_NE(krpc::hash_value(std::vector<int32_t>{1, 2, 3}),
            krpc::hash_value(std::vector<int32_t>{3, 2, 1}));
  ASSERT_EQ(krpc::hash_value(std::set<int32_t>{1, 2}), krpc::hash_value(std::set<int32_t>{2, 1}));
  ASSERT_EQ(krpc::hash_value(std::map<std::string, int32_t>{{"a", 1}}),
            krpc::hash_value(std::map<std::string, int32_t>{{"a", 1}}));
  ASSERT_NE(krpc::hash_value(std::map<std::string, int32_t>{{"a", 1}}),
            krpc::hash_value(std::map<std::string, int32_t>{{"a", 2}}));
}

TEST(test_hash, optional) {
  ASSERT_EQ(krpc::hash_value(std::optional<int32_t>(1)),
            krpc::hash_value(std::optional<int32_t>(1)));
  ASSERT_NE(krpc::hash_value(std::optional<int32_t>(1)),
            krpc::hash_value(std::optional<int32_t>(2)));
  ASSERT_NE(krpc::hash_value(std::optional<int32_t>(1)),
            krpc::hash_value(std::optional<int32_t>()));
  ASSERT_EQ(krpc::hash_value(std::optional<int32_t>()), krpc::hash_value(std::optional<int32_t>()));
}

TEST(test_hash, nested_collections) {
  std::vector<std::tuple<int32_t, std::string>> x{{1, "jeb"}, {2, "bob"}};
  std::vector<std::tuple<int32_t, std::string>> y{{1, "jeb"}, {2, "bob"}};
  ASSERT_EQ(krpc::hash_value(x), krpc::hash_value(y));
}

TEST(test_hash, as_a_container_hash) {
  // The use the hash is for: a standard container keyed by a type the standard library does not
  // hash on its own
  std::unordered_set<std::tuple<double, double, double>, krpc::hash> points;
  points.insert(std::make_tuple(1.0, 2.0, 3.0));
  points.insert(std::make_tuple(1.0, 2.0, 3.0));
  ASSERT_EQ(1u, points.size());
  ASSERT_EQ(1u, points.count(std::make_tuple(1.0, 2.0, 3.0)));
  ASSERT_EQ(0u, points.count(std::make_tuple(1.0, 2.0, 4.0)));

  std::unordered_map<std::vector<int32_t>, std::string, krpc::hash> names;
  names[std::vector<int32_t>{1, 2}] = "jeb";
  ASSERT_EQ("jeb", names[std::vector<int32_t>({1, 2})]);
}

}  // namespace
