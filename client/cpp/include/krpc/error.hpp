#ifndef HEADER_KRPC_ERROR
#define HEADER_KRPC_ERROR

#include <boost/exception/all.hpp>

namespace krpc {

  typedef boost::error_info<struct tag_error_description, std::string> error_description;

  struct RPCError : virtual boost::exception, virtual std::exception {};

}

#endif
