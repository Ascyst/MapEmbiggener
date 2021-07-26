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
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(ModId, ModName, "1.0.0")]
    [BepInProcess("Rounds.exe")]
    public class MapEmbiggener : BaseUnityPlugin
    {
        private struct NetworkEventType
        {
            public const string SyncModSettings = ModId + "_Sync";
        }
        private const string ModId = "com.ascyst.rounds.mapembiggener";

        private const string ModName = "Map Embiggener";

        public static float setSize = 1.0f;
        public static bool suddenDeathMode = false;
        public static bool chaosMode = false;

        internal static readonly float shrinkRate = 0.998f;
        internal static readonly float shrinkDelay = 0.05f;

        private Toggle suddenDeathModeToggle;
        private Toggle chaosModeToggle;

        private void Awake()
        {
            new Harmony(ModId).PatchAll();
            Unbound.RegisterHandshake(NetworkEventType.SyncModSettings, OnHandShakeCompleted);
        }
        private void OnHandShakeCompleted()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC_Others(typeof(MapEmbiggener), nameof(SyncSettings), new object[] { MapEmbiggener.setSize, MapEmbiggener.suddenDeathMode, MapEmbiggener.chaosMode });
            }
        }

        [UnboundRPC]
        private static void SyncSettings(float setSize, bool suddenDeath, bool chaos)
        {
            MapEmbiggener.setSize = setSize;
            MapEmbiggener.suddenDeathMode = suddenDeath;
            MapEmbiggener.chaosMode = chaos;
        }
        private void Start()
        {
            Unbound.RegisterCredits(ModName, new String[] {"Ascyst (Project creation)", "Pykess (Code)"}, "github", "https://github.com/Ascyst/MapEmbiggener");
            Unbound.RegisterHandshake(ModId, OnHandShakeCompleted);
            Unbound.RegisterMenu(ModName, () => { }, NewGUI);

            GameModeManager.AddHook(GameModeHooks.HookPickStart, (gm) => this.StartPickPhaseCamera());
            GameModeManager.AddHook(GameModeHooks.HookPickEnd, (gm) => this.EndPickPhaseCamera());
        }
        private void NewGUI(GameObject menu)
        {


            MenuHandler.CreateText("Map Embiggener Options", menu, out TextMeshProUGUI _);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI warning, 30, true, Color.red);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            void SliderChangedAction(float val)
            {
                MapEmbiggener.setSize = val;
                if (val > 2f)
                {
                    warning.text = "warning: scaling maps beyond 2× can cause gameplay difficulties and visual glitches".ToUpper();
                }
                else if (val < 1f)
                {
                    warning.text = "warning: scaling maps below 1× can cause gameplay difficulties and visual glitches".ToUpper();
                }
                else
                {
                    warning.text = " ";
                }
            }
            MenuHandler.CreateSlider("Map Size Multiplier", menu, 60, 0.5f, 3f, setSize, SliderChangedAction, out UnityEngine.UI.Slider slider);
            void ResetButton()
            {
                slider.value = 1f;
                SliderChangedAction(1f);
            }
            MenuHandler.CreateButton("Reset Multiplier", menu, ResetButton, 30);
            void SuddenDeathModeCheckbox(bool flag)
            {
                MapEmbiggener.suddenDeathMode = flag;
                if (MapEmbiggener.chaosMode && MapEmbiggener.suddenDeathMode)
                {
                    MapEmbiggener.chaosMode = false;
                    chaosModeToggle.isOn = false;
                }
            }
            suddenDeathModeToggle = MenuHandler.CreateToggle(MapEmbiggener.suddenDeathMode, "Sudden Death Mode", menu, SuddenDeathModeCheckbox, 60).GetComponent<Toggle>();
            void ChaosModeCheckbox(bool flag)
            {
                MapEmbiggener.chaosMode = flag;
                if (MapEmbiggener.chaosMode && MapEmbiggener.suddenDeathMode)
                {
                    MapEmbiggener.suddenDeathMode = false;
                    suddenDeathModeToggle.isOn = false;
                }
            }
            chaosModeToggle = MenuHandler.CreateToggle(MapEmbiggener.chaosMode, "Chaos Mode", menu, ChaosModeCheckbox, 60).GetComponent<Toggle>();


        }
        private void DrawGUI()
        {
            GUILayout.Label("Current Map Size: x" + setSize);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Set Map Size To:");
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("x1.5"))
            {
                setSize = 1.5f;
            }
            if (GUILayout.Button("x2.0"))
            {
                setSize = 2.0f;
            }
            if (GUILayout.Button("Default"))
            {
                setSize = 1.0f;
            }
            GUILayout.EndHorizontal();

        }

        private IEnumerator StartPickPhaseCamera()
        {
            MapManager.instance.currentMap.Map.size /= MapEmbiggener.setSize;

            yield break;
        }
        private IEnumerator EndPickPhaseCamera()
        {
            MapManager.instance.currentMap.Map.size *= MapEmbiggener.setSize;

            yield return new WaitForSecondsRealtime(0.1f);

            yield break;
        }

    }

    [Serializable]
    [HarmonyPatch(typeof(Map),"Start")]
    class MapPatchStart
    {
        private static void Postfix(Map __instance)
        {
            __instance.GetComponentInChildren<SpawnPoint>().localStartPos *= MapEmbiggener.setSize;
            __instance.transform.localScale *= MapEmbiggener.setSize;
            __instance.size *= MapEmbiggener.setSize;

        }
    }

    [Serializable]
    [HarmonyPatch(typeof(Map), "StartMatch")]
    class MapPatchStartMatch
    {
        private static void Postfix(Map __instance)
        {
            Unbound.Instance.ExecuteAfterFrames(2, () =>
            {
                foreach (Rigidbody2D rig in __instance.allRigs)
                {
                    // rescale physics objects UNLESS they have a movesequence component
                    // also UNLESS they are a crate from stickfight (Boss Sloth's stickfight maps mod)
                    // if they have a movesequence component then scale the points in that component

                    if (rig.gameObject.name.Contains("Real"))
                    {
                        rig.mass *= MapEmbiggener.setSize;
                        continue;
                    }

                    if (rig.gameObject.GetComponentInChildren<MoveSequence>() == null)
                    { 
                        rig.transform.localScale *= MapEmbiggener.setSize;
                        rig.mass *= MapEmbiggener.setSize;
                    }
                    else
                    {
                        
                        List<Vector2> newPos = new List<Vector2>() { };
                        foreach (Vector2 pos in rig.gameObject.GetComponentInChildren<MoveSequence>().positions)
                        {
                            newPos.Add(pos * MapEmbiggener.setSize);
                        }
                        rig.gameObject.GetComponentInChildren<MoveSequence>().positions = newPos.ToArray();
                        Traverse.Create(rig.gameObject.GetComponentInChildren<MoveSequence>()).Field("startPos").SetValue((Vector2)Traverse.Create(rig.gameObject.GetComponentInChildren<MoveSequence>()).Field("startPos").GetValue() * MapEmbiggener.setSize);
                    }
                }
                GameObject Rendering = UnityEngine.GameObject.Find("/Game/Visual/Rendering ");

                if (Rendering != null)
                {
                    foreach (Transform transform in Rendering.GetComponentsInChildren<Transform>(true))
                    {
                        transform.localScale = Vector3.one * MapEmbiggener.setSize;
                    }
                }

                Unbound.Instance.StartCoroutine(GameModes(__instance));
            });
        }
        private static float timerStart;
        private static System.Collections.IEnumerator GameModes(Map instance)
        {
            timerStart = Time.time;
            while (instance.enabled)
            {
                if ((float)Traverse.Create(instance).Field("counter").GetValue() > 2f && instance.size > 1f && (MapEmbiggener.chaosMode || (MapEmbiggener.suddenDeathMode && CountPlayersAlive() <= 2)))
                {
                    if (Time.time > timerStart + MapEmbiggener.shrinkDelay)
                    {
                        timerStart = Time.time;
                        instance.size *= MapEmbiggener.shrinkRate;
                    }
                }
                yield return null;
            }
            yield break;
        }
        private static int CountPlayersAlive()
        {
            return PlayerManager.instance.players.Where(p => !p.data.dead).Count();
        }
    }

    [Serializable]
    [HarmonyPatch(typeof(OutOfBoundsHandler), "LateUpdate")]
    class OutOfBoundsHandlerPatchLateUpdate
    {
        private static bool Prefix(OutOfBoundsHandler __instance)
        {
            CharacterData data = (CharacterData)Traverse.Create(__instance).Field("data").GetValue();
            float warningPercentage = (float)Traverse.Create(__instance).Field("warningPercentage").GetValue();

            if (!data)
            {
                UnityEngine.GameObject.Destroy(__instance.gameObject);
                return false; // skip the original (BAD IDEA)
            }
            if (!(bool)Traverse.Create(data.playerVel).Field("simulated").GetValue())
            {
                return false; // skip the original (BAD IDEA)
            }
            if (!data.isPlaying)
            {
                return false; // skip the original (BAD IDEA)
            }
            /*
            float x = Mathf.InverseLerp(-35.56f, 35.56f, data.transform.position.x);
            float y = Mathf.InverseLerp(-20f, 20f, data.transform.position.y);
            Vector3 vector = new Vector3(x, y, 0f);
            vector = new Vector3(Mathf.Clamp(vector.x, 0f, 1f), Mathf.Clamp(vector.y, 0f, 1f), vector.z);
            */
            Vector3 vector = MainCam.instance.transform.GetComponent<Camera>().WorldToScreenPoint(new Vector3(data.transform.position.x, data.transform.position.y, 0f));

            vector.x /= (float)Screen.width;
            vector.y /= (float)Screen.height;

            vector = new Vector3(Mathf.Clamp01(vector.x), Mathf.Clamp01(vector.y), 0f);

            Traverse.Create(__instance).Field("almostOutOfBounds").SetValue(false);
            Traverse.Create(__instance).Field("outOfBounds").SetValue(false);
            if (vector.x <= 0f || vector.x >= 1f || vector.y >= 1f || vector.y <= 0f)
            {
                Traverse.Create(__instance).Field("outOfBounds").SetValue(true);
            }
            else if (vector.x < warningPercentage || vector.x > 1f - warningPercentage || vector.y > 1f - warningPercentage || vector.y < warningPercentage)
            {
                Traverse.Create(__instance).Field("almostOutOfBounds").SetValue(true);
                if (vector.x < warningPercentage)
                {
                    vector.x = 0f;
                }
                if (vector.x > 1f - warningPercentage)
                {
                    vector.x = 1f;
                }
                if (vector.y < warningPercentage)
                {
                    vector.y = 0f;
                }
                if (vector.y > 1f - warningPercentage)
                {
                    vector.y = 1f;
                }
            }
            Traverse.Create(__instance).Field("counter").SetValue((float)Traverse.Create(__instance).Field("counter").GetValue() + TimeHandler.deltaTime);
            if ((bool)Traverse.Create(__instance).Field("almostOutOfBounds").GetValue() && !data.dead)
            {
                __instance.transform.position = (Vector3)typeof(OutOfBoundsHandler).InvokeMember("GetPoint",
                                                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                                                    BindingFlags.NonPublic, null, __instance, new object[] { vector });
                __instance.transform.rotation = Quaternion.LookRotation(Vector3.forward, -(data.transform.position - __instance.transform.position));
                if ((float)Traverse.Create(__instance).Field("counter").GetValue() > 0.1f)
                {
                    Traverse.Create(__instance).Field("counter").SetValue(0f);
                    __instance.warning.Play();
                }
            }
            if ((bool)Traverse.Create(__instance).Field("outOfBounds").GetValue() && !data.dead)
            {
                data.sinceGrounded = 0f;
                __instance.transform.position = (Vector3)typeof(OutOfBoundsHandler).InvokeMember("GetPoint",
                                                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                                                    BindingFlags.NonPublic, null, __instance, new object[] { vector });
                __instance.transform.rotation = Quaternion.LookRotation(Vector3.forward, -(data.transform.position - __instance.transform.position));
                if ((float)Traverse.Create(__instance).Field("counter").GetValue() > 0.1f && data.view.IsMine)
                {
                    Traverse.Create(__instance).Field("counter").SetValue(0f);
                    if (data.block.IsBlocking())
                    {
                        ((ChildRPC)Traverse.Create(__instance).Field("rpc").GetValue()).CallFunction("ShieldOutOfBounds");
                        Traverse.Create(data.playerVel).Field("velocity").SetValue((Vector2)Traverse.Create(data.playerVel).Field("velocity").GetValue() * 0f);
                        data.healthHandler.CallTakeForce(__instance.transform.up * 400f * (MapManager.instance.currentMap.Map.size / 20f) * (float)Traverse.Create(data.playerVel).Field("mass").GetValue(), ForceMode2D.Impulse, false, true, 0f);
                        data.transform.position = __instance.transform.position;
                        return false; // skip the original (BAD IDEA)
                    }
                    ((ChildRPC)Traverse.Create(__instance).Field("rpc").GetValue()).CallFunction("OutOfBounds");
                    data.healthHandler.CallTakeForce(__instance.transform.up * 200f * (MapManager.instance.currentMap.Map.size/20f) * (float)Traverse.Create(data.playerVel).Field("mass").GetValue(), ForceMode2D.Impulse, false, true, 0f);
                    data.healthHandler.CallTakeDamage(51f * __instance.transform.up, data.transform.position, null, null, true);
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
            Vector3 result = MainCam.instance.transform.GetComponent<Camera>().ScreenToWorldPoint(new Vector3(p.x * (float)Screen.width, p.y * (float)Screen.height, 0f));
            __result = new Vector3(result.x, result.y, 0f);

            return false; // skip the original (BAD IDEA)
        }
    }

}