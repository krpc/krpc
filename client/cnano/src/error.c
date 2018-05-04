#include <krpc_cnano/error.h>

#ifdef KRPC_ERROR_CHECK_FN
void (*krpc_error_handler)(krpc_error_t);
#endif

const char * krpc_get_error(krpc_error_t error) {
  switch (error) {
  case KRPC_ERROR_IO:                return "I/O failure";
  case KRPC_ERROR_EOF:               return "end of file";
  case KRPC_ERROR_CONNECTION_FAILED: return "connection failed";
  case KRPC_ERROR_NO_RESULTS:        return "no results";
  case KRPC_ERROR_RPC_FAILED:        return "rpc failed";
  case KRPC_ERROR_ENCODING_FAILED:   return "encoding failed";
  case KRPC_ERROR_DECODING_FAILED:   return "decoding failed";
  case KRPC_ERROR_BUFFER_TOO_SMALL:  return "buffer too small";
  default: return "";
  }
}
