package = "kRPC"
version = "0.1.0-0"
source = {
  url = "https://github.com/djungelorm/krpc/releases/download/lua-client-v0.1.0/krpc-0.1.0-0.tar.gz"
}
description = {
  summary = "Client library for kRPC, a Remote Procedure Call server for Kerbal Space Program",
  homepage = "https://github.com/djungelorm/krpc/wiki",
  license = "GNU GPL v3"
}
dependencies = {
  "lua ~> 5.2"
}
build = {
  type = "builtin",
  modules = {
  }
}
