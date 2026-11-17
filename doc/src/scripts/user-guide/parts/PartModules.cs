using System;
using KRPC.Client;
using KRPC.Client.Services.SpaceCenter;

class PartModules {
    public static void Main () {
        using (var connection = new Connection ()) {
            var vessel = connection.SpaceCenter ().ActiveVessel;
            // [docs]
            // Print the fields the right-click menu shows for each engine
            foreach (var module in vessel.Parts.ModulesWithName ("ModuleEngines"))
                foreach (var field in module.FieldList)
                    if (field.Visible)
                        Console.WriteLine ("{0} {1} {2}",
                            module.Part.Title, field.GuiName, field.Value);

            // Limit the thrust of each engine to half
            foreach (var module in vessel.Parts.ModulesWithName ("ModuleEngines"))
                foreach (var field in module.FieldList)
                    if (field.Name == "thrustPercentage")
                        field.FloatValue = 50;

            // Extend the solar panels, as their Extend button does
            var panels = vessel.Parts.ModulesWithName (
                "ModuleDeployableSolarPanel");
            foreach (var module in panels)
                foreach (var evnt in module.EventList)
                    if (evnt.Name == "Extend" && evnt.Active)
                        evnt.Trigger ();
            // [/docs]
        }
    }
}
