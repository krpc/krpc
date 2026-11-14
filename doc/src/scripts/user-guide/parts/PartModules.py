import krpc

conn = krpc.connect()
vessel = conn.space_center.active_vessel

# [docs]
# Print the fields the right-click menu shows for each engine
for module in vessel.parts.modules_with_name("ModuleEngines"):
    for field in module.field_list:
        if field.visible:
            print(module.part.title, field.gui_name, field.value)

# Limit the thrust of each engine to half
for module in vessel.parts.modules_with_name("ModuleEngines"):
    for field in module.field_list:
        if field.name == "thrustPercentage":
            field.float_value = 50

# Extend the solar panels, as their Extend button does
for module in vessel.parts.modules_with_name("ModuleDeployableSolarPanel"):
    for event in module.event_list:
        if event.name == "Extend" and event.active:
            event.trigger()
# [/docs]
