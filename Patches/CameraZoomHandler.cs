using HarmonyLib;
using UnityEngine;
using MapEmbiggener.Controllers;
using System.Reflection.Emit;
using System.Collections.Generic;
using UnboundLib;
using System.Reflection;
namespace MapEmbiggener.Patches
{
    /// <summary>
    /// Patch to separate the camera zoom from the map size 
    /// </summary>
    [HarmonyPatch(typeof(CameraZoomHandler), "Update")]
    class CameraZoomHandler_Patch_Update
    {
        static float GetCurrentZoom()
        {
            return ControllerManager.Zoom;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            FieldInfo f_size = ExtensionMethods.GetFieldInfo(typeof(Map), nameof(Map.size));
            MethodInfo m_zoom = ExtensionMethods.GetMethodInfo(typeof(CameraZoomHandler_Patch_Update), nameof(GetCurrentZoom));
            foreach (CodeInstruction code in codes)
            {
                if (code.LoadsField(f_size))
                {
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Call, m_zoom);
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}
