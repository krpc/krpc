# kRPC for KSA Client Stubs

Generated code for the `KSA` service, for the kRPC clients that need it. Use them with the kRPC
client libraries version @CLIENT_VERSION@. Python and Lua build the service when they connect, and
need no stubs. JavaScript builds it too, and its stubs add the service's types and values.

## C#

Add `csharp/KSA.cs` to a project that references the `KRPC.Client` package:

```csharp
using KRPC.Client.Services.KSA;

var ksa = connection.KSA ();
Console.WriteLine (ksa.ActiveVehicle.Name);
```

## C++

Add `cpp/include` to the include path, beside the kRPC C++ client:

```cpp
#include <krpc.hpp>
#include <krpc/services/ksa.hpp>

krpc::services::KSA ksa(conn);
std::cout << ksa.active_vehicle()->name() << std::endl;
```

## C-nano

Add `cnano/include` to the include path, beside the kRPC C-nano client. Include `krpc_cnano.h`
first:

```c
#include <krpc_cnano.h>
#include <krpc_cnano/services/ksa.h>

krpc_KSA_Vehicle_t vehicle;
krpc_KSA_ActiveVehicle(conn, &vehicle);
```

## Java

Compile `java/krpc/client/services/KSA.java` with the kRPC Java client jar on the classpath:

```java
import krpc.client.services.KSA;

KSA ksa = KSA.newInstance(connection);
System.out.println(ksa.getActiveVehicle().getName());
```

## JavaScript

Copy `js/KSA.js` and `js/KSA.d.ts` beside your script, which uses the `@krpc-org/krpc` package.
Import `KSA.js` before you connect, and the connection has the service as `conn.ksa`. The client
connects to the server's WebSockets server, on port 50001:

```js
import { connect } from "@krpc-org/krpc";
import "./KSA.js";

const conn = await connect();
const vehicle = await conn.ksa.getActiveVehicle();
console.log(await vehicle.getName());
```
