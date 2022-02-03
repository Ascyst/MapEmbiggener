using System;
using System.Collections;
using UnityEngine;
using MapEmbiggener.UI;

namespace MapEmbiggener
{
    public enum OutOfBoundsDamage
    {
        Normal,
        OverTime,
        Instakill,
        None
    }
    public class OutOfBoundsUtils : MonoBehaviour
    {

        public const OutOfBoundsDamage DefaultDamage = OutOfBoundsDamage.Normal;

        public const float defaultX = 35.56f;
        public const float defaultY = 20f;
        public const float defaultAngle = 0f;

        public static float width => -OutOfBoundsUtils.minX+OutOfBoundsUtils.maxX;
        public static float height => -OutOfBoundsUtils.minY+OutOfBoundsUtils.maxY;

        public static float minX { get; private set; } = -35.56f; 
        public static float maxX { get; private set; } = 35.56f;
        public static float minY { get; private set; } = -20f;
        public static float maxY { get; private set; } = 20f;

        public static float angle { get; private set; } = 0;

        public static Vector3 center
        {
            get
            {
                Vector3 unrotated = new Vector3(minX + (maxX - minX) / 2, minY + (maxY - minY) / 2, 0);

                return Quaternion.AngleAxis(OutOfBoundsUtils.angle, Vector3.forward) * unrotated;
            }
        }

        public static GameObject border {get; private set; }
        public static GameObject particleMask { get; internal set; }

        private static bool _OOBEnabled;
        internal static bool OOBEnabled
        {
            get
            {
                if (GM_Test.instance != null && GM_Test.instance.gameObject != null)
                {
                    return (OutOfBoundsUtils._OOBEnabled || GM_Test.instance.gameObject.activeInHierarchy);
                }
                else
                {
                    return OutOfBoundsUtils._OOBEnabled;
                }

            }
            set => OutOfBoundsUtils._OOBEnabled = value;
        }
        
        internal static IEnumerator SetOOBEnabled(bool enabled)
        {
            OutOfBoundsUtils.OOBEnabled = enabled;
            yield break;
        }

        public static void SetOOB(float x, float y, float angle = -1)
        {
            SetOOB(-x, x, -y, y, angle);
        }
        
        public static void SetOOB(float minX, float maxX, float minY, float maxY, float angle = -1)
        {
            OutOfBoundsUtils.minX = minX;
            OutOfBoundsUtils.maxX = maxX;
            OutOfBoundsUtils.minY = minY;
            OutOfBoundsUtils.maxY = maxY;
            OutOfBoundsUtils.angle = angle == -1 ? OutOfBoundsUtils.angle : angle;

            if (OutOfBoundsUtils.border == null) return;
            var rect = OutOfBoundsUtils.border.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2((maxX - minX)*(1930/OutOfBoundsUtils.defaultX/2), (maxY - minY)*(1090/OutOfBoundsUtils.defaultY/2));
            rect.rotation = Quaternion.Euler(0, 0, OutOfBoundsUtils.angle);
            OutOfBoundsUtils.border.transform.position = center;

            if (OutOfBoundsUtils.particleMask == null) return;
            OutOfBoundsUtils.particleMask.transform.rotation = Quaternion.Euler(0, 0, 0);
            OutOfBoundsParticles.SetSpriteSize(OutOfBoundsUtils.particleMask.GetComponent<SpriteMask>(), new Vector2((maxX - minX) * (1930 / OutOfBoundsUtils.defaultX / 2), (maxY - minY) * (1090 / OutOfBoundsUtils.defaultY / 2)));
            OutOfBoundsUtils.particleMask.transform.rotation = Quaternion.Euler(0, 0, OutOfBoundsUtils.angle);
            OutOfBoundsUtils.particleMask.transform.position = OutOfBoundsUtils.center + new Vector3(0f, 0f, OutOfBoundsUtils.particleMask.transform.position.z);
        }

        public static void SetAngle(float angle)
        {
            SetOOB(OutOfBoundsUtils.minX, OutOfBoundsUtils.maxX, OutOfBoundsUtils.minY, OutOfBoundsUtils.maxY, angle);
        }


        public static Vector3 GetPoint(Vector3 point)
        {
            // points coords are a 0 to 1 value that represent the percentage of how far the player is across the rotated border
            // that means if the border is not rotated x:0 means the left side, x:1 means the right side
            // here we convert the percentage to a world position of a not rotated border
            // then we rotate the world position to the current rotation of the border
            var radical = OutOfBoundsUtils.angle /180f * Mathf.PI;
            var x = Mathf.Lerp(OutOfBoundsUtils.minX, OutOfBoundsUtils.maxX, point.x);
            var y = Mathf.Lerp(OutOfBoundsUtils.minY, OutOfBoundsUtils.maxY, point.y);
            var cos = Mathf.Cos(radical);
            var sin = Mathf.Sin(radical);
            // rotation matrix
            var x1 = cos * x - sin * y;
            var y1 = sin * x + cos * y;
            

            return new Vector3(x1, y1, 0f);
        }

        public static Vector3 InverseGetPoint(Vector3 point)
        {
            var radical = OutOfBoundsUtils.angle / 180f * Mathf.PI;
            var x = point.x;
            var y = point.y;
            var cos = Mathf.Cos(radical);
            var sin = Mathf.Sin(radical);
            // inverse rotation matrix
            var x1 = cos * x + sin * y;
            var y1 = -sin * x + cos * y;

            // pass out the vector
            float xv = Mathf.InverseLerp(OutOfBoundsUtils.minX, OutOfBoundsUtils.maxX, x1);
            float yv = Mathf.InverseLerp(OutOfBoundsUtils.minY, OutOfBoundsUtils.maxY, y1);
            return new Vector3(xv, yv, 0f);
        }

        // check if point is inside of a rotated rectangle with some fancy math (Almost fully generated by github copilot)
        public static bool IsInsideBounds(Vector2 point, out Vector3 vector)
        {
            var radical = OutOfBoundsUtils.angle /180f * Mathf.PI;
            var x = point.x;
            var y = point.y;
            var cos = Mathf.Cos(radical);
            var sin = Mathf.Sin(radical);
            // inverse rotation matrix
            var x1 = cos * x + sin * y;
            var y1 = -sin * x + cos * y;
            
            // pass out the vector
            float xv = Mathf.InverseLerp(OutOfBoundsUtils.minX, OutOfBoundsUtils.maxX, x1);
            float yv = Mathf.InverseLerp(OutOfBoundsUtils.minY, OutOfBoundsUtils.maxY, y1);
            vector = new Vector3(xv, yv, 0f);
            // vector = new Vector3(Mathf.Clamp(vector.x, 0f, 1f), Mathf.Clamp(vector.y, 0f, 1f), vector.z);
            
            return x1 >= OutOfBoundsUtils.minX && x1 <= OutOfBoundsUtils.maxX && y1 >= OutOfBoundsUtils.minY && y1 <= OutOfBoundsUtils.maxY;
        }

        // check if point is almost outside of a rotated rectangle with some fancy math
        public static bool IsAlmostOutsideRect(Vector2 point, float warningPercentage, out Vector3 vector)
        {
            var radical = OutOfBoundsUtils.angle /180f * Mathf.PI;
            var x = point.x;
            var y = point.y;
            var cos = Mathf.Cos(radical);
            var sin = Mathf.Sin(radical);
            // inverse rotation matrix
            var x1 = cos * x + sin * y;
            var y1 = -sin * x + cos * y;
            
            float xv = Mathf.InverseLerp(OutOfBoundsUtils.minX, OutOfBoundsUtils.maxX, x1);
            float yv = Mathf.InverseLerp(OutOfBoundsUtils.minY, OutOfBoundsUtils.maxY, y1);
            vector = new Vector3(xv, yv, 0f);
            // vector = new Vector3(Mathf.Clamp(vector.x, 0f, 1f), Mathf.Clamp(vector.y, 0f, 1f), vector.z);

            return vector.x <= warningPercentage || vector.x >= 1f - warningPercentage ||
                   vector.y >= 1f - warningPercentage || vector.y <= warningPercentage;
        }

        internal static void CreateBorder()
        {
            // Reset OOB
            SetOOB(OutOfBoundsUtils.defaultX, OutOfBoundsUtils.defaultY, 0);
            
            // Move border to a worldSpace canvas
            OutOfBoundsUtils.border = UIHandler.instance.transform.Find("Canvas/Border").gameObject;
            var canvas = new GameObject("BorderCanvas").AddComponent<Canvas>();
            OutOfBoundsUtils.border.transform.SetParent(canvas.transform);
            OutOfBoundsUtils.border.transform.position = Vector3.zero;
            OutOfBoundsUtils.border.transform.localScale = new Vector3(0.037f, 0.037f, 0.037f);
            canvas.renderMode = RenderMode.WorldSpace;

            // make particles
            GameObject Particles = new GameObject("OutOfBoundsParticles", typeof(OutOfBoundsParticles));
        }
    }
}