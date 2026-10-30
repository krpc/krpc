#include <optional>
#include <type_traits>

#include "krpc/client.hpp"
#include "services/test_service.hpp"

// A client can be declared before it connects, and moved into a container
static_assert(std::is_default_constructible_v<krpc::Client>);
static_assert(std::is_move_constructible_v<krpc::Client>);

// The C++ type of a parameter carries whether it can hold null, so a null at a parameter
// that is not nullable is rejected by the compiler rather than by the server
static_assert(std::is_invocable_v<decltype(&krpc::services::TestService::echo_test_object),
                                  krpc::services::TestService&, std::nullopt_t>);
static_assert(!std::is_invocable_v<decltype(&krpc::services::TestService::not_nullable_object),
                                   krpc::services::TestService&, std::nullopt_t>);
