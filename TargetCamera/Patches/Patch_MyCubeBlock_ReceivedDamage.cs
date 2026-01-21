using System;
using HarmonyLib;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace SETargetCamera.Patches
{
    [HarmonyPatch(typeof(MySlimBlock), "ApplyAccumulatedDamage")]
    public static class Patch_MySlimBlock_ApplyAccumulatedDamage
    {
        /// <summary>
        /// Postfix patch for MySlimBlock.ApplyAccumulatedDamage to create hit feedback
        /// </summary>
        public static bool Prefix(MySlimBlock __instance, bool addDirtyParts = true, long attackerId = 0)
        {
            if (!Plugin.Settings.DamageFeedbackEnabled)
                return true;
            
            if (__instance == null || __instance.CubeGrid == null)
                return true;
            
            // Get the cube grid that this block belongs to
            MyCubeGrid cubeGrid = __instance.CubeGrid;
            if (cubeGrid.Closed)
                return true;


            // Check if this grid is the currently targeted entity
            if (!IsTargetedGrid(cubeGrid))
                return true;
 

            // Get the accumulated damage
            float damage = __instance.AccumulatedDamage;
            if (damage <= 0)
                return true;
                
            Vector3D gridRelativePosition = __instance.Position * __instance.CubeGrid.GridSize;
            
            double damageMultiplier = Math.Pow(damage / 100.0, 0.29);

            double lifespan = (damageMultiplier * 1f);
            double radius = (damageMultiplier * 1f); 
            
            Color color = Plugin.Settings.DamageFeedbackColor;

            HitFeedbackManager.AddHitFeedback(gridRelativePosition, cubeGrid, (float)lifespan, color, (float)radius);

            return true;
        }

        /// <summary>
        /// Checks if the given grid is the currently targeted entity in TargetCamera
        /// </summary>
        private static bool IsTargetedGrid(MyCubeGrid grid)
        {
            return TargetCamera.IsGridTargeted(grid);
        }
    }
}
