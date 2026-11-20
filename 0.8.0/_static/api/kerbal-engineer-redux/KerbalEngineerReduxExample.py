import krpc

conn = krpc.connect(name="Kerbal Engineer Redux Example")
vessel = conn.space_center.active_vessel
ker = conn.kerbal_engineer_redux

for stage in reversed(ker.stages(vessel)):
    print(
        "Stage %d: %.0f m/s, TWR %.2f"
        % (stage.number, stage.delta_v, stage.available_twr)
    )

burn = ker.burn_time(vessel, 500)
if burn is None:
    print("Not enough delta-v for the burn")
else:
    print("A 500 m/s burn takes %.1f seconds" % burn.duration)
