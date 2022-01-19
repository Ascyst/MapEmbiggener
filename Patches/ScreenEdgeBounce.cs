using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace MapEmbiggener.Patches
{
    [HarmonyPatch(typeof(ScreenEdgeBounce), "Update")]
    public class ScreenEdgeBouncePatch
    {
        public static bool Prefix(ScreenEdgeBounce __instance, PhotonView ___view, ref bool ___done, Camera ___mainCam, ref Vector2 ___lastNormal, RayHitReflect ___reflect, ref float ___sinceBounce)
        {
            if (!___view.IsMine)
            {
                return false;
            }
            if (___done)
            {
                return false;
            }
            // float x = Mathf.InverseLerp(OutOfBoundsUtils.minX, OutOfBoundsUtils.maxX, __instance.transform.position.x);
            // float y = Mathf.InverseLerp(OutOfBoundsUtils.minY, OutOfBoundsUtils.maxY, __instance.transform.position.y);
            // Vector3 vector = new Vector3(x, y, 0f);
            // vector = new Vector3(Mathf.Clamp(vector.x, 0f, 1f), Mathf.Clamp(vector.y, 0f, 1f), vector.z);
            
            
            if (!OutOfBoundsUtils.IsInsideBounds(__instance.transform.position, out var vector))
            {
                Vector2 vector2 = Vector2.zero;
                
                // Here we rotate the direction vector by the border angle
                if (vector.x == 0f)
                {
                    vector2 = Quaternion.Euler(0,0,OutOfBoundsUtils.angle)*Vector2.right;
                }
                else if (vector.x == 1f)
                {
                    vector2 = Quaternion.Euler(0,0,OutOfBoundsUtils.angle)*-Vector2.right;
                }
                if (vector.y == 0f)
                {
                    vector2 = Quaternion.Euler(0,0,OutOfBoundsUtils.angle)*Vector2.up;
                }
                else if (vector.y == 1f)
                {
                    vector2 = Quaternion.Euler(0,0,OutOfBoundsUtils.angle)*-Vector2.up;
                }
                if (___lastNormal == vector2 && Vector2.Angle(vector2, __instance.transform.forward) < 90f)
                {
                    ___lastNormal = vector2;
                    return false;
                }
                ___lastNormal = vector2;
                RaycastHit2D raycastHit2D = default(RaycastHit2D);
                raycastHit2D.normal = vector2;
                raycastHit2D.point = __instance.transform.position;
                int num = -1;
                if (raycastHit2D.transform)
                {
                    PhotonView component = raycastHit2D.transform.root.GetComponent<PhotonView>();
                    if (component)
                    {
                        num = component.ViewID;
                    }
                }
                int intData = -1;
                if (num == -1)
                {
                    Collider2D[] componentsInChildren = MapManager.instance.currentMap.Map.GetComponentsInChildren<Collider2D>();
                    for (int i = 0; i < componentsInChildren.Length; i++)
                    {
                        if (componentsInChildren[i] == raycastHit2D.collider)
                        {
                            intData = i;
                        }
                    }
                }
                __instance.GetComponentInParent<ChildRPC>().CallFunction("ScreenBounce", raycastHit2D.point, raycastHit2D.normal, num, intData);
                if (___reflect.reflects <= 0)
                {
                    ___done = true;
                }
                ___sinceBounce = 0f;
            }

            return false;
        }
    }
}