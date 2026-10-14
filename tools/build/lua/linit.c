/*
** Initialization of libraries for lua.c, in place of the distribution's linit.c.
**
** The C modules the client needs are linked into this interpreter rather than loaded
** from shared objects, so there is nothing for require to find on disk. Registering
** them in package.preload puts them where require looks first, and each one then
** initialises exactly as it would have on being loaded: the entry points below are
** the distribution's own, untouched.
**
** The names are the ones require is asked for, dots included, because that is what
** the preload loader is keyed on. luaopen_socket_core is only how the dynamic loader
** spells a name it has to derive from a file, and no file is involved here.
*/

#define linit_c
#define LUA_LIB

#include "lprefix.h"

#include <stddef.h>

#include "lua.h"

#include "lualib.h"
#include "lauxlib.h"

int luaopen_lfs(lua_State *L);
int luaopen_mime_core(lua_State *L);
int luaopen_pb(lua_State *L);
int luaopen_pb_buffer(lua_State *L);
int luaopen_pb_slice(lua_State *L);
int luaopen_socket_core(lua_State *L);
int luaopen_socket_unix(lua_State *L);

static const luaL_Reg loadedlibs[] = {
  {LUA_GNAME, luaopen_base},
  {LUA_LOADLIBNAME, luaopen_package},
  {LUA_COLIBNAME, luaopen_coroutine},
  {LUA_TABLIBNAME, luaopen_table},
  {LUA_IOLIBNAME, luaopen_io},
  {LUA_OSLIBNAME, luaopen_os},
  {LUA_STRLIBNAME, luaopen_string},
  {LUA_MATHLIBNAME, luaopen_math},
  {LUA_UTF8LIBNAME, luaopen_utf8},
  {LUA_DBLIBNAME, luaopen_debug},
  {NULL, NULL}
};

static const luaL_Reg preloadedlibs[] = {
  {"lfs", luaopen_lfs},
  {"mime.core", luaopen_mime_core},
  {"pb", luaopen_pb},
  {"pb.buffer", luaopen_pb_buffer},
  {"pb.slice", luaopen_pb_slice},
  {"socket.core", luaopen_socket_core},
  {"socket.unix", luaopen_socket_unix},
  {NULL, NULL}
};

LUALIB_API void luaL_openlibs (lua_State *L) {
  const luaL_Reg *lib;
  for (lib = loadedlibs; lib->func; lib++) {
    luaL_requiref(L, lib->name, lib->func, 1);
    lua_pop(L, 1);
  }
  /* package.preload, which luaopen_package has just created */
  luaL_getsubtable(L, LUA_REGISTRYINDEX, LUA_PRELOAD_TABLE);
  for (lib = preloadedlibs; lib->func; lib++) {
    lua_pushcfunction(L, lib->func);
    lua_setfield(L, -2, lib->name);
  }
  lua_pop(L, 1);
}
