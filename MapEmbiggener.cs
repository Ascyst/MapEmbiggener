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
using MapEmbiggener.UI;
using MapEmbiggener.Controllers;

namespace MapEmbiggener
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(MapEmbiggener.ModId, MapEmbiggener.ModName, "2.0.0")]
    [BepInProcess("Rounds.exe")]
    public class MapEmbiggener : BaseUnityPlugin
    {
        internal static readonly string[] stickFightObjsToIgnore = new string[] { "Real", "Chain", "PLatform", "Platform", "TreadMill", "Spike(Spike)", "SpikeBall"};
        internal static readonly string[] stickFightSpawnerObjs = new string[] {"(Pusher)(Clone)", "Box(Clone)(Clone)" };

        // array of object names which are NOT stickfightmaps objects, but contain the names of stickfightmaps objects above
        internal static readonly string[] falsePositiveNonStickFightObjs = new string[] { "MovingPlatform" };

        public static ConfigEntry<float> SizeConfig;
        public static ConfigEntry<bool> ChaosConfig;
        public static ConfigEntry<bool> ClassicChaosConfig;
        public static ConfigEntry<bool> SuddenDeathConfig;

        internal static MapEmbiggener instance;
        
        private struct NetworkEventType
        {
            public const string SyncModSettings = MapEmbiggener.ModId + "_Sync";
        }
        private const string ModId = "pykess.rounds.plugins.mapembiggener";

        private const string ModName = "Map Embiggener";

        internal static float setSize = 1.0f;
        internal static bool suddenDeathMode { get; private set; } = false;
        internal static bool chaosMode { get; private set; } = false;
        internal static bool chaosModeClassic { get; private set; } = false;

        internal static float defaultMapSize;

#if DEBUG
        internal static readonly bool DEBUG = true;
#else
        internal static readonly bool DEBUG = false;
#endif

        private Toggle suddenDeathModeToggle;
        private Toggle chaosModeToggle;
        private Toggle chaosModeClassicToggle;

        private void Awake()
        {

            MapEmbiggener.instance = this;

            // bind configs with BepInEx
            MapEmbiggener.SizeConfig = this.Config.Bind("MapEmbiggener", "Size", 1f, "Size to scale maps to");
            MapEmbiggener.SuddenDeathConfig = this.Config.Bind("MapEmbiggener", "SuddenDeathMode", false, "Enable Sudden Death mode");
            MapEmbiggener.ChaosConfig = this.Config.Bind("MapEmbiggener", "ChaosMode", false, "Enable Chaos mode");
            MapEmbiggener.ClassicChaosConfig = this.Config.Bind("MapEmbiggener", "ChaosModeClassic", false, "Enable Chaos Mode Classic");

            new Harmony(MapEmbiggener.ModId).PatchAll();
            
            this.gameObject.AddComponent<OutOfBoundsUtils>();
            this.gameObject.AddComponent<ControllerManager>().Init();
            
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

        private void Start()
        {
            // load settings
            MapEmbiggener.setSize = MapEmbiggener.SizeConfig.Value;
            MapEmbiggener.suddenDeathMode = MapEmbiggener.SuddenDeathConfig.Value;
            MapEmbiggener.chaosMode = MapEmbiggener.ChaosConfig.Value;
            MapEmbiggener.chaosModeClassic = MapEmbiggener.ClassicChaosConfig.Value;

            Unbound.RegisterCredits(MapEmbiggener.ModName, new String[] {"Pykess", "Ascyst (Project creation)", "BossSloth (Customizable bounds)"}, new string[] { "github", "support pykess", "support ascyst", "support bosssloth" }, new string[] { "https://github.com/pdcook/MapEmbiggener", "https://ko-fi.com/pykess", "https://www.buymeacoffee.com/Ascyst", "https://www.buymeacoffee.com/BossSloth" });
            Unbound.RegisterMenu(MapEmbiggener.ModName, () => { }, this.NewGUI, null, false);

            // hooks for OOB patch
            GameModeManager.AddHook(GameModeHooks.HookPointStart, (gm) => OutOfBoundsUtils.SetOOBEnabled(true));
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, (gm) => OutOfBoundsUtils.SetOOBEnabled(false));

            // hooks for controllermanager
            GameModeManager.AddHook(GameModeHooks.HookInitStart, ControllerManager.OnInitStart);
            GameModeManager.AddHook(GameModeHooks.HookInitEnd, ControllerManager.OnInitEnd);
            GameModeManager.AddHook(GameModeHooks.HookGameStart, ControllerManager.OnGameStart);
            GameModeManager.AddHook(GameModeHooks.HookGameEnd, ControllerManager.OnGameEnd);
            GameModeManager.AddHook(GameModeHooks.HookRoundStart, ControllerManager.OnRoundStart);
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, ControllerManager.OnRoundEnd);
            GameModeManager.AddHook(GameModeHooks.HookPointStart, ControllerManager.OnPointStart);
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, ControllerManager.OnPointEnd);
            GameModeManager.AddHook(GameModeHooks.HookBattleStart, ControllerManager.OnBattleStart);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, ControllerManager.OnPickStart);
            GameModeManager.AddHook(GameModeHooks.HookPickEnd, ControllerManager.OnPickEnd);
            GameModeManager.AddHook(GameModeHooks.HookPlayerPickStart, ControllerManager.OnPlayerPickStart);
            GameModeManager.AddHook(GameModeHooks.HookPlayerPickEnd, ControllerManager.OnPlayerPickEnd);
        }

        private void NewGUI(GameObject menu)
        {
            MenuHandler.CreateText("Map Embiggener Options", menu, out TextMeshProUGUI _);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            void SliderChangedAction(float val)
            {
                MapEmbiggener.SizeConfig.Value = val;
                MapEmbiggener.setSize = val;
            }
            MenuHandler.CreateSlider("Map Size Multiplier", menu, 60, 0.5f, 3f, MapEmbiggener.SizeConfig.Value, SliderChangedAction, out Slider slider);
            void ResetButton()
            {
                slider.value = 1f;
                SliderChangedAction(1f);
            }
            MenuHandler.CreateButton("Reset Multiplier", menu, ResetButton, 30);
            void suddenDeathModeToggleAction(bool flag)
            {
                MapEmbiggener.SuddenDeathConfig.Value = flag;
                MapEmbiggener.suddenDeathMode = MapEmbiggener.SuddenDeathConfig.Value;
                if (flag)
                {
                    chaosModeToggle.isOn = false;
                    chaosModeToggleAction(false);
                    chaosModeClassicToggle.isOn = false;
                    chaosModeClassicToggleAction(false);
                }
            }
            void chaosModeToggleAction(bool flag)
            {
                MapEmbiggener.ChaosConfig.Value = flag;
                MapEmbiggener.chaosMode = MapEmbiggener.ChaosConfig.Value;
                if (flag)
                {
                    suddenDeathModeToggle.isOn = false;
                    suddenDeathModeToggleAction(false);
                    chaosModeClassicToggle.isOn = false;
                    chaosModeClassicToggleAction(false);
                }
            }
            void chaosModeClassicToggleAction(bool flag)
            {
                MapEmbiggener.ClassicChaosConfig.Value = flag;
                MapEmbiggener.chaosModeClassic = MapEmbiggener.ClassicChaosConfig.Value;
                if (flag)
                {
                    suddenDeathModeToggle.isOn = false;
                    suddenDeathModeToggleAction(false);
                    chaosModeToggle.isOn = false;
                    chaosModeToggleAction(false);
                }
            }

            this.suddenDeathModeToggle = MenuHandler.CreateToggle(MapEmbiggener.SuddenDeathConfig.Value, "Sudden Death Mode", menu, suddenDeathModeToggleAction, 60).GetComponent<Toggle>();
            this.chaosModeToggle = MenuHandler.CreateToggle(MapEmbiggener.ChaosConfig.Value, "Chaos Mode", menu, chaosModeToggleAction, 60).GetComponent<Toggle>();
            this.chaosModeClassicToggle = MenuHandler.CreateToggle(MapEmbiggener.ClassicChaosConfig.Value, "Chaos Mode <i>Classic</i>", menu, chaosModeClassicToggleAction, 60).GetComponent<Toggle>();

        }
    }

}