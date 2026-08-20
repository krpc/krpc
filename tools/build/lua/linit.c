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

#include "lua.h"

#include "lauxlib.h"
#include "lualib.h"

int luaopen_lfs(lua_State *L);
int luaopen_mime_core(lua_State *L);
int luaopen_protobuf_pb(lua_State *L);
int luaopen_socket_core(lua_State *L);
int luaopen_socket_unix(lua_State *L);

static const luaL_Reg lualibs[] = {
  {"", luaopen_base},
  {LUA_LOADLIBNAME, luaopen_package},
  {LUA_TABLIBNAME, luaopen_table},
  {LUA_IOLIBNAME, luaopen_io},
  {LUA_OSLIBNAME, luaopen_os},
  {LUA_STRLIBNAME, luaopen_string},
  {LUA_MATHLIBNAME, luaopen_math},
  {LUA_DBLIBNAME, luaopen_debug},
  {NULL, NULL}
};

static const luaL_Reg preloadedlibs[] = {
  {"lfs", luaopen_lfs},
  {"mime.core", luaopen_mime_core},
  {"protobuf.pb", luaopen_protobuf_pb},
  {"socket.core", luaopen_socket_core},
  {"socket.unix", luaopen_socket_unix},
  {NULL, NULL}
};

LUALIB_API void luaL_openlibs (lua_State *L) {
  const luaL_Reg *lib;
  for (lib = lualibs; lib->func; lib++) {
    lua_pushcfunction(L, lib->func);
    lua_pushstring(L, lib->name);
    lua_call(L, 1, 0);
  }
  /* package.preload, which luaopen_package has just created */
  lua_getfield(L, LUA_GLOBALSINDEX, LUA_LOADLIBNAME);
  lua_getfield(L, -1, "preload");
  for (lib = preloadedlibs; lib->func; lib++) {
    lua_pushcfunction(L, lib->func);
    lua_setfield(L, -2, lib->name);
  }
  lua_pop(L, 2);
}
