import time

import krpc

conn = krpc.connect()
resources = conn.space_center.active_vessel.resources

# Compile the function once. Its remote procedure calls
# are made again each time the function runs
fuel_fraction = conn.compile(
    lambda: resources.amount("LiquidFuel") / resources.max("LiquidFuel")
)

# Run the compiled function as often as needed
while True:
    print("Fuel:", conn.run(fuel_fraction))
    time.sleep(1)
