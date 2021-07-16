/*using System;
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

    [HarmonyPatch(typeof(CameraZoomHandler), "Update")]
    class CameraZoomHandler_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            int insertIndex = -1;

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld && codes[i].opcode != )
                {
                    insertIndex = i;
                    codes.Insert(insertIndex, new CodeInstruction(OpCodes.Ldarg_0));
                    codes.Insert(insertIndex + 1, new CodeInstruction(OpCodes.Ldfld, MapEmbiggener.Mod.setSize));
                    codes.Insert(insertIndex + 2, new CodeInstruction(OpCodes.Mul));
                    break;
                }
            }
            return codes;
        }
    }
}*/