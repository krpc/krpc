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

#endif  // KRPC_DEPRECATED_H
