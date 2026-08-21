#pragma once

#include <cstddef>
#include <cstdint>
#include <functional>
#include <map>
#include <optional>
#include <set>
#include <string>
#include <tuple>
#include <type_traits>
#include <utility>
#include <vector>

namespace krpc {

template <typename T>
class Object;

/** Mix a hash into a running one. The constant and the shifts are the ones boost::hash_combine
    uses, which spread the bits of each value it is given across the result. */
inline void hash_combine(std::size_t* seed, std::size_t value) {
  *seed ^= value + 0x9e3779b9 + (*seed << 6) + (*seed >> 2);
}

/** The hash of a value of any type kRPC carries.

    The standard library hashes the arithmetic types, std::string and enumerations, but not
    std::tuple nor any of its containers, which is what a kRPC tuple and the collection types
    are. Specializing std::hash for those is not allowed: a program may only add a
    specialization of a standard template for a type of its own, and std::tuple<double, double>
    is not one. So the hash of everything the client carries is written here instead, and
    krpc::hash below is the function object to give a standard container that needs one.

    A service that defines a structure type is generated with an overload of its own, in the
    namespace enclosing the structure, which argument dependent lookup finds from the ones
    below in the same way as the encoder and decoder find theirs. The overloads are all
    declared before any is defined, so that a collection finds the hash of what it holds
    however the two are nested. */
template <typename T, typename std::enable_if<
                          std::is_arithmetic<T>::value || std::is_enum<T>::value, int>::type = 0>
std::size_t hash_value(const T& value);
std::size_t hash_value(const std::string& value);
template <typename T>
std::size_t hash_value(const Object<T>& object);
template <typename T>
std::size_t hash_value(const std::optional<T>& value);
template <typename T>
std::size_t hash_value(const std::vector<T>& list);
template <typename T>
std::size_t hash_value(const std::set<T>& set);
template <typename K, typename V>
std::size_t hash_value(const std::map<K, V>& dictionary);
template <typename... Ts>
std::size_t hash_value(const std::tuple<Ts...>& tuple);

template <typename T, typename std::enable_if<
                          std::is_arithmetic<T>::value || std::is_enum<T>::value, int>::type>
inline std::size_t hash_value(const T& value) {
  return std::hash<T>()(value);
}

inline std::size_t hash_value(const std::string& value) { return std::hash<std::string>()(value); }

/** An object is a handle to something on the server, and is equal to another handle to the
    same thing, so its identifier is what it hashes as. */
template <typename T>
inline std::size_t hash_value(const Object<T>& object) {
  return std::hash<uint64_t>()(object._id);
}

/** An optional holding a value hashes as if it were the one value it holds; an empty one
    hashes as an empty collection does. */
template <typename T>
inline std::size_t hash_value(const std::optional<T>& value) {
  std::size_t seed = 0;
  if (value) hash_combine(&seed, hash_value(*value));
  return seed;
}

template <typename T>
inline std::size_t hash_value(const std::vector<T>& list) {
  std::size_t seed = 0;
  for (typename std::vector<T>::const_iterator x = list.begin(); x != list.end(); ++x)
    hash_combine(&seed, hash_value(*x));
  return seed;
}

template <typename T>
inline std::size_t hash_value(const std::set<T>& set) {
  std::size_t seed = 0;
  for (typename std::set<T>::const_iterator x = set.begin(); x != set.end(); ++x)
    hash_combine(&seed, hash_value(*x));
  return seed;
}

template <typename K, typename V>
inline std::size_t hash_value(const std::map<K, V>& dictionary) {
  std::size_t seed = 0;
  for (typename std::map<K, V>::const_iterator x = dictionary.begin(); x != dictionary.end(); ++x) {
    hash_combine(&seed, hash_value(x->first));
    hash_combine(&seed, hash_value(x->second));
  }
  return seed;
}

template <typename... Ts>
inline std::size_t hash_value(const std::tuple<Ts...>& tuple) {
  std::size_t seed = 0;
  std::apply([&](const Ts&... args) { (hash_combine(&seed, hash_value(args)), ...); }, tuple);
  return seed;
}

/** Hashes a value of any type kRPC carries, for a standard container that takes the hash as a
    template argument:

        std::unordered_set<std::tuple<double, double, double>, krpc::hash> points;

    A structure type is also given a std::hash of its own, so a container of one needs nothing
    here. */
struct hash {
  template <typename T>
  std::size_t operator()(const T& value) const {
    return hash_value(value);
  }
};

}  // namespace krpc
