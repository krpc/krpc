#ifndef KRPC_DEPRECATED_H
#define KRPC_DEPRECATED_H

// Marks a generated declaration as deprecated, carrying an optional reason
// message. Expands to the compiler's deprecation attribute where available,
// and to nothing on compilers that do not support one.
#ifndef KRPC_DEPRECATED
#if defined(__GNUC__) || defined(__clang__)
#define KRPC_DEPRECATED(msg) __attribute__((deprecated(msg)))
#elif defined(_MSC_VER)
#define KRPC_DEPRECATED(msg) __declspec(deprecated(msg))
#else
#define KRPC_DEPRECATED(msg)
#endif
#endif

// Marks a generated enumeration value as deprecated. It follows the name of the value, a
// position that takes the compiler's attribute on GCC and Clang and a standard attribute
// in MSVC from C++17 on. It expands to nothing on other compilers.
#ifndef KRPC_DEPRECATED_VALUE
#if defined(__GNUC__) || defined(__clang__)
#define KRPC_DEPRECATED_VALUE(msg) __attribute__((deprecated(msg)))
#elif defined(_MSVC_LANG) && _MSVC_LANG >= 201703L
#define KRPC_DEPRECATED_VALUE(msg) [[deprecated(msg)]]
#else
#define KRPC_DEPRECATED_VALUE(msg)
#endif
#endif

// Generated code uses the deprecated declarations it makes, as a function's encoding reads each
// field of a structure. These keep those uses from warning. A use in a program still warns.
#if defined(__GNUC__) || defined(__clang__)
#define KRPC_DEPRECATED_USES_BEGIN \
  _Pragma("GCC diagnostic push") _Pragma("GCC diagnostic ignored \"-Wdeprecated-declarations\"")
#define KRPC_DEPRECATED_USES_END _Pragma("GCC diagnostic pop")
#elif defined(_MSC_VER)
#define KRPC_DEPRECATED_USES_BEGIN __pragma(warning(push)) __pragma(warning(disable : 4996))
#define KRPC_DEPRECATED_USES_END __pragma(warning(pop))
#else
#define KRPC_DEPRECATED_USES_BEGIN
#define KRPC_DEPRECATED_USES_END
#endif

#endif  // KRPC_DEPRECATED_H
