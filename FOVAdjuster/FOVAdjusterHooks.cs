using BBI.Unity.Game;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FOVAdjuster
{
    internal class FOVAdjusterHooks
    {
        [HarmonyPatch(typeof(GlassModeController), "HandleGlassMode", new Type[] { typeof(bool), typeof(bool) })]
        public class GlassModeController_HandleGlassMode
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (FOVAdjuster.needsInit)
                {
                    FOVAdjuster.UpdateFOV();
                    FOVAdjuster.needsInit = false;
                }
            }
        }
    }

    [HarmonyPatch(typeof(GunController), "Awake")]
    public class GunController_Awake
    {
        [HarmonyPostfix]
        public static void Postfix(Transform ___m_GrapplingGunRestPoint, Transform ___m_GrapplingGunShadowRestPoint)
        {
            var newOffset = Mathf.LerpUnclamped(0, Settings.settings.toolOffsetAt90, FOVAdjuster.InverseLerpUnclamped(72, 90, Settings.settings.fov));
            ___m_GrapplingGunRestPoint.position -= new Vector3(0, newOffset, 0);
        }
    }
        
    [HarmonyPatch(typeof(CuttingToolController), "Awake")]
    public class CuttingToolController_Awake
    {
        [HarmonyPostfix]
        public static void Postfix(Transform ___m_ToolRestPoint, Transform ___m_CuttingToolShadowRestPoint)
        {
            var newOffset = Mathf.LerpUnclamped(0, Settings.settings.toolOffsetAt90, FOVAdjuster.InverseLerpUnclamped(72, 90, Settings.settings.fov));
            ___m_ToolRestPoint.position -= new Vector3(0, newOffset, 0);
        }
    }

    [HarmonyPatch(typeof(HelmetController), "OnHelmetToggled")]
    public class HelmetController_OnHelmetToggled
    {
        [HarmonyPrefix]
        public static bool Prefix(ToggleHelmetEvent ev)
        {
            if(ev.State == HelmetState.On && !Settings.settings.showHelmet)
            {
                return false;
            }

            return true;
        }
    }
}
