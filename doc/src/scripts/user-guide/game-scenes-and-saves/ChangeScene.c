#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#define sleep_ms(ms) Sleep(ms)
#else
#define _POSIX_C_SOURCE 200809L
#include <time.h>
static void sleep_ms(long ms) {
  struct timespec duration = {ms / 1000, (ms % 1000) * 1000000};
  nanosleep(&duration, NULL);
}
#endif
#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Change scene") != KRPC_OK) return 1;

  // [docs]
  krpc_KRPC_set_GameScene(conn, KRPC_KRPC_GAMESCENE_SPACECENTER);
  krpc_KRPC_GameScene_t scene;
  while (krpc_KRPC_GameScene(conn, &scene) == KRPC_OK &&
         scene != KRPC_KRPC_GAMESCENE_SPACECENTER)
    sleep_ms(100);
  // [/docs]

  krpc_disconnect(conn);
}
