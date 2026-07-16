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
using System.Reflection;
using UnityEngine;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class StockAerodynamics
    {
        // Cached once: ModuleControlSurface.FixedUpdate reads these protected fields
        // when splitting force between fixed and moving areas (issue #622).
        static readonly FieldInfo controlSurfaceDeflectionField =
            typeof(ModuleControlSurface).GetField(
                "deflection", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo liftingSurfaceBaseTransformField =
            typeof(ModuleLiftingSurface).GetField(
                "baseTransform", BindingFlags.Instance | BindingFlags.NonPublic);
        static bool loggedControlSurfaceReflectionFailure;

        /// <summary>
        /// Stock atmospheric state at a hypothetical body-relative position and UT.
        /// Pressure is in KSP's native kilopascals; the other quantities are SI.
        /// </summary>
        internal struct AtmosphericState
        {
            public double Altitude;
            public double Pressure;
            public double Temperature;
            public double Density;
            public double SpeedOfSound;
        }

        /// <summary>
        /// Evaluate the KSP 1.12.5 FlightIntegrator atmospheric-state chain once.
        /// The supplied world position provides the hypothetical body-relative radial
        /// direction; UT is used only for the body/Sun ephemeris and seasonal curves.
        /// </summary>
        internal static AtmosphericState GetAtmosphericState(
            Vector3d position, CelestialBody body, double ut)
        {
            var relativePosition = position - body.position;
            var altitude = relativePosition.magnitude - body.Radius;
            if (!body.atmosphere || altitude >= body.atmosphereDepth) {
                return new AtmosphericState {
                    Altitude = altitude,
                    Temperature = PhysicsGlobals.SpaceTemperature
                };
            }

            var pressure = body.GetPressure(altitude);
            if (pressure <= 0d) {
                return new AtmosphericState {
                    Altitude = altitude,
                    Temperature = PhysicsGlobals.SpaceTemperature
                };
            }

            var up = (Vector3)relativePosition.normalized;
            var bodyAxis = body.bodyTransform.up;
            var sun = Planetarium.fetch.Sun;
            var sunDirectionAtUT = (sun.getTruePositionAtUT(ut)
                                    - body.getTruePositionAtUT(ut)).normalized;
            var sunDirection = (Vector3)sunDirectionAtUT;

            // CelestialBody.GetAtmoThermalStats calculates the latitude and the
            // normalization bounds from these two polar angles. Clamp the dot
            // products only against round-off at the acos domain boundary.
            var bodyAxisDot = Math.Max(-1d, Math.Min(1d,
                (double)Vector3.Dot(bodyAxis, up)));
            var sunAxisDot = Math.Max(-1d, Math.Min(1d,
                (double)Vector3.Dot(sunDirection, bodyAxis)));
            var bodyPolarAngle = Math.Acos(bodyAxisDot);
            var sunPolarAngle = Math.Acos(sunAxisDot);
            var maximumSunDot = (1d + Math.Cos(
                sunPolarAngle - bodyPolarAngle)) * 0.5d;
            var minimumSunDot = (1d + Math.Cos(
                sunPolarAngle + bodyPolarAngle)) * 0.5d;

            var phaseRotation = Quaternion.AngleAxis(
                45f * Mathf.Sign((float)body.rotationPeriod), bodyAxis);
            var correctedSunDot = (1d + Vector3.Dot(
                sunDirection, phaseRotation * up)) * 0.5d;
            var sunDotRange = maximumSunDot - minimumSunDot;
            var normalizedSunlight = sunDotRange > 0.001d
                ? (correctedSunDot - minimumSunDot) / sunDotRange
                : minimumSunDot + sunDotRange * 0.5d;

            var foldedPolarAngle = bodyPolarAngle;
            if (foldedPolarAngle > Math.PI / 2d)
                foldedPolarAngle = Math.PI - foldedPolarAngle;
            var latitude = (float)((Math.PI / 2d - foldedPolarAngle)
                                   * 57.29578d);

            // The axial and eccentricity terms use the body in this body's
            // hierarchy that directly orbits the stock system's single Sun.
            var bodyReferencingSun = global::CelestialBody.GetBodyReferencing(
                body, sun);
            var axialPhase = 0f;
            var eccentricityOffset = 0d;
            if (bodyReferencingSun != null && bodyReferencingSun.orbit != null) {
                var orbit = bodyReferencingSun.orbit;
                axialPhase = (float)((orbit.TrueAnomalyAtUT(ut) * 57.29578d
                                      + 360d) % 360d);
                if (orbit.eccentricity != 0d) {
                    var radiusAtUT = orbit.getRelativePositionAtUT(ut).magnitude;
                    eccentricityOffset = body.eccentricityTemperatureBiasCurve.Evaluate(
                        (float)((radiusAtUT - orbit.PeR) / (orbit.ApR - orbit.PeR)));
                }
            }

            var temperatureOffset =
                (double)body.latitudeTemperatureBiasCurve.Evaluate(latitude)
                + (double)body.latitudeTemperatureSunMultCurve.Evaluate(latitude)
                    * normalizedSunlight
                + (double)body.axialTemperatureSunBiasCurve.Evaluate(axialPhase)
                    * body.axialTemperatureSunMultCurve.Evaluate(latitude)
                + eccentricityOffset;
            var temperature = body.GetTemperature(altitude)
                + body.atmosphereTemperatureSunMultCurve.Evaluate((float)altitude)
                    * temperatureOffset;
            var density = body.GetDensity(pressure, temperature);
            var speedOfSound = body.GetSpeedOfSound(pressure, density);
            return new AtmosphericState {
                Altitude = altitude,
                Pressure = pressure,
                Temperature = temperature,
                Density = density,
                SpeedOfSound = speedOfSound
            };
        }

        /// <summary>
        /// Gets the exact stock atmospheric temperature at the current UT.
        /// </summary>
        public static double GetTemperature(Vector3d position, CelestialBody body)
        {
            return GetAtmosphericState(
                position, body, Planetarium.GetUniversalTime()).Temperature;
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

        /// <summary>
        /// A complete aerodynamic force/torque result in SI units and world-space
        /// orientation. Force is in newtons and torque is in newton-meters about the
        /// hypothetical vessel center of mass.
        /// </summary>
        internal struct AerodynamicWrench
        {
            public Vector3d Force;
            public Vector3d Torque;
        }

        /// <summary>
        /// Compatibility wrapper for the legacy force endpoint. It intentionally gives
        /// every part the same center-of-mass airflow and ignores rotational part flow,
        /// preserving SimulateAerodynamicForceAt's established behavior.
        /// </summary>
        public static Vector3 SimAeroForce(
            CelestialBody body, Vessel vessel, Vector3d worldVelocity,
            Vector3d worldCoM, QuaternionD attitudeDelta)
        {
            var wrench = SimAeroWrench(
                body, vessel, worldVelocity, Vector3d.zero, worldCoM,
                attitudeDelta, Planetarium.GetUniversalTime(), false, true);
            return (Vector3)wrench.Force;
        }

        /// <summary>
        /// Compatibility wrapper for the torque endpoint. Returns the torque component
        /// of the full rate-aware aerodynamic wrench.
        /// </summary>
        public static Vector3 SimAeroTorque(
            CelestialBody body, Vessel vessel, Vector3d worldVelocity,
            Vector3d worldAngularVelocity, Vector3d worldCoM,
            QuaternionD attitudeDelta)
        {
            var wrench = SimAeroWrench(
                body, vessel, worldVelocity, worldAngularVelocity, worldCoM,
                attitudeDelta, Planetarium.GetUniversalTime(), true, false);
            return (Vector3)wrench.Torque;
        }

        /// <summary>
        /// Simulate the instantaneous rigid-body aerodynamic wrench. The input state is
        /// the hypothetical vessel center-of-mass state in world space. Current part
        /// geometry is rotated by attitudeDelta into that hypothetical state. Each part
        /// force is evaluated once, accumulated into the net force, and levered about the
        /// hypothetical center of mass; rigidbody angular drag is added to the torque.
        /// The only kN/kN-m to SI conversion occurs when the completed wrench is returned.
        /// </summary>
        internal static AerodynamicWrench SimAeroWrench(
            CelestialBody body, Vessel vessel, Vector3d worldVelocity,
            Vector3d worldAngularVelocity, Vector3d worldCoM,
            QuaternionD attitudeDelta, double ut, bool includeRotationalPartFlow,
            bool useLegacyUniformFlow)
        {
            var atmosphere = GetAtmosphericState(worldCoM, body, ut);
            var rho = atmosphere.Density;
            if (rho <= 0)
                return new AerodynamicWrench ();
            var soundSpeed = atmosphere.SpeedOfSound;

            var inverseDelta = attitudeDelta.Inverse ();
            var currentWorldCoM = (Vector3d)vessel.CoM;
            var angularVelocityCurrent = (Vector3)(inverseDelta * worldAngularVelocity);
            var uniformAirflowCurrent = (Vector3)(inverseDelta
                * (worldVelocity - body.getRFrmVel(worldCoM)));
            var totalForce = Vector3d.zero;
            var totalTorque = Vector3d.zero;

            for (int i = 0; i < vessel.Parts.Count; ++i) {
                var part = vessel.Parts[i];
                if (part.ShieldedFromAirstream || part.Rigidbody == null)
                    continue;

                var currentPartOffset =
                    (Vector3d)part.Rigidbody.worldCenterOfMass - currentWorldCoM;
                var hypotheticalPartOffset = attitudeDelta * currentPartOffset;
                var hypotheticalPartPosition = worldCoM + hypotheticalPartOffset;

                Vector3 partAirflowCurrent;
                if (useLegacyUniformFlow) {
                    partAirflowCurrent = uniformAirflowCurrent;
                } else {
                    var hypotheticalPartVelocity = worldVelocity;
                    if (includeRotationalPartFlow)
                        hypotheticalPartVelocity += Vector3d.Cross(
                            worldAngularVelocity, hypotheticalPartOffset);
                    var partAirflowWorld = hypotheticalPartVelocity
                                           - body.getRFrmVel(hypotheticalPartPosition);
                    partAirflowCurrent = (Vector3)(inverseDelta * partAirflowWorld);
                }

                var partForce = SimPartAeroForce(
                    part, partAirflowCurrent, rho, soundSpeed);
                var liftWorld = attitudeDelta * partForce.Lift;
                var dragWorld = attitudeDelta * partForce.Drag;
                totalForce += liftWorld + dragWorld;

                // FlightIntegrator and ModuleLiftingSurface apply lift at CoL and
                // drag at CoP. Rotate those current COM-relative offsets into the
                // hypothetical attitude before taking their moments.
                var liftOffset = attitudeDelta * (
                    (Vector3d)part.partTransform.TransformPoint(part.CoLOffset)
                    - currentWorldCoM);
                var dragOffset = attitudeDelta * (
                    (Vector3d)part.partTransform.TransformPoint(part.CoPOffset)
                    - currentWorldCoM);
                totalTorque += Vector3d.Cross(liftOffset, liftWorld);
                totalTorque += Vector3d.Cross(dragOffset, dragWorld);

                // Unity angular drag acts on the part's physical rigidbody inertia.
                // Evaluate it in the current geometry, then rotate the torque into
                // the hypothetical attitude with the rest of the wrench.
                if (part.angularDragByFI && part.rb != null) {
                    var qAtm = 0.0005 * rho * (double)partAirflowCurrent.sqrMagnitude
                               * 0.009869232667160128;
                    var angularDrag = part.angularDrag * (float)qAtm
                                      * PhysicsGlobals.AngularDragMultiplier;
                    var angularDragTorque = angularDrag
                        * PartAngularMomentum(part.rb, angularVelocityCurrent);
                    totalTorque -= attitudeDelta * (Vector3d)angularDragTorque;
                }
            }

            return new AerodynamicWrench {
                Force = totalForce * 1000d,
                Torque = totalTorque * 1000d
            };
        }

        /// <summary>
        /// The angular momentum of a part's rigidbody if it were rotating at the given
        /// world-space angular velocity. Uses the rigidbody's own (Unity,
        /// collider-derived) inertia tensor, which is what the engine's angular drag
        /// acts on. KSP's Unity physics uses tonne/kilonewton units, so the inertia
        /// tensor is in tonne*m^2 and the result is in kilonewton-meter-seconds.
        /// </summary>
        public static Vector3 PartAngularMomentum(Rigidbody rb, Vector3 angularVelocity)
        {
            var inertiaFrame = rb.rotation * rb.inertiaTensorRotation;
            var omegaLocal = Quaternion.Inverse(inertiaFrame) * angularVelocity;
            var inertia = rb.inertiaTensor;
            return inertiaFrame * new Vector3(
                inertia.x * omegaLocal.x, inertia.y * omegaLocal.y, inertia.z * omegaLocal.z);
        }

        /// <summary>
        /// The aerodynamic lift and drag on a single part. Forces are in the internal
        /// (kilonewton-scale) units accumulated by SimAeroForce; the caller
        /// applies the final conversion to newtons.
        /// </summary>
        internal struct PartAeroForce
        {
            public Vector3d Lift;
            public Vector3d Drag;
        }

        /// <summary>
        /// Whether a body-lift provider would be lifting in the hypothetical flow.
        /// ModuleLiftingSurface.IsLifting reads the module's live liftScalar, so using
        /// it directly makes hypothetical pod body lift depend on whether the real
        /// attached heatshield is currently on the pad, in flight, or in vacuum.
        /// </summary>
        static bool IsLiftProviderActiveAt(
            ILiftProvider provider, Vector3 velocity, float mach)
        {
            var wing = provider as ModuleLiftingSurface;
            if (wing == null)
                return provider.IsLifting;
            if (wing.part == null || wing.part.ShieldedFromAirstream
                || wing.part.Rigidbody == null)
                return false;
            if (wing.nodeEnabled && !string.IsNullOrEmpty(wing.attachNodeName)) {
                var node = wing.part.FindAttachNode(wing.attachNodeName);
                if (node != null && node.attachedPart != null)
                    return false;
            }

            Vector3 nVel, liftVector;
            float liftDot, absDot;
            wing.SetupCoefficients(
                velocity, out nVel, out liftVector, out liftDot, out absDot);
            var liftScalar = Mathf.Sign(liftDot) * wing.liftCurve.Evaluate(absDot)
                             * wing.liftMachCurve.Evaluate(mach)
                             * wing.deflectionLiftCoeff;
            return liftScalar != 0f && !float.IsNaN(liftScalar);
        }

        /// <summary>
        /// Simulate the aerodynamic lift and drag acting on a single part at the given
        /// air-relative velocity. Mirrors the per-part accumulation in KSP's
        /// FlightIntegrator.
        /// </summary>
        internal static PartAeroForce SimPartAeroForce(
            Part p, Vector3 v_wrld_vel, double rho, double soundSpeed)
        {
            var result = new PartAeroForce ();

            var dyn_pressure = 0.0005 * rho * v_wrld_vel.sqrMagnitude;
            var mach = (float)Math.Min (25.0, v_wrld_vel.magnitude / soundSpeed);
            // KSP's FlightIntegrator scales cube drag by a pseudo-Reynolds factor (density
            // times speed); omitting it overestimates subsonic low-altitude drag (see #911).
            var pseudoReDragMult = PhysicsGlobals.DragCurvePseudoReynolds.Evaluate(
                (float)(rho * v_wrld_vel.magnitude));

            var sim_dragVectorDir = v_wrld_vel.normalized;
            var sim_dragVectorDirLocal = -(p.transform.InverseTransformDirection(sim_dragVectorDir));

            var liftForce = Vector3.zero;
            float drag;

            switch (p.dragModel) {
                case Part.DragModel.DEFAULT:
                case Part.DragModel.CUBE:
                    var cubes = p.DragCubes;

                    var p_drag_data = new DragCubeList.CubeData();

                    if (cubes.None) { // since 1.0.5, some parts don't have drag cubes (for example fuel lines and struts)
                       drag = p.maximum_drag;
                    } else {
                        try {
                            cubes.AddSurfaceDragDirection(-sim_dragVectorDirLocal, mach, ref p_drag_data);
                        } catch (Exception) {
                            cubes.SetDrag(sim_dragVectorDirLocal, mach);
                            cubes.ForceUpdate(true, true);
                            cubes.AddSurfaceDragDirection(-sim_dragVectorDirLocal, mach, ref p_drag_data);
                        }

                        drag = p_drag_data.areaDrag * PhysicsGlobals.DragCubeMultiplier;

                        liftForce = p_drag_data.liftForce;
                    }
                    break;

                case Part.DragModel.SPHERICAL:
                    drag = p.maximum_drag;
                    break;

                case Part.DragModel.CYLINDRICAL:
                    drag = Mathf.Lerp(
                        p.minimum_drag, p.maximum_drag,
                        Mathf.Abs(Vector3.Dot(
                            p.partTransform.TransformDirection(p.dragReferenceVector),
                            sim_dragVectorDir)));
                    break;

                case Part.DragModel.CONIC:
                    drag = Mathf.Lerp(
                        p.minimum_drag, p.maximum_drag,
                        Vector3.Angle(
                            p.partTransform.TransformDirection(p.dragReferenceVector),
                            sim_dragVectorDir) / 180f);
                    break;

                default:
                    // no drag to apply
                    drag = 0f;
                    break;
            }

            // FlightIntegrator applies the same dynamic-pressure, pseudo-Reynolds,
            // and global drag scaling after every model-specific drag coefficient.
            // The legacy spherical/cylindrical/conic paths previously returned the
            // bare coefficient as a force, omitting this entire stock scaling chain.
            var sim_dragScalar = dyn_pressure * drag * PhysicsGlobals.DragMultiplier
                                 * pseudoReDragMult;
            var dragForce = -(Vector3d)sim_dragVectorDir * sim_dragScalar;
            result.Drag += dragForce;

            // If it isn't a wing or lifter, get body lift. Mirror the
            // FlightIntegrator's bodyLiftOnlyUnattachedLift gate: command pods get
            // NO cube body lift while their designated node (the bottom) is occupied
            // by a lift-providing part -- a heatshield, whose own CapsuleBottom
            // module supplies the capsule lift instead. Without this gate the sim
            // adds ~1 kN of phantom pod lift, shifting an asymmetric craft's
            // predicted trim by ~2 degrees (found by per-part sim-vs-live tracing
            // during #914 flight validation).
            var bodyLiftGated = p.bodyLiftOnlyUnattachedLiftActual
                                && p.bodyLiftOnlyProvider != null
                                && IsLiftProviderActiveAt(
                                    p.bodyLiftOnlyProvider, v_wrld_vel, mach);
            if (!p.hasLiftModule && !bodyLiftGated) {
                var simbodyLiftScalar = p.bodyLiftMultiplier * PhysicsGlobals.BodyLiftMultiplier * (float)dyn_pressure;
                simbodyLiftScalar *= PhysicsGlobals.GetLiftingSurfaceCurve("BodyLift").liftMachCurve.Evaluate(mach);
                var bodyLift = p.transform.rotation * (simbodyLiftScalar * liftForce);
                bodyLift = Vector3.ProjectOnPlane(bodyLift, sim_dragVectorDir);
                // Only accumulate forces for non-LiftModules
                result.Lift += bodyLift;
            }

            // Find ModuleLiftingSurface for wings and lift force. This also catches
            // control surfaces (a subclass) and the capsule body-lift module that
            // command pods carry (liftingSurfaceCurve = CapsuleBottom).
            for (int j = 0; j < p.Modules.Count; ++j) {
                var wing = p.Modules[j] as ModuleLiftingSurface;
                if (!wing)
                    continue;

                // Node gate, as GetLiftVector/GetDragVector apply it: a module tied
                // to an attach node (the pod's bottom node) is inert while that node
                // is occupied (e.g. a heatshield is attached).
                if (wing.nodeEnabled && !string.IsNullOrEmpty (wing.attachNodeName)) {
                    var node = p.FindAttachNode (wing.attachNodeName);
                    if (node != null && node.attachedPart != null)
                        continue;
                }

                var liftQ = dyn_pressure * 1000;
                Vector3 local_lift;
                Vector3 local_drag;
                // ModuleControlSurface / ModuleAeroSurface need KSP's fixed/moving
                // area split (issue #622). Plain ModuleLiftingSurface keeps the
                // undeflected path.
                var controlSurface = wing as ModuleControlSurface;
                if (controlSurface != null)
                    SimulateControlSurfaceForce (
                        controlSurface, v_wrld_vel, liftQ, mach,
                        out local_lift, out local_drag);
                else
                    SimulateLiftingSurfaceForce (
                        wing, v_wrld_vel, liftQ, mach,
                        out local_lift, out local_drag);
                result.Lift += local_lift;
                result.Drag += local_drag;
            }

            return result;
        }

        /// <summary>
        /// Replicate ModuleLiftingSurface.GetLiftVector against the given simulated
        /// airflow. Do NOT use wing.GetLiftVector here: its perpendicularOnly branch
        /// projects against the module's internal nVel FIELD, which is only written
        /// by the module's own FixedUpdate from the craft's REAL airflow (and is
        /// zero / stale on the pad or in vacuum). The SetupCoefficients
        /// out-parameter shadows that field, so for hypothetical states the
        /// projection uses arbitrary stale state and leaks a large mis-directed
        /// force (found via a bare Mk1 pod, whose CapsuleBottom lift reached
        /// ~5-24 kN unprojected).
        /// </summary>
        static Vector3 SimulateLiftVector (
            ModuleLiftingSurface wing, Vector3 nVel, Vector3 liftVector,
            float liftDot, float absDot, double liftQ, float mach)
        {
            var liftScalar = Mathf.Sign (liftDot) * wing.liftCurve.Evaluate (absDot)
                             * wing.liftMachCurve.Evaluate (mach)
                             * wing.deflectionLiftCoeff;
            if (liftScalar == 0f || float.IsNaN (liftScalar))
                return Vector3.zero;
            var lift = -liftVector
                * (float)(liftQ * (PhysicsGlobals.LiftMultiplier * liftScalar));
            if (wing.perpendicularOnly)
                lift = Vector3.ProjectOnPlane (lift, -nVel);
            return lift;
        }

        static void SimulateLiftingSurfaceForce (
            ModuleLiftingSurface wing, Vector3 velocity, double liftQ, float mach,
            out Vector3 lift, out Vector3 drag)
        {
            Vector3 nVel;
            Vector3 liftVector;
            float liftDot;
            float absDot;
            wing.SetupCoefficients (velocity, out nVel, out liftVector, out liftDot, out absDot);
            lift = SimulateLiftVector (wing, nVel, liftVector, liftDot, absDot, liftQ, mach);
            // Historical SimAeroForce behaviour: always evaluate internal drag
            // curves for plain lifting surfaces (mach passed explicitly).
            drag = wing.GetDragVector (nVel, absDot, liftQ, mach);
        }

        // F = F_neutral * (1 - A) + F_moving * A, matching ModuleControlSurface.FixedUpdate.
        // Reflection failure keeps this path with zero deflection so useInternalDragModel
        // is honored consistently (including for modded parts).
        static void SimulateControlSurfaceForce (
            ModuleControlSurface surface, Vector3 velocity, double liftQ, float mach,
            out Vector3 lift, out Vector3 drag)
        {
            float deflection;
            Transform baseTransform;
            GetControlSurfaceState (surface, out deflection, out baseTransform);

            Vector3 nVel;
            Vector3 liftVector;
            float liftDot;
            float absDot;
            surface.SetupCoefficients (velocity, out nVel, out liftVector, out liftDot, out absDot);

            var area = surface.ctrlSurfaceArea;
            var fixedArea = 1f - area;

            lift = SimulateLiftVector (
                surface, nVel, liftVector, liftDot, absDot, liftQ, mach) * fixedArea;
            drag = surface.useInternalDragModel
                ? surface.GetDragVector (nVel, absDot, liftQ, mach) * fixedArea
                : Vector3.zero;

            // Default moving contribution to the neutral coefficients. Only rotate the
            // lift vector when deflection and baseTransform are both available.
            var movingLiftVector = liftVector;
            var movingLiftDot = liftDot;
            var movingAbsDot = absDot;
            // Unity overloaded == treats destroyed objects as null.
            if (baseTransform != null && deflection != 0f) {
                var airflowIncidence = Quaternion.AngleAxis (
                    deflection, baseTransform.rotation * Vector3.right);
                movingLiftVector = airflowIncidence * liftVector;
                movingLiftDot = Vector3.Dot (nVel, movingLiftVector);
                movingAbsDot = Mathf.Abs (movingLiftDot);
            }

            lift += SimulateLiftVector (
                surface, nVel, movingLiftVector, movingLiftDot, movingAbsDot,
                liftQ, mach) * area;
            if (surface.useInternalDragModel)
                drag += surface.GetDragVector (nVel, movingAbsDot, liftQ, mach) * area;
        }

        static void GetControlSurfaceState (
            ModuleControlSurface surface, out float deflection, out Transform baseTransform)
        {
            deflection = 0f;
            baseTransform = null;
            if (controlSurfaceDeflectionField == null || liftingSurfaceBaseTransformField == null) {
                LogControlSurfaceReflectionFailureOnce ();
                return;
            }
            try {
                var deflectionValue = controlSurfaceDeflectionField.GetValue (surface);
                var transformValue = liftingSurfaceBaseTransformField.GetValue (surface);
                if (!(deflectionValue is float) || !(transformValue is Transform)) {
                    LogControlSurfaceReflectionFailureOnce ();
                    return;
                }
                var transform = (Transform)transformValue;
                // Unity overloaded == treats destroyed objects as null.
                if (transform == null) {
                    LogControlSurfaceReflectionFailureOnce ();
                    return;
                }
                deflection = (float)deflectionValue;
                baseTransform = transform;
            } catch (Exception) {
                LogControlSurfaceReflectionFailureOnce ();
            }
        }

        static void LogControlSurfaceReflectionFailureOnce ()
        {
            if (loggedControlSurfaceReflectionFailure)
                return;
            loggedControlSurfaceReflectionFailure = true;
            KRPC.Utils.Logger.WriteLine (
                "StockAerodynamics: failed to read ModuleControlSurface.deflection " +
                "or ModuleLiftingSurface.baseTransform; treating control surfaces as " +
                "undeflected (issue #622)",
                KRPC.Utils.Logger.Severity.Warning);
        }
    }
}
