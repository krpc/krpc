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

        /// <summary>
        /// TEMPORARY (#914 investigation, remove before merge): evaluate the cube drag
        /// on the first physics part exactly as SimPartAeroForce does, tracing every
        /// intermediate term, alongside an independent manual recomputation of the same
        /// formula from the raw cube arrays and PhysicsGlobals curves. Any term where
        /// the in-game evaluation and the manual mirror disagree localizes the
        /// context-dependent divergence.
        /// </summary>
        public static string TraceAeroForce(CelestialBody body, Vessel _vessel, Vector3 v_wrld_vel, Vector3 position)
        {
            var sb = new System.Text.StringBuilder();
            var altitude = (position - body.position).magnitude - body.Radius;
            var rho = GetDensity(altitude, body);
            var soundSpeed = body.GetSpeedOfSound(body.GetPressure(altitude), rho);
            var dyn_pressure = 0.0005 * rho * v_wrld_vel.sqrMagnitude;
            var mach = (float)Math.Min(25.0, v_wrld_vel.magnitude / soundSpeed);
            var pseudoRe = PhysicsGlobals.DragCurvePseudoReynolds.Evaluate(
                (float)(rho * v_wrld_vel.magnitude));
            sb.Append("alt=").Append(altitude.ToString("G9"))
              .Append(" rho=").Append(rho.ToString("G9"))
              .Append(" soundSpeed=").Append(soundSpeed.ToString("G9"))
              .Append(" mach=").Append(mach.ToString("G9"))
              .Append(" q_kPa=").Append(dyn_pressure.ToString("G9"))
              .Append(" pseudoRe=").Append(pseudoRe.ToString("G9")).Append('\n');

            sb.Append("vessel parts=").Append(_vessel.Parts.Count).Append(": ");
            foreach (var part in _vessel.Parts) {
                sb.Append(part.partInfo.name)
                  .Append("(shielded=").Append(part.ShieldedFromAirstream)
                  .Append(",rb=").Append(part.Rigidbody != null)
                  .Append(",ownRb=").Append(part.rb != null)
                  .Append(",dragModel=").Append(part.dragModel);
                var pf = SimPartAeroForce(part, v_wrld_vel, rho, soundSpeed);
                sb.Append(",dragN=").Append(
                    (pf.Drag.magnitude * 1000.0).ToString("G9"));
                sb.Append(",liftN=").Append(
                    (pf.Lift.magnitude * 1000.0).ToString("G9"));
                sb.Append(") ");
            }
            sb.Append('\n');

            Part p = null;
            foreach (var part in _vessel.Parts)
                if (!part.ShieldedFromAirstream && part.Rigidbody != null) {
                    p = part;
                    break;
                }
            if (p == null)
                return sb.Append("no physics part").ToString();

            var dir = v_wrld_vel.normalized;
            var dirLocal = -(p.transform.InverseTransformDirection(dir));
            sb.Append("part=").Append(p.partInfo.name)
              .Append(" flowLocal=").Append((-dirLocal).ToString("G6")).Append('\n');

            var cubes = p.DragCubes;
            var data = new DragCubeList.CubeData();
            cubes.AddSurfaceDragDirection(-dirLocal, mach, ref data);
            sb.Append("game: areaDrag=").Append(data.areaDrag.ToString("G9"))
              .Append(" area=").Append(data.area.ToString("G9"))
              .Append(" dragCoeff=").Append(data.dragCoeff.ToString("G9"))
              .Append(" exposedArea=").Append(data.exposedArea.ToString("G9"))
              .Append(" liftForce=").Append(data.liftForce.ToString("G6")).Append('\n');

            var flags = System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.Instance;
            var type = typeof(DragCubeList);
            var areaOccluded = (float[])type.GetField("areaOccluded", flags).GetValue(cubes);
            var weightedDrag = (float[])type.GetField("weightedDrag", flags).GetValue(cubes);
            var faceField = type.GetField("faceDirections",
                flags | System.Reflection.BindingFlags.Static);
            var faces = faceField != null && faceField.IsStatic
                ? (Vector3[])faceField.GetValue(null)
                : new[] { Vector3.right, -Vector3.right, Vector3.up, -Vector3.up,
                          Vector3.forward, -Vector3.forward };
            sb.Append("faceDirections=");
            foreach (var f in faces)
                sb.Append(f.ToString("G4")).Append(';');
            sb.Append('\n');

            var flow = (Vector3)(-dirLocal);
            float manualAreaDrag = 0f;
            for (int i = 0; i < 6 && i < faces.Length; i++) {
                float dot = Vector3.Dot(flow, faces[i]);
                float dotN = (dot + 1f) * 0.5f;
                float curve = PhysicsGlobals.DragCurveValue(
                    PhysicsGlobals.SurfaceCurves, dotN, mach);
                float cd = weightedDrag[i];
                float cdEff = cd < 1f
                    ? Mathf.Pow(PhysicsGlobals.DragCurveCd.Evaluate(cd),
                                PhysicsGlobals.DragCurveCdPower.Evaluate(mach))
                    : cd;
                manualAreaDrag += areaOccluded[i] * curve * cdEff;
                sb.Append("face").Append(i)
                  .Append(" dot=").Append(dot.ToString("G6"))
                  .Append(" dotN=").Append(dotN.ToString("G6"))
                  .Append(" curve=").Append(curve.ToString("G9"))
                  .Append(" cd=").Append(cd.ToString("G6"))
                  .Append(" cdEff=").Append(cdEff.ToString("G9"))
                  .Append(" contrib=").Append(
                      (areaOccluded[i] * curve * cdEff).ToString("G9"))
                  .Append('\n');
            }
            sb.Append("manual: areaDrag=").Append(manualAreaDrag.ToString("G9"))
              .Append(" game/manual=").Append(
                  (data.areaDrag / manualAreaDrag).ToString("G9")).Append('\n');
            var dragN = dyn_pressure * data.areaDrag * PhysicsGlobals.DragCubeMultiplier
                        * PhysicsGlobals.DragMultiplier * pseudoRe * 1000.0;
            sb.Append("game dragN=").Append(dragN.ToString("G9")).Append('\n');

            // The full body-lift chain, mirroring SimPartAeroForce exactly. The raw
            // cube lift is large and nearly anti-parallel to the wind; the final
            // perpendicular lift is a small residue of ProjectOnPlane, so any error
            // in the scalar or the projection leaks into the along-wind component
            // with large amplification. Trace every link.
            var liftMachCurve = PhysicsGlobals.GetLiftingSurfaceCurve("BodyLift").liftMachCurve;
            var liftMach = liftMachCurve.Evaluate(mach);
            var scalar = p.bodyLiftMultiplier * PhysicsGlobals.BodyLiftMultiplier
                         * (float)dyn_pressure * liftMach;
            sb.Append("lift: p.bodyLiftMultiplier=").Append(p.bodyLiftMultiplier.ToString("G9"))
              .Append(" BodyLiftMultiplier=").Append(PhysicsGlobals.BodyLiftMultiplier.ToString("G9"))
              .Append(" liftMach=").Append(liftMach.ToString("G9"))
              .Append(" scalar=").Append(scalar.ToString("G9"))
              .Append(" hasLiftModule=").Append(p.hasLiftModule).Append('\n');
            var bodyLift = p.transform.rotation * (scalar * data.liftForce);
            sb.Append("lift world preProject=").Append(bodyLift.ToString("G6"))
              .Append(" |.|=").Append(bodyLift.magnitude.ToString("G9")).Append('\n');
            var projected = Vector3.ProjectOnPlane(bodyLift, dir);
            sb.Append("lift world postProject=").Append(projected.ToString("G6"))
              .Append(" |.|=").Append(projected.magnitude.ToString("G9"))
              .Append(" alongWindLeak=").Append(
                  Vector3.Dot(projected, dir).ToString("G9")).Append('\n');
            sb.Append("total: alongWind N=").Append(
                (dragN - 1000.0 * Vector3.Dot(projected, dir)).ToString("G9"))
              .Append(" perp N=").Append((1000f * projected.magnitude).ToString("G9"))
              .Append('\n');

            // Wing branch (ModuleLiftingSurface), mirroring SimPartAeroForce's calls
            // with every intermediate: for the Mk1 pod this is the CapsuleBottom
            // body-lift module. This is the identified extra contributor; trace its
            // geometry (liftDot sign gates it) and the exact vectors it returns.
            foreach (var moduleItem in p.Modules) {
                var wing = moduleItem as ModuleLiftingSurface;
                if (wing == null)
                    continue;
                var liftQ = dyn_pressure * 1000;
                Vector3 nVel, liftVector;
                float liftdot, absdot;
                wing.SetupCoefficients(v_wrld_vel, out nVel, out liftVector,
                                       out liftdot, out absdot);
                sb.Append("wing ").Append(wing.GetType().Name)
                  .Append(": nVel=").Append(nVel.ToString("G6"))
                  .Append(" liftVector=").Append(liftVector.ToString("G6"))
                  .Append(" liftDot=").Append(liftdot.ToString("G9"))
                  .Append(" absDot=").Append(absdot.ToString("G9"))
                  .Append(" liftQ=").Append(liftQ.ToString("G9")).Append('\n');
                var prevMach = p.machNumber;
                p.machNumber = mach;
                var wingLift = wing.GetLiftVector(liftVector, liftdot, absdot, liftQ, mach);
                var wingDrag = wing.GetDragVector(nVel, absdot, liftQ);
                p.machNumber = prevMach;
                sb.Append("wing lift=").Append(wingLift.ToString("G6"))
                  .Append(" |.|N=").Append((1000f * wingLift.magnitude).ToString("G9"))
                  .Append(" alongWind N=").Append(
                      (1000f * Vector3.Dot(wingLift, dir)).ToString("G9")).Append('\n');
                sb.Append("wing drag=").Append(wingDrag.ToString("G6"))
                  .Append(" |.|N=").Append((1000f * wingDrag.magnitude).ToString("G9"))
                  .Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>
        /// TEMPORARY (#914 investigation, remove before merge): per-part side-by-side
        /// of the SIMULATED aero forces/torque contributions (SimPartAeroForce at the
        /// vessel's CURRENT state) versus the LIVE game-applied fields (dragScalar,
        /// bodyLift, wing forces), including each part's actual airflow vector versus
        /// the rigid-body model's. Localizes which part and which component carries a
        /// sim-vs-live torque discrepancy.
        /// </summary>
        public static string TraceAeroTorquePerPart(CelestialBody body, Vessel _vessel)
        {
            var sb = new System.Text.StringBuilder();
            Vector3d worldCoM = _vessel.CoM;
            var vAir = (Vector3)_vessel.srf_velocity;
            var altitude = _vessel.altitude;
            var rho = GetDensity(altitude, body);
            if (rho <= 0)
                return "no airflow (rho <= 0)";
            var soundSpeed = body.GetSpeedOfSound(body.GetPressure(altitude), rho);
            var omegaWorld = (Vector3)(_vessel.ReferenceTransform.rotation
                                       * _vessel.angularVelocity);
            sb.Append("alt=").Append(altitude.ToString("G6"))
              .Append(" |vAir|=").Append(vAir.magnitude.ToString("G6"))
              .Append(" mach=").Append((vAir.magnitude / soundSpeed).ToString("G4"))
              .Append(" |omega|=").Append(omegaWorld.magnitude.ToString("G4"))
              .Append('\n');

            Vector3d tauSimTotal = Vector3d.zero, tauLiveTotal = Vector3d.zero;
            foreach (var p in _vessel.Parts) {
                if (p.ShieldedFromAirstream || p.Rigidbody == null)
                    continue;
                var vPart = vAir + Vector3.Cross(
                    omegaWorld, (Vector3)((Vector3d)p.Rigidbody.worldCenterOfMass - worldCoM));
                var pf = SimPartAeroForce(p, vPart, rho, soundSpeed);
                Vector3d liftPoint = p.partTransform.TransformPoint(p.CoLOffset);
                Vector3d dragPoint = p.partTransform.TransformPoint(p.CoPOffset);
                var tauSim = Vector3d.Cross(dragPoint - worldCoM, pf.Drag)
                             + Vector3d.Cross(liftPoint - worldCoM, pf.Lift);

                Vector3d liveCubeDrag = -(Vector3d)p.dragVectorDir * p.dragScalar;
                Vector3d liveBodyLift = Vector3d.zero;
                Vector3d liveModuleLift = Vector3d.zero;
                Vector3d liveModuleDrag = Vector3d.zero;
                Vector3d liveLiftPoint = liftPoint;
                if (!p.hasLiftModule) {
                    liveBodyLift = (Vector3d)p.partTransform.TransformDirection(
                        p.bodyLiftLocalVector);
                    liveLiftPoint = p.partTransform.TransformPoint(
                        p.bodyLiftLocalPosition);
                }
                var tauLive = Vector3d.Cross(dragPoint - worldCoM, liveCubeDrag)
                              + Vector3d.Cross(liveLiftPoint - worldCoM, liveBodyLift);
                foreach (var module in p.Modules) {
                    var wing = module as ModuleLiftingSurface;
                    if (wing == null)
                        continue;
                    liveModuleLift += (Vector3d)wing.liftForce;
                    liveModuleDrag += (Vector3d)wing.dragForce;
                    tauLive += Vector3d.Cross(liftPoint - worldCoM,
                                              (Vector3d)wing.liftForce);
                    tauLive += Vector3d.Cross(dragPoint - worldCoM,
                                              (Vector3d)wing.dragForce);

                    var nodeGated = false;
                    if (wing.nodeEnabled && !string.IsNullOrEmpty(wing.attachNodeName)) {
                        var node = p.FindAttachNode(wing.attachNodeName);
                        nodeGated = node != null && node.attachedPart != null;
                    }

                    var dynPressure = 0.0005 * rho * vPart.sqrMagnitude;
                    var mach = (float)Math.Min(25.0, vPart.magnitude / soundSpeed);
                    var liftQ = dynPressure * 1000.0;
                    Vector3 nVel, liftVector;
                    float liftDot, absDot;
                    wing.SetupCoefficients(
                        vPart, out nVel, out liftVector, out liftDot, out absDot);
                    var liftCurve = wing.liftCurve.Evaluate(absDot);
                    var liftMachCurve = wing.liftMachCurve.Evaluate(mach);
                    var coefficient = Mathf.Sign(liftDot) * liftCurve * liftMachCurve
                                      * wing.deflectionLiftCoeff;
                    var preProjection = nodeGated || coefficient == 0f
                        || float.IsNaN(coefficient)
                        ? Vector3.zero
                        : -liftVector * (float)(liftQ
                            * (PhysicsGlobals.LiftMultiplier * coefficient));
                    var simModuleLift = wing.perpendicularOnly
                        ? Vector3.ProjectOnPlane(preProjection, -nVel)
                        : preProjection;

                    sb.Append("  module ").Append(wing.moduleName)
                      .Append(" curve=").Append(wing.liftingSurfaceCurve)
                      .Append(" gated=").Append(nodeGated)
                      .Append(" mach=").Append(mach.ToString("G6"))
                      .Append(" qSimPa=").Append(liftQ.ToString("G6"))
                      .Append(" qLivePa=").Append(
                          (p.dynamicPressurekPa * 1000.0).ToString("G6"))
                      .Append(" liftDot=").Append(liftDot.ToString("G6"))
                      .Append(" absDot=").Append(absDot.ToString("G6"))
                      .Append(" liftCurve=").Append(liftCurve.ToString("G6"))
                      .Append(" machCurve=").Append(liftMachCurve.ToString("G6"))
                      .Append(" deflect=").Append(wing.deflectionLiftCoeff.ToString("G6"))
                      .Append(" coeff=").Append(coefficient.ToString("G6"))
                      .Append(" preProjN=").Append(
                          (preProjection.magnitude * 1000f).ToString("G6"))
                      .Append(" simN=").Append(
                          (simModuleLift.magnitude * 1000f).ToString("G6"))
                      .Append(" liveN=").Append(
                          (wing.liftForce.magnitude * 1000f).ToString("G6"))
                      .Append(" liveScalarN=").Append(
                          (wing.liftScalar * 1000f).ToString("G6"))
                      .Append(" dirDot=").Append(Vector3.Dot(
                          simModuleLift.sqrMagnitude > 0f ? simModuleLift.normalized
                                                        : wing.liftForce.normalized,
                          wing.liftForce.sqrMagnitude > 0f ? wing.liftForce.normalized
                                                          : simModuleLift.normalized).ToString("G6"))
                      .Append('\n');
                }
                var liveDrag = liveCubeDrag + liveModuleDrag;
                var liveLift = liveBodyLift + liveModuleLift;
                tauSimTotal += tauSim;
                tauLiveTotal += tauLive;

                var flowAngle = Vector3.Angle(vPart, p.dragVector);
                sb.Append(p.partInfo.name)
                  .Append(": dragN sim=").Append((pf.Drag.magnitude * 1000.0).ToString("G6"))
                  .Append(" live=").Append((liveDrag.magnitude * 1000.0).ToString("G6"))
                  .Append(" dirDot=").Append(
                      Vector3d.Dot(pf.Drag.normalized, liveDrag.magnitude > 0
                          ? liveDrag.normalized : pf.Drag.normalized).ToString("G4"))
                  .Append(" | liftN sim=").Append((pf.Lift.magnitude * 1000.0).ToString("G6"))
                  .Append(" live=").Append((liveLift.magnitude * 1000.0).ToString("G6"))
                  .Append(" bodyLive=").Append(
                      (liveBodyLift.magnitude * 1000.0).ToString("G6"))
                  .Append(" moduleLive=").Append(
                      (liveModuleLift.magnitude * 1000.0).ToString("G6"))
                  .Append(" | flow sim=").Append(vPart.magnitude.ToString("G6"))
                  .Append(" real=").Append(p.dragVector.magnitude.ToString("G6"))
                  .Append(" angle=").Append(flowAngle.ToString("G4")).Append("deg")
                  .Append(" | tauN sim=").Append((tauSim * 1000.0).magnitude.ToString("G6"))
                  .Append(" live=").Append((tauLive * 1000.0).magnitude.ToString("G6"))
                  .Append(" dTauN=").Append(((tauSim - tauLive) * 1000.0).magnitude.ToString("G6"))
                  .Append('\n');
            }
            sb.Append("TOTAL tauN sim=").Append((tauSimTotal * 1000.0).magnitude.ToString("G6"))
              .Append(" live=").Append((tauLiveTotal * 1000.0).magnitude.ToString("G6"))
              .Append(" |dTau|N=").Append(
                  ((tauSimTotal - tauLiveTotal) * 1000.0).magnitude.ToString("G6"));
            return sb.ToString();
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
                attitudeDelta, false, true);
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
                attitudeDelta, true, false);
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
            QuaternionD attitudeDelta, bool includeRotationalPartFlow,
            bool useLegacyUniformFlow)
        {
            var altitude = (worldCoM - body.position).magnitude - body.Radius;
            var rho = GetDensity(altitude, body);
            if (rho <= 0)
                return new AerodynamicWrench ();
            var soundSpeed = body.GetSpeedOfSound(body.GetPressure(altitude), rho);

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
                Vector3 nVel, liftVector;
                float liftdot, absdot;
                wing.SetupCoefficients(v_wrld_vel, out nVel, out liftVector, out liftdot, out absdot);

                // Do NOT use wing.GetLiftVector here: its perpendicularOnly branch
                // projects against the module's internal nVel FIELD, which is only
                // written by the module's own FixedUpdate from the craft's REAL
                // airflow (and is zero / stale on the pad or in vacuum). The
                // SetupCoefficients out-parameter above shadows that field, so for
                // hypothetical states the projection uses arbitrary stale state and
                // leaks a large mis-directed force (found via a bare Mk1 pod, whose
                // CapsuleBottom lift reached ~5-24 kN unprojected). Replicate the
                // lift math against the simulated airflow instead.
                var liftScalar = Mathf.Sign (liftdot) * wing.liftCurve.Evaluate (absdot)
                                 * wing.liftMachCurve.Evaluate (mach);
                liftScalar *= wing.deflectionLiftCoeff;
                if (liftScalar != 0f && !float.IsNaN (liftScalar)) {
                    var wingLift = -liftVector
                        * (float)(liftQ * (PhysicsGlobals.LiftMultiplier * liftScalar));
                    if (wing.perpendicularOnly)
                        wingLift = Vector3.ProjectOnPlane (wingLift, -nVel);
                    result.Lift += wingLift;
                }

                // GetDragVector is safe: it takes the airflow as a parameter.
                var prevMach = p.machNumber;
                try {
                    p.machNumber = mach;
                    result.Drag += wing.GetDragVector(nVel, absdot, liftQ);
                } finally {
                    p.machNumber = prevMach;
                }
            }

            return result;
        }
    }
}
