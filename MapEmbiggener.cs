using System;
using BepInEx;
using HarmonyLib;
using Photon.Pun;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;
using System.Collections;
using UnboundLib.Networking;
using System.Linq;
using System.Collections.Generic;
using UnboundLib.Utils.UI;
using TMPro;
using UnityEngine.UI;
using BepInEx.Configuration;

namespace MapEmbiggener
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(ModId, ModName, "1.2.9")]
    [BepInProcess("Rounds.exe")]
    public class MapEmbiggener : BaseUnityPlugin
    {
        internal static readonly string[] stickFightObjsToIgnore = new string[] { "Real", "Chain", "Platform", "TreadMill", "Spike(Spike)", "SpikeBall"};
        internal static readonly string[] stickFightSpawnerObjs = new string[] {"(Pusher)(Clone)", "Box(Clone)(Clone)" };

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
        internal static float zoomShrink = 1;

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
            
            On.MainMenuHandler.Awake += (orig, self) =>
            {
                orig(self);
                this.ExecuteAfterSeconds(0.5f, () =>
                {
                    // Create the bounds border
                    OutOfBoundsUtils.CreateBorder();
                });
            };
        }

        // Uncomment this to DEBUG the bounds with sliders
        
        // private void OnGUI()
        // {
        //     var minX = GUILayout.HorizontalSlider(OutOfBoundsUtils.minX, -50, 50, GUILayout.Width(300f));
        //     var maxX = GUILayout.HorizontalSlider(OutOfBoundsUtils.maxX, -50, 50, GUILayout.Width(300f));
        //     var minY = GUILayout.HorizontalSlider(OutOfBoundsUtils.minY, -40, 40, GUILayout.Width(300f));
        //     var maxY = GUILayout.HorizontalSlider(OutOfBoundsUtils.maxY, -40, 40, GUILayout.Width(300f));
        //     OutOfBoundsUtils.SetOOB(minX, maxX, minY, maxY);
        // }

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
            Unbound.RegisterMenu(ModName, () => { }, NewGUI, null, false);

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

            // hooks for OOB patch
            GameModeManager.AddHook(GameModeHooks.HookPointStart, (gm) => OutOfBoundsUtils.SetOOBEnabled(true));
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, (gm) => OutOfBoundsUtils.SetOOBEnabled(false));
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
            OutOfBoundsUtils.SetOOB(OutOfBoundsUtils.defaultX, OutOfBoundsUtils.defaultY);

            yield return new WaitForSecondsRealtime(1.5f);

            yield break;
        }
        private IEnumerator EndPickPhaseCamera()
        {
            MapManager.instance.currentMap.Map.size = MapEmbiggener.defaultMapSize * MapEmbiggener.settingsSetSize;
            OutOfBoundsUtils.SetOOB(OutOfBoundsUtils.defaultX * MapEmbiggener.settingsSetSize, OutOfBoundsUtils.defaultY * MapEmbiggener.settingsSetSize);

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
            MapEmbiggener.zoomShrink = 1;
            OutOfBoundsUtils.SetOOB(OutOfBoundsUtils.defaultX * MapEmbiggener.settingsSetSize, OutOfBoundsUtils.defaultY * MapEmbiggener.settingsSetSize);
        }
    }

    public static class StringExtension
    {
        public static bool ContainsAny(this string str, params string[] substrings)
        {
            foreach (string substr in substrings)
            {
                if (str.Contains(substr))
                {
                    return true;
                }
            }
            return false;
        }
    }

    internal static class GameObjectExtension
    {
        internal static bool IsStickFightObject(this GameObject obj, int depth = 2)
        {
            if (obj == null) { return false; }
            Transform transform = obj.transform;
            for (int i = 0; i < depth; i++)
            {
                if (transform == null) { break; }

                if (transform.gameObject.name.ContainsAny(MapEmbiggener.stickFightObjsToIgnore)) { return true; }

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
    // patch for special stickfightmaps objects
    [Serializable]
    [HarmonyPatch(typeof(RemoveAfterSeconds),"Start")]
    class RemoveAfterSecondsPatchStart
    {
        private static void Prefix(RemoveAfterSeconds __instance)
        {
            // if this object is a stickfightmaps spawner object, then scale it up and increase its timer
            if (__instance.gameObject.name.ContainsAny(MapEmbiggener.stickFightSpawnerObjs) && !__instance.gameObject.name.ContainsAny(MapEmbiggener.stickFightObjsToIgnore))
            {
                __instance.seconds *= MapEmbiggener.setSize;
                foreach (Rigidbody2D rig in __instance.gameObject.GetComponentsInChildren<Rigidbody2D>())
                {
                    //rig.mass *= MapEmbiggener.setSize;
                    rig.transform.localScale *= MapEmbiggener.setSize;
                }
            }
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
                // if a stickfightmaps object has any particlesystem components, then scale those up
                foreach (ParticleSystem part in __instance.gameObject.GetComponentsInChildren<ParticleSystem>())
                {
                    if (part.gameObject.IsStickFightObject())
                    {
                        part.transform.localScale *= MapEmbiggener.setSize;
                    }
                }
                foreach (Rigidbody2D rig in __instance.allRigs)
                {

                    // if its a MapsExtended object, mess with the position only on the host's side
                    if (rig.gameObject.IsMapsExtObject())
                    {
                        if (rig.gameObject.GetComponent<PhotonView>().IsMine)
                        {
                            rig.gameObject.transform.position = new Vector3(MapEmbiggener.setSize * rig.gameObject.transform.position.x, rig.gameObject.transform.position.y * MapEmbiggener.setSize, rig.gameObject.transform.position.z);
                        }
                    }

                    // rescale physics objects UNLESS they have a movesequence component
                    // also UNLESS they are a crate from stickfight (Boss Sloth's stickfight maps mod)
                    // if they have a movesequence component then scale the points in that component

                    if (rig.gameObject.IsStickFightObject())
                    {

                        if (rig.gameObject.name.Contains("PLatform"))
                        {
                            rig.transform.localScale /= MapEmbiggener.setSize;
                        }

                        rig.mass *= MapEmbiggener.setSize;

                        // increase the strength of chains
                        if (rig.gameObject.name.Contains("Chain"))
                        {
                            if (rig.gameObject.GetComponent<ForceMultiplier>() != null) { rig.gameObject.GetComponent<ForceMultiplier>().multiplier *= MapEmbiggener.setSize; }
                            foreach (DistanceJoint2D joint in rig.gameObject.GetComponents<DistanceJoint2D>())
                            {
                                joint.distance *= MapEmbiggener.setSize;
                            }
                        }
                        continue;
                    }

                    if (rig.gameObject.GetComponentInChildren<MoveSequence>() == null)
                    {
                        rig.mass *= MapEmbiggener.setSize;

                        // if its a maps extended object, then only change its size on the host client
                        if (rig.gameObject.IsMapsExtObject() && !rig.gameObject.GetComponent<PhotonView>().IsMine)
                        {
                            continue;
                        }

                        rig.transform.localScale *= MapEmbiggener.setSize;
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
            if (instance == null) { yield break; }
            timerStart = Time.time;
            rotTimerStart = Time.time;
            while (instance != null && instance.enabled)
            {
                if (instance != null && (float)instance.GetFieldValue("counter") > 2f && MapEmbiggener.zoomShrink > 0.3f && (MapEmbiggener.chaosMode || (MapEmbiggener.suddenDeathMode && CountPlayersAlive() <= 2)))
                {
                    if (instance != null && Time.time > timerStart + MapEmbiggener.shrinkDelay)
                    {
                        timerStart = Time.time;
                        MapEmbiggener.zoomShrink *= MapEmbiggener.shrinkRate;
                        instance.size = MapEmbiggener.defaultMapSize * MapEmbiggener.settingsSetSize *
                                         MapEmbiggener.zoomShrink;
                        OutOfBoundsUtils.SetOOB(
                            OutOfBoundsUtils.defaultX * MapEmbiggener.settingsSetSize * MapEmbiggener.zoomShrink + 0.15f,
                            OutOfBoundsUtils.defaultY * MapEmbiggener.settingsSetSize * MapEmbiggener.zoomShrink + 0.15f);
                        
                    }
                }
                if (instance != null && (float)instance.GetFieldValue("counter") > 2f && MapEmbiggener.zoomShrink > 0.3f && MapEmbiggener.chaosMode)
                {
                    if (instance != null && Time.time > rotTimerStart + MapEmbiggener.rotationDelay)
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
            return PlayerManager.instance.players.Count(p => !p.data.dead);
        }
    }

}