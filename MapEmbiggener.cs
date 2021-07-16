using System;
using BepInEx;
using HarmonyLib;
using Photon.Pun;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;
using System.Collections;
using UnboundLib.Networking;

namespace MapEmbiggener
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(ModId, ModName, "1.0.0")]
    [BepInProcess("Rounds.exe")]
    public class Mod : BaseUnityPlugin
    {
        private struct NetworkEventType
        {
            public const string SyncSize = ModId + "_Sync";
        }
        private const string ModId = "com.ascyst.rounds.mapembiggener";

        private const string ModName = "Map Embiggener";

        public static float setSize = 1.0f;

        private void Awake()
        {
            new Harmony(ModId).PatchAll();
            NetworkingManager.RegisterEvent(NetworkEventType.SyncSize, sync => setSize = (float)sync[0]);
        }

        private void Start()
        {
            Unbound.RegisterGUI(ModName, DrawGUI);
            Unbound.RegisterHandshake(ModId, OnHandShakeCompleted);
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
                Unbound.BuildInfoPopup("Button has been clicked");
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

        private void OnHandShakeCompleted()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                    NetworkingManager.RaiseEventOthers(NetworkEventType.SyncSize, setSize);
            }
        }

    }
}