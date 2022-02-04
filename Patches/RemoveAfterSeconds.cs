using UnityEngine;
using HarmonyLib;
using MapEmbiggener.Extensions;
using MapEmbiggener.Controllers;
namespace MapEmbiggener.Patches
{
    // patch for special stickfightmaps objects
    [HarmonyPatch(typeof(RemoveAfterSeconds),"Start")]
    class RemoveAfterSecondsPatchStart
    {
        private static void Prefix(RemoveAfterSeconds __instance)
        {
            // if this object is a stickfightmaps spawner object, then scale it up and increase its timer
            if (__instance.gameObject.name.ContainsAny(MapEmbiggener.stickFightSpawnerObjs) && !__instance.gameObject.name.ContainsAny(MapEmbiggener.stickFightObjsToIgnore))
            {
                __instance.seconds *= ControllerManager.MapSize;
                foreach (Rigidbody2D rig in __instance.gameObject.GetComponentsInChildren<Rigidbody2D>())
                {
                    //rig.mass *= ControllerManager.MapSize;
                    rig.transform.localScale *= ControllerManager.MapSize;
                }
            }
        }
    }
}
