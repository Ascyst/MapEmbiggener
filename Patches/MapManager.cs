using HarmonyLib;
using MapEmbiggener.Controllers;
namespace MapEmbiggener.Patches
{
    [HarmonyPatch(typeof(MapManager), "OnLevelFinishedLoading")]
    class MapManager_Patch_OnLevelFinishedLoading
    {
        // patch to move maps out of the way of the pick phase
        static void Postfix(MapManager __instance, bool ___callInNextMap)
        {
            if (!___callInNextMap)
            {
                __instance.currentMap.Map.transform.position = UnityEngine.Vector3.Scale(__instance.currentMap.Map.transform.position, new UnityEngine.Vector3(ControllerManager.MapSize, 1f, 1f));
            }
        }
    }
}
