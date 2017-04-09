/* Code adapted from KSP Trajectories mod (https://github.com/neuoy/KSPTrajectories)
 *
 *
 * The MIT License (MIT)
 *
 * Copyright (c) 2014 Youen Toupin, aka neuoy
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 *
 * Except as contained in this notice, the name of the copyright holders shall not
 * be used in advertising or otherwise to promote the sale, use or other dealings
 * in this Software without prior written authorization from the copyright holders.
 */
using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class StockAerodynamics
    {
        /// <summary>
        /// This function should return exactly the same value as Vessel.atmDensity, but is more generic because you
        /// don't need an actual vessel updated by KSP to get a value at the desired location.
        /// Computations are performed for the current body position, which means it's theoritically wrong if you want
        /// to know the temperature in the future, but since body rotation is not used (position is given in sun frame),
        /// you should get accurate results up to a few weeks.
        /// </summary>
        public static double GetTemperature(Vector3d position, CelestialBody body)
        {
            if (!body.atmosphere)
                return PhysicsGlobals.SpaceTemperature;

            var altitude = (position - body.position).magnitude - body.Radius;
            if (altitude > body.atmosphereDepth)
                return PhysicsGlobals.SpaceTemperature;

            var up = (position - body.position).normalized;
            var polarAngle = Mathf.Acos(Vector3.Dot(body.bodyTransform.up, up));
            if (polarAngle > Mathf.PI / 2.0f)
                polarAngle = Mathf.PI - polarAngle;
            var time = (Mathf.PI / 2.0f - polarAngle) * 57.29578f;

            var sunVector = (FlightGlobals.Bodies[0].position - position).normalized;
            var sunAxialDot = Vector3.Dot(sunVector, body.bodyTransform.up);
            var bodyPolarAngle = Mathf.Acos(Vector3.Dot(body.bodyTransform.up, up));
            var sunPolarAngle = Mathf.Acos(sunAxialDot);
            var sunBodyMaxDot = (1.0f + Mathf.Cos(sunPolarAngle - bodyPolarAngle)) * 0.5f;
            var sunBodyMinDot = (1.0f + Mathf.Cos(sunPolarAngle + bodyPolarAngle)) * 0.5f;
            var sunDotCorrected = (1.0f + Vector3.Dot(sunVector, Quaternion.AngleAxis(45f * Mathf.Sign((float)body.rotationPeriod), body.bodyTransform.up) * up)) * 0.5f;
            var sunDotNormalized = (sunDotCorrected - sunBodyMinDot) / (sunBodyMaxDot - sunBodyMinDot);
            double atmosphereTemperatureOffset = body.latitudeTemperatureBiasCurve.Evaluate(time) + (double)body.latitudeTemperatureSunMultCurve.Evaluate(time) * sunDotNormalized + body.axialTemperatureSunMultCurve.Evaluate(sunAxialDot);
            return body.GetTemperature(altitude) + body.atmosphereTemperatureSunMultCurve.Evaluate((float)altitude) * atmosphereTemperatureOffset;
        }

        /// <summary>
        /// Gets the air density (rho) for the specified altitude (above sea level, in meters) on the specified body.
        /// This is an approximation, because actual calculations, taking sun exposure into account to compute air
        /// temperature, require to know the actual point on the body where the density is to be computed
        /// (knowing the altitude is not enough).
        /// However, the difference is small for high altitudes, so it makes very little difference
        /// for trajectory prediction.
        /// </summary>
        public static double GetDensity(double altitude, CelestialBody body)
        {
            if (!body.atmosphere)
                return 0;
            if (altitude > body.atmosphereDepth)
                return 0;
            var pressure = body.GetPressure(altitude);
            // get an average day/night temperature at the equator
            var sunDot = 0.5;
            var sunAxialDot = 0f;
            var atmosphereTemperatureOffset = body.latitudeTemperatureBiasCurve.Evaluate(0) + body.latitudeTemperatureSunMultCurve.Evaluate(0) * sunDot + body.axialTemperatureSunMultCurve.Evaluate(sunAxialDot);
            var temperature = body.GetTemperature(altitude) + body.atmosphereTemperatureSunMultCurve.Evaluate((float)altitude) * atmosphereTemperatureOffset;
            return body.GetDensity(pressure, temperature);
        }

        /// <summary>
        /// Gets the air pressure (in Pascals) for the specified altitude (above sea level, in meters) on the specified body.
        /// </summary>
        public static double GetPressure (double altitude, CelestialBody body)
        {
            if (!body.atmosphere)
                return 0;
            if (altitude > body.atmosphereDepth)
                return 0;
            return body.GetPressure (altitude) * 1000d;
        }

        public static Vector3 SimAeroForce(CelestialBody body, Vessel _vessel, Vector3 v_wrld_vel, Vector3 position)
        {
            //double latitude = body.GetLatitude(position) / 180.0 * Math.PI;
            var altitude = (position - body.position).magnitude - body.Radius;
            return SimAeroForce(body, _vessel, v_wrld_vel, altitude);
        }

        [SuppressMessage ("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        public static Vector3 SimAeroForce(CelestialBody body, Vessel _vessel, Vector3 v_wrld_vel, double altitude)
        {
            var pressure = body.GetPressure(altitude);
            // Lift and drag for force accumulation.
            var total_lift = Vector3d.zero;
            var total_drag = Vector3d.zero;

            // dynamic pressure for standard drag equation
            var rho = GetDensity(altitude, body);
            var dyn_pressure = 0.0005 * rho * v_wrld_vel.sqrMagnitude;

            if (rho <= 0)
                return Vector3.zero;

            var soundSpeed = body.GetSpeedOfSound(pressure, rho);
            var mach = v_wrld_vel.magnitude / soundSpeed;
            if (mach > 25.0)
                mach = 25.0;

            // Loop through all parts, accumulating drag and lift.
            for (int i = 0; i < _vessel.Parts.Count; ++i) {
                // need checks on shielded components
                var p = _vessel.Parts[i];
                //TrajectoriesDebug partDebug = VesselAerodynamicModel.DebugParts ? p.FindModuleImplementing<TrajectoriesDebug>() : null;
                //if (partDebug != null)
                //{
                //    partDebug.Drag = 0;
                //    partDebug.Lift = 0;
                //}

                if (p.ShieldedFromAirstream || p.Rigidbody == null)
                    continue;

                // Get Drag
                var sim_dragVectorDir = v_wrld_vel.normalized;
                var sim_dragVectorDirLocal = -(p.transform.InverseTransformDirection(sim_dragVectorDir));

                var liftForce = Vector3.zero;
                Vector3d dragForce;

                switch (p.dragModel) {
                    case Part.DragModel.DEFAULT:
                    case Part.DragModel.CUBE:
                        var cubes = p.DragCubes;

                        var p_drag_data = new DragCubeList.CubeData();

                        float drag;
                        if (cubes.None) { // since 1.0.5, some parts don't have drag cubes (for example fuel lines and struts)
                           drag = p.maximum_drag;
                        } else {
                            try {
                                cubes.AddSurfaceDragDirection(-sim_dragVectorDirLocal, (float)mach, ref p_drag_data);
                            } catch (Exception) {
                                cubes.SetDrag(sim_dragVectorDirLocal, (float)mach);
                                cubes.ForceUpdate(true, true);
                                cubes.AddSurfaceDragDirection(-sim_dragVectorDirLocal, (float)mach, ref p_drag_data);
                                //Debug.Log(String.Format("Trajectories: Caught NRE on Drag Initialization.  Should be fixed now.  {0}", e));
                            }

                            drag = p_drag_data.areaDrag * PhysicsGlobals.DragCubeMultiplier;

                            liftForce = p_drag_data.liftForce;
                        }

                        var sim_dragScalar = dyn_pressure * drag * PhysicsGlobals.DragMultiplier;
                        dragForce = -(Vector3d)sim_dragVectorDir * sim_dragScalar;
                        break;

                    case Part.DragModel.SPHERICAL:
                        dragForce = -(Vector3d)sim_dragVectorDir * p.maximum_drag;
                        break;

                    case Part.DragModel.CYLINDRICAL:
                        dragForce = -(Vector3d)sim_dragVectorDir * Mathf.Lerp(p.minimum_drag, p.maximum_drag, Mathf.Abs(Vector3.Dot(p.partTransform.TransformDirection(p.dragReferenceVector), sim_dragVectorDir)));
                        break;

                    case Part.DragModel.CONIC:
                        dragForce = -(Vector3d)sim_dragVectorDir * Mathf.Lerp(p.minimum_drag, p.maximum_drag, Vector3.Angle(p.partTransform.TransformDirection(p.dragReferenceVector), sim_dragVectorDir) / 180f);
                        break;

                    default:
                        // no drag to apply
                        dragForce = Vector3d.zero;
                        break;
                }

                //if (partDebug != null)
                //{
                //    partDebug.Drag += (float)dragForce.magnitude;
                //}
                total_drag += dragForce;

                // If it isn't a wing or lifter, get body lift.
                if (!p.hasLiftModule) {
                    var simbodyLiftScalar = p.bodyLiftMultiplier * PhysicsGlobals.BodyLiftMultiplier * (float)dyn_pressure;
                    simbodyLiftScalar *= PhysicsGlobals.GetLiftingSurfaceCurve("BodyLift").liftMachCurve.Evaluate((float)mach);
                    var bodyLift = p.transform.rotation * (simbodyLiftScalar * liftForce);
                    bodyLift = Vector3.ProjectOnPlane(bodyLift, sim_dragVectorDir);
                    // Only accumulate forces for non-LiftModules
                    total_lift += bodyLift;
                }

                // Find ModuleLifingSurface for wings and liftforce.
                // Should catch control surface as it is a subclass
                for (int j = 0; j < p.Modules.Count; ++j) {
                    var m = p.Modules[j];
                    float mcs_mod;
                    var wing = m as ModuleLiftingSurface;
                    if (wing) {
                        mcs_mod = 1.0f;
                        var liftQ = dyn_pressure * 1000;
                        var nVel = Vector3.zero;
                        var liftVector = Vector3.zero;
                        float liftdot;
                        float absdot;
                        wing.SetupCoefficients(v_wrld_vel, out nVel, out liftVector, out liftdot, out absdot);

                        var prevMach = p.machNumber;
                        p.machNumber = mach;
                        var local_lift = mcs_mod * wing.GetLiftVector(liftVector, liftdot, absdot, liftQ, (float)mach);
                        var local_drag = mcs_mod * wing.GetDragVector(nVel, absdot, liftQ);
                        p.machNumber = prevMach;

                        total_lift += local_lift;
                        total_drag += local_drag;

                        //if (partDebug != null) {
                        //    partDebug.Lift += (float)local_lift.magnitude;
                        //    partDebug.Drag += (float)local_drag.magnitude;
                        //}
                    }
                }
            }
            return (total_lift + total_drag) * 1000d;
        }
    }
}
