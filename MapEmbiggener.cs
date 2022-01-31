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
    [BepInPlugin(MapEmbiggener.ModId, MapEmbiggener.ModName, "1.2.9")]
    [BepInProcess("Rounds.exe")]
    public class MapEmbiggener : BaseUnityPlugin
    {
        internal static readonly string[] stickFightObjsToIgnore = new string[] { "Real", "Chain", "PLatform", "Platform", "TreadMill", "Spike(Spike)", "SpikeBall"};
        internal static readonly string[] stickFightSpawnerObjs = new string[] {"(Pusher)(Clone)", "Box(Clone)(Clone)" };

        // array of object names which are NOT stickfightmaps objects, but contain the names of stickfightmaps objects above
        internal static readonly string[] falsePositiveNonStickFightObjs = new string[] { "MovingPlatform" };

        public static ConfigEntry<float> SizeConfig;
        public static ConfigEntry<bool> ChaosConfig;
        public static ConfigEntry<bool> SuddenDeathConfig;
        
        private struct NetworkEventType
        {
            public const string SyncModSettings = MapEmbiggener.ModId + "_Sync";
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
            MapEmbiggener.SizeConfig = this.Config.Bind("MapEmbiggener", "Size", 1f, "Size to scale maps to");
            MapEmbiggener.SuddenDeathConfig = this.Config.Bind("MapEmbiggener", "SuddenDeathMode", false, "Enable Sudden Death mode");
            MapEmbiggener.ChaosConfig = this.Config.Bind("MapEmbiggener", "ChaosMode", false, "Enable Chaos mode");

            new Harmony(MapEmbiggener.ModId).PatchAll();
            Unbound.RegisterHandshake(NetworkEventType.SyncModSettings, OnHandShakeCompleted);
            
            this.gameObject.AddComponent<OutOfBoundsUtils>();
            
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
        //     GUILayout.Space(10f);
        //     var angle = GUILayout.HorizontalSlider(OutOfBoundsUtils.angle, 0, 360, GUILayout.Width(300f));
        //     OutOfBoundsUtils.SetOOB(minX, maxX, minY, maxY, angle);
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

            Unbound.RegisterCredits(MapEmbiggener.ModName, new String[] {"Pykess (Code)", "Ascyst (Project creation)", "BossSloth (Customizable bounds)"}, new string[] { "github", "support pykess", "support ascyst", "support bosssloth" }, new string[] { "https://github.com/pdcook/MapEmbiggener", "https://ko-fi.com/pykess", "https://www.buymeacoffee.com/Ascyst", "https://www.buymeacoffee.com/BossSloth" });
            Unbound.RegisterHandshake(MapEmbiggener.ModId, OnHandShakeCompleted);
            Unbound.RegisterMenu(MapEmbiggener.ModName, () => { }, this.NewGUI, null, false);

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
            GameModeManager.AddHook(GameModeHooks.HookGameStart, this.ResetRotationDirection);
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
            MenuHandler.CreateSlider("Map Size Multiplier", menu, 60, 0.5f, 3f, MapEmbiggener.SizeConfig.Value, SliderChangedAction, out Slider slider);
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

            this.suddenDeathModeToggle = MenuHandler.CreateToggle(MapEmbiggener.SuddenDeathConfig.Value, "Sudden Death Mode", menu, suddenDeathModeToggleAction, 60).GetComponent<Toggle>();
            this.chaosModeToggle = MenuHandler.CreateToggle(MapEmbiggener.ChaosConfig.Value, "Chaos Mode", menu, chaosModeToggleAction, 60).GetComponent<Toggle>();

        }
        private IEnumerator SetZoomModes(IGameModeHandler gm, bool enable)
        {
            // disable zoom modes
            if (!enable)
            {
                MapEmbiggener.chaosMode = false;
                MapEmbiggener.suddenDeathMode = false;
                yield return this.ResetCamera(gm);
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
            yield return this.ResetCamera(gm);
            yield break;
        }

    }

}