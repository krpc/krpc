import krpc

conn = krpc.connect(name="Decouple")
ksa = conn.ksa
vehicle = ksa.active_vehicle
# [docs]
decouplers = [part for part in vehicle.parts if part.decoupler is not None]
lowest = min(decouplers, key=lambda part: part.sequence)
engine = next(part for part in vehicle.parts if part.title == "LR91 Sea")
separated = lowest.decoupler.decouple()
print(f"{vehicle.name} has {len(vehicle.parts)} parts")
print(f"The LR91 Sea is now part of {engine.vehicle.name}")
print(f"Separated: {', '.join(other.name for other in separated)}")
# [/docs]
