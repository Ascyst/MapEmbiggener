using System;
using BepInEx;
using HarmonyLib;
using Photon.Pun;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;
using System.Collections;
using UnboundLib.Networking;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnboundLib.Utils.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MapEmbiggener
{
    public static class Interface
    {
        public static float GetCurrentSetSize()
        {
            return MapEmbiggener.setSize;
        }
        public static void ChangeOptions(float? size = null, bool? suddenDeath = null, bool? chaos = null, float? shrinkRate = null, bool zoomOnly = true, bool apply = true, ChangeUntil changeUntil = ChangeUntil.Forever, float duration = 0f)
        {
            float Size = size ?? MapEmbiggener.setSize;
            bool SuddenDeath = suddenDeath ?? MapEmbiggener.suddenDeathMode;
            bool Chaos = chaos ?? MapEmbiggener.chaosMode;
            float ShrinkRate = shrinkRate ?? MapEmbiggener.shrinkRate;

            MapEmbiggener.setSize = Size;
            MapEmbiggener.restoreSettingsOn = changeUntil;

            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC(typeof(MapEmbiggener), nameof(MapEmbiggener.SyncCurrentOptions), new object[] { Size, SuddenDeath, Chaos, ShrinkRate, changeUntil });
            }
            if (apply) { ApplyNewMapSize(Size, zoomOnly); }

            if (changeUntil == ChangeUntil.Custom && duration > 0f)
            {
                Unbound.Instance.StartCoroutine(Interface.WaitToRestore(duration));
            }

        }
        public static IEnumerator RestoreDefaults(bool zoomOnly = false, bool apply = true)
        {
            MapEmbiggener.OnHandShakeCompleted();
            if (apply) { ApplyNewMapSize(MapEmbiggener.settingsSetSize, zoomOnly); }
            yield return new WaitForSecondsRealtime(1f);
        }
        private static IEnumerator WaitToRestore(float duration)
        {
            float startTime = Time.time;
            while (Time.time < startTime + duration)
            {
                yield return null;
            }
            MapEmbiggener.OnHandShakeCompleted();
            ApplyNewMapSize(MapEmbiggener.settingsSetSize, false);
        }
        internal static void ApplyNewMapSize(float size, bool zoomOnly)
        {

            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC(typeof(Interface), nameof(RPCA_ApplyNewMapSize), new object[] { size, zoomOnly });
            }
        }
        [UnboundRPC]
        private static void RPCA_ApplyNewMapSize(float size, bool zoomOnly)
        {
            MapManager.instance.currentMap.Map.size = MapEmbiggener.defaultMapSize * size;

            if (zoomOnly) { return; }
            foreach (SpawnPoint spawnPoint in MapManager.instance.currentMap.Map.GetComponentsInChildren<SpawnPoint>())
            {
                spawnPoint.localStartPos *= size/MapEmbiggener.setSize;
            }
            MapManager.instance.currentMap.Map.transform.localScale *= size/MapEmbiggener.setSize;

            Unbound.Instance.ExecuteAfterFrames(2, () =>
            {
                foreach (Rigidbody2D rig in MapManager.instance.currentMap.Map.allRigs)
                {
                    // rescale physics objects UNLESS they have a movesequence component
                    // also UNLESS they are a crate from stickfight (Boss Sloth's stickfight maps mod)
                    // if they have a movesequence component then scale the points in that component

                    if (rig.gameObject.name.Contains("Real"))
                    {
                        rig.mass *= size / MapEmbiggener.setSize;
                        continue;
                    }

                    if (rig.gameObject.GetComponentInChildren<MoveSequence>() == null)
                    {
                        rig.transform.localScale *= size / MapEmbiggener.setSize;
                        rig.mass *= size / MapEmbiggener.setSize;
                    }
                    else
                    {

                        List<Vector2> newPos = new List<Vector2>() { };
                        foreach (Vector2 pos in rig.gameObject.GetComponentInChildren<MoveSequence>().positions)
                        {
                            newPos.Add(pos * size/MapEmbiggener.setSize);
                        }
                        rig.gameObject.GetComponentInChildren<MoveSequence>().positions = newPos.ToArray();
                        Traverse.Create(rig.gameObject.GetComponentInChildren<MoveSequence>()).Field("startPos").SetValue((Vector2)Traverse.Create(rig.gameObject.GetComponentInChildren<MoveSequence>()).Field("startPos").GetValue() * size/MapEmbiggener.setSize);
                    }
                }
                GameObject Rendering = UnityEngine.GameObject.Find("/Game/Visual/Rendering ");

                if (Rendering != null)
                {
                    foreach (Transform transform in Rendering.GetComponentsInChildren<Transform>(true))
                    {
                        transform.localScale = Vector3.one * UnityEngine.Mathf.Clamp(size/MapEmbiggener.setSize, 0.1f, 2f);
                    }
                }
            });
        }
        internal static IEnumerator BattleStart(IGameModeHandler gm)
        {
            if (MapEmbiggener.restoreSettingsOn == ChangeUntil.BattleStart)
            {
                yield return RestoreDefaults();
            }

            yield break;
        }
        internal static IEnumerator PointStart(IGameModeHandler gm)
        {
            if (MapEmbiggener.restoreSettingsOn == ChangeUntil.PointStart)
            {
                yield return RestoreDefaults();
            }

            yield break;
        }
        internal static IEnumerator PointEnd(IGameModeHandler gm)
        {
            if (MapEmbiggener.restoreSettingsOn == ChangeUntil.BattleEnd)
            {
                yield return RestoreDefaults();
            }

            yield break;
        }
        internal static IEnumerator RoundEnd(IGameModeHandler gm)
        {
            if (MapEmbiggener.restoreSettingsOn == ChangeUntil.RoundEnd)
            {
                yield return RestoreDefaults();
            }

            yield break;
        }
        internal static IEnumerator PickEnd(IGameModeHandler gm)
        {
            if (MapEmbiggener.restoreSettingsOn == ChangeUntil.PickEnd)
            {
                yield return RestoreDefaults();
            }

            yield break;
        }
        internal static IEnumerator GameEnd(IGameModeHandler gm)
        {
            if (MapEmbiggener.restoreSettingsOn == ChangeUntil.GameEnd)
            {
                yield return RestoreDefaults();
            }

            yield break;
        }
        public static Vector2 GetCameraPos()
        {
            GameObject MainCam = UnityEngine.GameObject.Find("/Game/Visual/Rendering /Shake/MainCamera");

            return MainCam.transform.position;
        }
        public static Quaternion GetCameraRot()
        {
            GameObject MainCam = UnityEngine.GameObject.Find("/Game/Visual/Rendering /Shake/MainCamera");

            return MainCam.transform.rotation;
        }
        internal static void MoveCamera(Vector3? pos = null, float? angle = null)
        {
            GameObject MainCam = UnityEngine.GameObject.Find("/Game/Visual/Rendering /Shake/MainCamera");
            GameObject LightingCam = UnityEngine.GameObject.Find("/Game/Visual/Rendering /Shake/Lighting/LightCamera");

            Vector3 Pos = pos ?? MainCam.transform.position;
            Quaternion rot;
            if (angle == null)
            {
                rot = MainCam.transform.rotation;
            }
            else
            {
                rot = Quaternion.Euler((new Vector3(0f, 0f, (float)angle)));
            }

            MainCam.transform.position = Pos;
            LightingCam.transform.position = Pos;
            MainCam.transform.rotation = rot;
            LightingCam.transform.rotation = rot;
        }

        public enum ChangeUntil
        {
            BattleStart,
            PointStart,
            BattleEnd,
            RoundEnd,
            PickEnd,
            GameEnd,
            Custom,
            Forever
        }
    }
}