using System;
using BepInEx;
using HarmonyLib;
using Photon.Pun;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;
using System.Collections;
using UnboundLib.Networking;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace MapEmbiggener
{
    [HarmonyPatch(typeof(OutOfBoundsHandler), "LateUpdate")]
    class OutOfBoundsHandler_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            int insertIndexNeg = -1;
            int insertIndexPos = -1;

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == -35.56)
                {
                    insertIndexNeg = i;
                    codes.Insert(insertIndexNeg, new CodeInstruction(OpCodes.Ldarg_0));
                    codes.Insert(insertIndexNeg + 1, new CodeInstruction(OpCodes.Ldfld, MapEmbiggener.Mod.setSize));
                    codes.Insert(insertIndexNeg + 2, new CodeInstruction(OpCodes.Mul));
                    break;
                }
                if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 35.56)
                {
                    insertIndexPos = i;
                    codes.Insert(insertIndexPos, new CodeInstruction(OpCodes.Ldarg_0));
                    codes.Insert(insertIndexPos + 1, new CodeInstruction(OpCodes.Ldfld, MapEmbiggener.Mod.setSize));
                    codes.Insert(insertIndexPos + 2, new CodeInstruction(OpCodes.Mul));
                    break;
                }
            }
            return codes;
        }
    }
}
