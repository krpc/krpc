kRPC TestServer
===============

This image contains the kRPC TestServer, which can be run directly using docker.

To run the server use:
```
docker run -t -i -p 50000:50000 -p 50001:50001 krpc/testserver
```

This runs the server listening on RPC port 50000 and stream port 50001.

Additional flags can be passed by including them at the end of the docker run command. For example, to pass the --debug flag run the following:
```
docker run -t -i -p 50000:50000 -p 50001:50001 krpc/testserver --debug
```

For help on additional options and to see other usage instructions, pass the `--help` flag:
```
docker run -t -i -p 50000:50000 -p 50001:50001 krpc/testserver --help
```

To use an RPC and Stream port other than the defaults, runt the following command replacing `RPC_PORT` and `STREAM_PORT` with the ports of your choice:
```
docker run -t -i -p RPC_PORT:50000 -p STREAM_PORT:50001 krpc/testserver
```