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
using BepInEx.Configuration;

namespace MapEmbiggener
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(ModId, ModName, "1.2.2")]
    [BepInProcess("Rounds.exe")]
    public class MapEmbiggener : BaseUnityPlugin
    {
        public static ConfigEntry<float> SizeConfig;
        public static ConfigEntry<bool> ChaosConfig;
        public static ConfigEntry<bool> SuddenDeathConfig;

        private struct NetworkEventType
        {
            public const string SyncModSettings = ModId + "_Sync";
        }
        private const string ModId = "pykess.rounds.plugins.mapembiggener";

        private const string ModName = "Map Embiggener";

        internal static float setSize = 1.0f;
        internal static bool suddenDeathMode = false;
        internal static bool chaosMode = false;

        internal static float settingsSetSize = 1.0f;
        internal static bool settingsSuddenDeathMode = false;
        internal static bool settingsChaosMode = false;

        internal static float shrinkRate;

        internal static readonly float rotationRate = 0.1f;
        internal static float rotationDirection = 1f;
        internal static readonly float rotationDelay = 0.05f;
        private static readonly float defaultShrinkRate = 0.998f;
        internal static readonly float shrinkDelay = 0.05f;

        internal static float defaultMapSize;

        internal static Interface.ChangeUntil restoreSettingsOn;

        private Toggle suddenDeathModeToggle;
        private Toggle chaosModeToggle;

        private void Awake()
        {
            MapEmbiggener.shrinkRate = MapEmbiggener.defaultShrinkRate;
            MapEmbiggener.restoreSettingsOn = Interface.ChangeUntil.Forever;

            // bind configs with BepInEx
            SizeConfig = Config.Bind("MapEmbiggener", "Size", 1f, "Size to scale maps to");
            SuddenDeathConfig = Config.Bind("MapEmbiggener", "SuddenDeathMode", false, "Enable Sudden Death mode");
            ChaosConfig = Config.Bind("MapEmbiggener", "ChaosMode", false, "Enable Chaos mode");

            new Harmony(ModId).PatchAll();
            Unbound.RegisterHandshake(NetworkEventType.SyncModSettings, OnHandShakeCompleted);
        }
        internal static void OnHandShakeCompleted()
        {
            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)
            {
                MapEmbiggener.restoreSettingsOn = Interface.ChangeUntil.Forever;

                NetworkingManager.RPC_Others(typeof(MapEmbiggener), nameof(SyncSettings), new object[] { MapEmbiggener.settingsSetSize, MapEmbiggener.settingsSuddenDeathMode, MapEmbiggener.settingsChaosMode });
                NetworkingManager.RPC(typeof(MapEmbiggener), nameof(SyncCurrentOptions), new object[] { MapEmbiggener.settingsSetSize, MapEmbiggener.settingsSuddenDeathMode, MapEmbiggener.settingsChaosMode, MapEmbiggener.defaultShrinkRate, MapEmbiggener.restoreSettingsOn });
            }
        }

        [UnboundRPC]
        private static void SyncSettings(float setSize, bool suddenDeath, bool chaos)
        {
            MapEmbiggener.settingsSetSize = setSize;
            MapEmbiggener.settingsSuddenDeathMode = suddenDeath;
            MapEmbiggener.settingsChaosMode = chaos;
        }
        [UnboundRPC]
        internal static void SyncCurrentOptions(float setSize, bool suddenDeath, bool chaos, float rate, Interface.ChangeUntil restore)
        {
            MapEmbiggener.setSize = setSize;
            MapEmbiggener.suddenDeathMode = suddenDeath;
            MapEmbiggener.chaosMode = chaos;
            MapEmbiggener.shrinkRate = rate;
            MapEmbiggener.restoreSettingsOn = restore;
        }
        private void Start()
        {
            // load settings
            MapEmbiggener.settingsSetSize = MapEmbiggener.SizeConfig.Value;
            MapEmbiggener.settingsSuddenDeathMode = MapEmbiggener.SuddenDeathConfig.Value;
            MapEmbiggener.settingsChaosMode = MapEmbiggener.ChaosConfig.Value;
            MapEmbiggener.setSize = MapEmbiggener.SizeConfig.Value;
            MapEmbiggener.suddenDeathMode = MapEmbiggener.SuddenDeathConfig.Value;
            MapEmbiggener.chaosMode = MapEmbiggener.ChaosConfig.Value;

            Unbound.RegisterCredits(ModName, new String[] {"Pykess (Code)", "Ascyst (Project creation)"}, new string[] { "github", "buy pykess a coffee", "buy ascyst a coffee" }, new string[] { "https://github.com/Ascyst/MapEmbiggener", "https://www.buymeacoffee.com/Pykess", "https://www.buymeacoffee.com/Ascyst" });
            Unbound.RegisterHandshake(ModId, OnHandShakeCompleted);
            Unbound.RegisterMenu(ModName, () => { }, NewGUI, null, true);

            // disable zooming during entire pick phase
            GameModeManager.AddHook(GameModeHooks.HookPickStart, (gm) => this.SetZoomModes(gm, false));
            GameModeManager.AddHook(GameModeHooks.HookPlayerPickStart, (gm) => this.SetZoomModes(gm, false));
            // re-enable zooming when point starts
            GameModeManager.AddHook(GameModeHooks.HookPointStart, (gm) => this.SetZoomModes(gm, true));

            // set camera zoom level for pick phase
            GameModeManager.AddHook(GameModeHooks.HookPickStart, (gm) => this.StartPickPhaseCamera());
            GameModeManager.AddHook(GameModeHooks.HookPickEnd, (gm) => this.EndPickPhaseCamera());

            GameModeManager.AddHook(GameModeHooks.HookPointEnd, this.FlipRotationDirection);

            GameModeManager.AddHook(GameModeHooks.HookPickStart, this.ResetCamera);
            // reset camera on point end so that players next round aren't spawned OOB
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, (gm) => this.ResetCameraAfter(gm, 1f));

            GameModeManager.AddHook(GameModeHooks.HookPickEnd, Interface.PickEnd);
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, Interface.PointEnd);
            GameModeManager.AddHook(GameModeHooks.HookGameEnd, Interface.GameEnd);
            GameModeManager.AddHook(GameModeHooks.HookGameStart, ResetRotationDirection);
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, Interface.RoundEnd);
            GameModeManager.AddHook(GameModeHooks.HookBattleStart, Interface.BattleStart);
            GameModeManager.AddHook(GameModeHooks.HookPointStart, Interface.PointStart);
        }
        private IEnumerator ResetRotationDirection(IGameModeHandler gm)
        {
            MapEmbiggener.rotationDirection = 1f;
            yield break;
        }
        private void NewGUI(GameObject menu)
        {
            MenuHandler.CreateText("Map Embiggener Options", menu, out TextMeshProUGUI _);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI warning, 30, true, Color.red);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            void SliderChangedAction(float val)
            {
                MapEmbiggener.settingsSetSize = val;
                MapEmbiggener.SizeConfig.Value = val;
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
                OnHandShakeCompleted();
            }
            MenuHandler.CreateSlider("Map Size Multiplier", menu, 60, 0.5f, 3f, SizeConfig.Value, SliderChangedAction, out UnityEngine.UI.Slider slider);
            void ResetButton()
            {
                slider.value = 1f;
                SliderChangedAction(1f);
                OnHandShakeCompleted();
            }
            MenuHandler.CreateButton("Reset Multiplier", menu, ResetButton, 30);
            void suddenDeathModeToggleAction(bool flag)
            {
                MapEmbiggener.SuddenDeathConfig.Value = flag;
                MapEmbiggener.suddenDeathMode = MapEmbiggener.SuddenDeathConfig.Value;
                MapEmbiggener.settingsSuddenDeathMode = MapEmbiggener.SuddenDeathConfig.Value;
                OnHandShakeCompleted();
            }
            void chaosModeToggleAction(bool flag)
            {
                MapEmbiggener.ChaosConfig.Value = flag;
                MapEmbiggener.chaosMode = MapEmbiggener.ChaosConfig.Value;
                MapEmbiggener.settingsChaosMode = MapEmbiggener.ChaosConfig.Value;
                OnHandShakeCompleted();
            }
            suddenDeathModeToggle = MenuHandler.CreateToggle(MapEmbiggener.SuddenDeathConfig.Value, "Sudden Death Mode", menu, suddenDeathModeToggleAction, 60).GetComponent<Toggle>();
            chaosModeToggle = MenuHandler.CreateToggle(MapEmbiggener.ChaosConfig.Value, "Chaos Mode", menu, chaosModeToggleAction, 60).GetComponent<Toggle>();

        }
        private IEnumerator SetZoomModes(IGameModeHandler gm, bool enable)
        {
            // disable zoom modes
            if (!enable)
            {
                MapEmbiggener.chaosMode = false;
                MapEmbiggener.suddenDeathMode = false;
                yield return ResetCamera(gm);
            }
            // restore settings
            else
            {
                MapEmbiggener.chaosMode = MapEmbiggener.settingsChaosMode;
                MapEmbiggener.suddenDeathMode = MapEmbiggener.settingsSuddenDeathMode;
            }

            yield break;
        }
        private IEnumerator StartPickPhaseCamera()
        {
            MapManager.instance.currentMap.Map.size = MapEmbiggener.defaultMapSize;

            yield break;
        }
        private IEnumerator EndPickPhaseCamera()
        {
            MapManager.instance.currentMap.Map.size = MapEmbiggener.defaultMapSize * MapEmbiggener.settingsSetSize;

            yield return new WaitForSecondsRealtime(0.1f);

            yield break;
        }

        private IEnumerator FlipRotationDirection(IGameModeHandler gm)
        {
            MapEmbiggener.rotationDirection *= -1f;
            yield break;
        }
        private IEnumerator ResetCamera(IGameModeHandler gm)
        {

            Interface.MoveCamera(new Vector3(0f, 0f, -100f), 0f);

            yield break;
        }
        private IEnumerator ResetCameraAfter(IGameModeHandler gm, float delay)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            yield return ResetCamera(gm);
            yield break;
        }

    }

    [Serializable]
    [HarmonyPatch(typeof(Map),"Start")]
    class MapPatchStart
    {
        private static void Postfix(Map __instance)
        {
            MapEmbiggener.defaultMapSize = __instance.size;

            foreach (SpawnPoint spawnPoint in __instance.GetComponentsInChildren<SpawnPoint>())
            {
                spawnPoint.localStartPos *= MapEmbiggener.setSize;
            }
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
                        transform.localScale = Vector3.one * UnityEngine.Mathf.Clamp(MapEmbiggener.setSize, 0.1f, 2f);
                    }
                }

                Unbound.Instance.StartCoroutine(GameModes(__instance));
            });
        }
        private static float timerStart;
        private static float rotTimerStart;
        private static System.Collections.IEnumerator GameModes(Map instance)
        {
            timerStart = Time.time;
            rotTimerStart = Time.time;
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
                if ((float)Traverse.Create(instance).Field("counter").GetValue() > 2f && instance.size > 1f && MapEmbiggener.chaosMode)
                {
                    if (Time.time > rotTimerStart + MapEmbiggener.rotationDelay)
                    {
                        rotTimerStart = Time.time;
                        Vector3 currentRot = Interface.GetCameraRot().eulerAngles;
                        Interface.MoveCamera(angle: currentRot.z + MapEmbiggener.rotationDirection * MapEmbiggener.rotationRate);
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