## core

- Read and write each client's connection on threads of its own, so a client that stops
  reading cannot freeze the game (#1376)
- End the connection of a client that leaves more than 16 MB of messages unread (#1376)
- Read up to 16 requests ahead of the one running, and hold a client that sends faster than
  the server runs its calls (#1376)
- Read from a serial port as bytes arrive, so a client over serial gets several calls per
  update (#1376)

## server

- Hide the server windows while the game hides its UI, such as on a loading screen (#1376)
- Fix errors logged on every scene change, and on quitting, when the server fails its
  startup checks (#1376)
