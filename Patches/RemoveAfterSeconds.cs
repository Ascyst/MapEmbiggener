using UnityEngine;
using HarmonyLib;
using MapEmbiggener.Extensions;
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
                __instance.seconds *= MapEmbiggener.setSize;
                foreach (Rigidbody2D rig in __instance.gameObject.GetComponentsInChildren<Rigidbody2D>())
                {
                    //rig.mass *= MapEmbiggener.setSize;
                    rig.transform.localScale *= MapEmbiggener.setSize;
                }
            }
        }
    }
}
