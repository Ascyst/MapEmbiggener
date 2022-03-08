using UnityEngine;
using HarmonyLib;
using UnboundLib;
using MapEmbiggener.UI;
namespace MapEmbiggener.Patches
{
    [HarmonyPatch(typeof(ArtInstance), nameof(ArtInstance.TogglePart))]
    class ArtInstancePatchTogglePart
    {
        static void Postfix(ArtInstance __instance, bool on)
        {
            if (!on) { return; }
            foreach (ParticleSystem part in __instance.parts)
            {
                part.gameObject.GetOrAddComponent<FollowAndScaleWithCamera>();
            }
        }
    }
}
