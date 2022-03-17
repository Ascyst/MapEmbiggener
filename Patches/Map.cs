using HarmonyLib;
using UnityEngine;
using UnboundLib;
using Photon.Pun;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using MapEmbiggener.Extensions;
using MapEmbiggener.Controllers;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace MapEmbiggener.Patches
{
    [HarmonyPatch(typeof(Map),"Start")]
    class MapPatchStart
    {
        private static void Postfix(Map __instance)
        {
            MapEmbiggener.defaultMapSize = __instance.size;

            foreach (SpawnPoint spawnPoint in __instance.GetComponentsInChildren<SpawnPoint>())
            {
                spawnPoint.localStartPos *= ControllerManager.MapSize;
            }
            __instance.transform.localScale *= ControllerManager.MapSize;
            __instance.size *= ControllerManager.MapSize;
        }
    }
    [HarmonyPatch(typeof(Map),"Update")]
    class Map_Patch_Update
    {
        private static void Postfix(Map __instance)
        {
            if (__instance?.transform == null) { return; }
            float angle = ControllerManager.MapAngle - __instance.transform.rotation.eulerAngles.z;

            if (angle == 0f) { return; }
            __instance.transform.RotateAround(Vector3.zero, Vector3.forward, angle);
        }
    }
    [HarmonyPatch]
    class MapPatchStartMatch
    {
        static Type GetNestedMoveType()
        {
            var nestedTypes = typeof(Map).GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic);
            Type nestedType = null;

            foreach (var type in nestedTypes)
            {
                if (type.Name.Contains("StartMatch"))
                {
                    nestedType = type;
                    break;
                }
            }

            return nestedType;
        }

        static MethodBase TargetMethod()
        {
            return AccessTools.Method(GetNestedMoveType(), "MoveNext");
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            var m_Embiggen = ExtensionMethods.GetMethodInfo(typeof(MapPatchStartMatch), nameof(Embiggen));
            var f_allRigs = ExtensionMethods.GetFieldInfo(typeof(Map), nameof(Map.allRigs));

            foreach (CodeInstruction code in codes)
            {
                yield return code;
                if (code.StoresField(f_allRigs))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Call, m_Embiggen);
                }
            }
        }

        private static void Embiggen(Map __instance)
        {
            // if a stickfightmaps object has any particlesystem components, then scale those up
            foreach (ParticleSystem part in __instance.gameObject.GetComponentsInChildren<ParticleSystem>())
            {
                if (part.gameObject.IsStickFightObject())
                {
                    part.transform.localScale *= ControllerManager.MapSize;
                }
            }
            foreach (Rigidbody2D rig in __instance.allRigs)
            {

                // if its a MapsExtended object, mess with the position only on the host's side
                if (rig.gameObject.IsMapsExtObject())
                {
                    if (rig.gameObject.GetComponent<PhotonView>().IsMine)
                    {
                        rig.gameObject.transform.position = new Vector3(ControllerManager.MapSize * rig.gameObject.transform.position.x, rig.gameObject.transform.position.y * ControllerManager.MapSize, rig.gameObject.transform.position.z);
                    }
                }

                // rescale physics objects UNLESS they have a movesequence component
                // also UNLESS they are a crate from stickfight (Boss Sloth's stickfight maps mod)
                // if they have a movesequence component then scale the points in that component

                if (rig.gameObject.IsStickFightObject())
                {

                    if (rig.gameObject.name.Contains("PLatform"))
                    {
                        rig.transform.localScale /= ControllerManager.MapSize;
                    }

                    rig.mass *= ControllerManager.MapSize;

                    // increase the strength of chains
                    if (rig.gameObject.name.Contains("Chain"))
                    {
                        if (rig.gameObject.GetComponent<ForceMultiplier>() != null) { rig.gameObject.GetComponent<ForceMultiplier>().multiplier *= ControllerManager.MapSize; }
                        foreach (DistanceJoint2D joint in rig.gameObject.GetComponents<DistanceJoint2D>())
                        {
                            joint.distance *= ControllerManager.MapSize;
                        }
                    }
                    continue;
                }

                if (rig.gameObject.GetComponentInChildren<MoveSequence>(true) == null)
                {
                    rig.mass *= ControllerManager.MapSize;

                    // if its a maps extended object, then only change its size on the host client
                    if (rig.gameObject.IsMapsExtObject() && !rig.gameObject.GetComponent<PhotonView>().IsMine)
                    {
                        continue;
                    }

                    rig.transform.localScale *= ControllerManager.MapSize;
                }
                else
                {
                    List<Vector2> newPos = new List<Vector2>() { };
                    MoveSequence move = rig.gameObject.GetComponentInChildren<MoveSequence>(true);
                    foreach (Vector2 pos in move.positions)
                    {
                        newPos.Add(pos * ControllerManager.MapSize);
                    }
                    move.positions = newPos.ToArray();
                    move.SetFieldValue("startPos", ControllerManager.MapSize * (Vector2)move.GetFieldValue("startPos"));
                }
            }

            // this is only the move the overhead light into the correct place
            GameObject Rendering = GameObject.Find("/Game/Visual/Rendering ");

            if (Rendering != null)
            {
                Rendering.transform.localScale = new Vector3(ControllerManager.MapSize, ControllerManager.MapSize, Rendering.transform.localScale.z);
            }
        }
    }
}
