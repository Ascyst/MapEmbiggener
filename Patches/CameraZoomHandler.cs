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
    /// Patch to separate the camera zoom from the map size, as well as rotate and position the cameras 
    /// </summary>
    [HarmonyPatch(typeof(CameraZoomHandler), "Update")]
    class CameraZoomHandler_Patch_Update
    {
        static float GetCurrentZoom()
        {
            return ControllerManager.Zoom;
        }
        // set camera zoom
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
        // set camera rotation and position
        static void Postfix(Camera[] ___cameras)
        {
            // cameras will not move instantly for QoL reasons
            for (int i = 0; i < ___cameras.Length; i++)
            {
                ___cameras[i].transform.position = Vector3.Lerp(___cameras[i].transform.position, ControllerManager.CameraPosition, Time.unscaledDeltaTime * 5f);
                ___cameras[i].transform.rotation = Quaternion.RotateTowards(___cameras[i].transform.rotation, ControllerManager.CameraRotation, Time.unscaledDeltaTime * 50f);
            }
        }
    }
}
