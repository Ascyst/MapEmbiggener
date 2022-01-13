using System;
using HarmonyLib;
using UnboundLib;
using UnityEngine;

namespace MapEmbiggener.Patches
{
    [Serializable]
    [HarmonyPatch(typeof(OutOfBoundsHandler), "LateUpdate")]
    [HarmonyBefore(new string[] { "io.olavim.rounds.rwf" })]
    class OutOfBoundsHandlerPatchLateUpdate
    {
        private static bool Prefix(OutOfBoundsHandler __instance, CharacterData ___data, float ___warningPercentage, ref float ___counter, ref bool ___almostOutOfBounds, ref bool ___outOfBounds, ref ChildRPC ___rpc)
        {
            if (!___data)
            {
                UnityEngine.GameObject.Destroy(__instance.gameObject);
                return false; // skip the original (BAD IDEA)
            }
            //if (!(bool)Traverse.Create(___data.playerVel).Field("simulated").GetValue())
            //{
                //return false; // skip the original (BAD IDEA)
            //}
            if (!OutOfBoundsUtils.OOBEnabled)
            {
                return false;
            }
            if (!___data.isPlaying)
            {
                return false; // skip the original (BAD IDEA)
            }
            
            float x = Mathf.InverseLerp(OutOfBoundsUtils.minX, OutOfBoundsUtils.maxX, ___data.transform.position.x);
            float y = Mathf.InverseLerp(OutOfBoundsUtils.minY, OutOfBoundsUtils.maxY, ___data.transform.position.y);
            Vector3 vector = new Vector3(x, y, 0f);
            vector = new Vector3(Mathf.Clamp(vector.x, 0f, 1f), Mathf.Clamp(vector.y, 0f, 1f), vector.z);

            // Vector3 vector = MainCam.instance.transform.GetComponent<Camera>().FixedWorldToScreenPoint(new Vector3(___data.transform.position.x, ___data.transform.position.y, 0f));
            //
            // vector.x /= (float)FixedScreen.fixedWidth;
            // vector.y /= (float)Screen.height;
            //
            // vector = new Vector3(Mathf.Clamp01(vector.x), Mathf.Clamp01(vector.y), 0f);

            ___almostOutOfBounds = false;
            ___outOfBounds = false;
            if (vector.x <= 0f || vector.x >= 1f || vector.y >= 1f || vector.y <= 0f)
            {
                ___outOfBounds = true;
            }
            else if (vector.x < ___warningPercentage || vector.x > 1f - ___warningPercentage || vector.y > 1f - ___warningPercentage || vector.y < ___warningPercentage)
            {
                ___almostOutOfBounds = true;
                if (vector.x < ___warningPercentage)
                {
                    vector.x = 0f;
                }
                if (vector.x > 1f - ___warningPercentage)
                {
                    vector.x = 1f;
                }
                if (vector.y < ___warningPercentage)
                {
                    vector.y = 0f;
                }
                if (vector.y > 1f - ___warningPercentage)
                {
                    vector.y = 1f;
                }
            }
            ___counter = ___counter + TimeHandler.deltaTime;
            if (___almostOutOfBounds && !___data.dead)
            {
                __instance.transform.position = (Vector3)__instance.InvokeMethod("GetPoint", vector);
                __instance.transform.rotation = Quaternion.LookRotation(Vector3.forward, -(___data.transform.position - __instance.transform.position));
                if (___counter > 0.1f)
                {
                    ___counter = 0f;
                    __instance.warning.Play();
                }
            }
            if (___outOfBounds && !___data.dead)
            {
                ___data.sinceGrounded = 0f;
                __instance.transform.position = (Vector3)__instance.InvokeMethod("GetPoint", vector);
                __instance.transform.rotation = Quaternion.LookRotation(Vector3.forward, -(___data.transform.position - __instance.transform.position));
                if (___counter > 0.1f && ___data.view.IsMine)
                {
                    ___counter = 0f;
                    // This makes sure we almost always launch about 60% of the map size
                    var launchCorrection = __instance.transform.up.x != 0
                        ? (OutOfBoundsUtils.maxX - OutOfBoundsUtils.minX) / 71.12f
                        : (OutOfBoundsUtils.maxY - OutOfBoundsUtils.minY) / 40f;
                    
                    if (___data.block.IsBlocking())
                    {
                        ___rpc.CallFunction("ShieldOutOfBounds");
                        Traverse.Create(___data.playerVel).Field("velocity").SetValue((Vector2)Traverse.Create(___data.playerVel).Field("velocity").GetValue() * 0f);
                        ___data.healthHandler.CallTakeForce(__instance.transform.up * 400f * launchCorrection * (float)___data.playerVel.GetFieldValue("mass"), ForceMode2D.Impulse, false, true, 0f);
                        ___data.transform.position = __instance.transform.position;
                        return false; // skip the original (BAD IDEA)
                    }
                    ___rpc.CallFunction("OutOfBounds");
                    ___data.healthHandler.CallTakeForce(__instance.transform.up * 200f * launchCorrection * (float)___data.playerVel.GetFieldValue("mass"), ForceMode2D.Impulse, false, true, 0f);
                    ___data.healthHandler.CallTakeDamage(51f * __instance.transform.up, ___data.transform.position, null, null, true);
                }
            }
            return false; // skip the original (BAD IDEA)

        }
    }

    [Serializable]
    [HarmonyPatch(typeof(OutOfBoundsHandler),"GetPoint")]
    class OutOfBoundHandlerPatchGetPoint
    {
        private static bool Prefix(ref Vector3 __result, OutOfBoundsHandler __instance, Vector3 p)
        {
            float x = Mathf.Lerp(OutOfBoundsUtils.minX, OutOfBoundsUtils.maxX, p.x);
            float y = Mathf.Lerp(OutOfBoundsUtils.minY, OutOfBoundsUtils.maxY, p.y);
            __result = new Vector3(x, y, 0f);

            return false; // skip the original (BAD IDEA)
        }
    }
}