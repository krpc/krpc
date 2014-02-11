#!/bin/bash

PROTOGEN="../../Lib/protobuf-csharp-port-2.4.1.521-release-binaries/tools/ProtoGen.exe"

rm -f *.protobin *.cs

protoc RPC.proto -oRPC.protobin --include_imports
protoc Control.proto -oControl.protobin --include_imports
protoc Orbit.proto -oOrbit.protobin --include_imports

mono $PROTOGEN RPC.protobin -namespace=KRPC.Schema.RPC -umbrella_classname=RPC
mono $PROTOGEN Control.protobin -namespace=KRPC.Schema.Control -umbrella_classname=Control
mono $PROTOGEN Orbit.protobin -namespace=KRPC.Schema.Orbit -umbrella_classname=Orbit
