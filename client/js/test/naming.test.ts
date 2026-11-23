// The names a service and its types take in the client
import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { Connection } from "../dist/connection.js";
import { CONNECTION_MEMBERS, serviceName, typeName } from "../dist/naming.js";

describe("naming", () => {
  it("lists the members of a connection", () => {
    const members = new Set<string>();
    for (let p: object | null = Connection.prototype; p !== null; p = Object.getPrototypeOf(p)) {
      for (const name of Object.getOwnPropertyNames(p)) {
        if (!name.startsWith("_")) {
          members.add(name);
        }
      }
    }
    assert.deepEqual([...members].sort(), [...CONNECTION_MEMBERS].sort());
  });

  it("escapes a service named as a member of a connection", () => {
    assert.equal(serviceName("Close"), "close_");
    assert.equal(serviceName("SpaceCenter"), "spaceCenter");
  });

  it("escapes a type named as a declaration of the module", () => {
    assert.equal(typeName("Service"), "Service_");
    assert.equal(typeName("Vessel"), "Vessel");
  });
});
