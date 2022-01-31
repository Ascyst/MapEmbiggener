using UnityEngine;

namespace MapEmbiggener.Extensions
{
    internal static class GameObjectExtension
    {
        internal static bool IsStickFightObject(this GameObject obj, int depth = 2)
        {
            if (obj == null) { return false; }
            Transform transform = obj.transform;
            for (int i = 0; i < depth; i++)
            {
                if (transform == null) { break; }

                if (transform.gameObject.name.ContainsAny(MapEmbiggener.stickFightObjsToIgnore) && !transform.gameObject.name.ContainsAny(MapEmbiggener.falsePositiveNonStickFightObjs)) { return true; }

                transform = transform.parent;

            }
            return false;
        }
        internal static bool IsMapsExtObject(this GameObject obj)
        {
            if (obj == null) { return false; }
            foreach (Component comp in obj.GetComponents<Component>())
            {
                if (comp.ToString().Contains("MapsExt"))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
