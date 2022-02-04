using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using MapEmbiggener.Controllers;
using UnboundLib;
namespace MapEmbiggener.Patches
{
    [HarmonyPatch(typeof(MapTransition), nameof(MapTransition.Exit))]
    static class MapTransition_Patch_Exit
    {
        // patch to ensure maps fully exit the screen
        static float GetOffset()
        {
            return -90f * ControllerManager.MapSize;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            var m_offset = ExtensionMethods.GetMethodInfo(typeof(MapTransition_Patch_Exit), nameof(GetOffset));
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Ldc_R4 && (float)code.operand == -90f)
                {
                    yield return new CodeInstruction(OpCodes.Call, m_offset);        
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}
