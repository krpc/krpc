# kRPC TestServer

This image contains the kRPC TestServer, which can be run directly using docker.

To run the server use:
```
docker run -t -i -p 50000:50000 ghcr.io/krpc/testserver
```

This runs the server listening on port 50000.

Additional flags can be passed by including them at the end of the docker run command.
For example, to pass the --debug flag run the following:
```
docker run -t -i -p 50000:50000 ghcr.io/krpc/testserver --debug
```

For help on additional options and to see other usage instructions, pass the `--help` flag:
```
docker run -t -i -p 50000:50000 ghcr.io/krpc/testserver --help
```

To use a port other than the default, run the following command replacing `PORT` with the port
of your choice:
```
docker run -t -i -p PORT:50000 ghcr.io/krpc/testserver
```
