using System;
using KRPC.SpaceCenter.Services.Parts;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class ModuleWheelDeploymentExtensions
    {
        /// <summary>
        /// The deployment state of a landing leg or wheel. Both modules are optional
        /// on a part: a null deployment module means the leg or wheel is fixed, and so
        /// always deployed, and a null damage module means it cannot break.
        /// </summary>
        public static DeployableState ToDeployableState (
            this ModuleWheels.ModuleWheelDeployment deployment,
            ModuleWheels.ModuleWheelDamage damage)
        {
            if (damage != null && damage.isDamaged)
                return DeployableState.Broken;
            if (deployment == null)
                return DeployableState.Deployed;
            if (Math.Abs (deployment.position - deployment.deployedPosition) < 0.0001)
                return DeployableState.Deployed;
            if (Math.Abs (deployment.position - deployment.retractedPosition) < 0.0001)
                return DeployableState.Retracted;
            if (deployment.stateString.Equals (deployment.st_deploying.name))
                return DeployableState.Deploying;
            if (deployment.stateString.Equals (deployment.st_retracting.name))
                return DeployableState.Retracting;
            throw new InvalidOperationException ();
        }
    }
}
