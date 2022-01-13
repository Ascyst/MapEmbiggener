using System.Collections;
using UnityEngine;

namespace MapEmbiggener
{
    internal static class OutOfBoundsUtils
    {
        public static readonly float defaultX = 35.56f;
        public static readonly float defaultY = 20f;

        public static float minX { get; private set; } = -35.56f; 
        public static float maxX { get; private set; } = 35.56f;
        public static float minY { get; private set; } = -20f;
        public static float maxY { get; private set; } = 20f;

        private static GameObject border;

        private static bool _OOBEnabled = false;
        internal static bool OOBEnabled
        {
            get
            {
                if (GM_Test.instance != null && GM_Test.instance.gameObject != null)
                {
                    return (_OOBEnabled || GM_Test.instance.gameObject.activeInHierarchy);
                }
                else
                {
                    return _OOBEnabled;
                }

            }
            set => _OOBEnabled = value;
        }
        
        internal static IEnumerator SetOOBEnabled(bool enabled)
        {
            OOBEnabled = enabled;
            yield break;
        }

        public static void SetOOB(float x, float y)
        {
            SetOOB(-x, x, -y, y);
        }
        
        public static void SetOOB(float minX, float maxX, float minY, float maxY)
        {
            OutOfBoundsUtils.minX = minX;
            OutOfBoundsUtils.maxX = maxX;
            OutOfBoundsUtils.minY = minY;
            OutOfBoundsUtils.maxY = maxY;

            if (border == null) return;
            var renderer = border.GetComponent<RectTransform>();
            renderer.sizeDelta = new Vector2((maxX - minX)*(1930/defaultX/2), (maxY - minY)*(1090/defaultY/2));
            border.transform.position = new Vector3(minX + (maxX - minX) / 2, minY + (maxY - minY) / 2, 0);
        }

        internal static void CreateBorder()
        {
            // Reset OOB
            SetOOB(defaultX, defaultY);
            
            // Move border to a worldSpace canvas
            border = UIHandler.instance.transform.Find("Canvas/Border").gameObject;
            var canvas = new GameObject("BorderCanvas").AddComponent<Canvas>();
            border.transform.SetParent(canvas.transform);
            border.transform.position = Vector3.zero;
            border.transform.localScale = new Vector3(0.037f, 0.037f, 0.037f);
            canvas.renderMode = RenderMode.WorldSpace;
        }
    }
}