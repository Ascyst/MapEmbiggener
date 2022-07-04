using System;
using UnityEngine;
using UnityEngine.UI.ProceduralImage;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections;
using MapEmbiggener.Controllers.Default;
using MapEmbiggener.UI;
using UnboundLib.GameModes;
using UnboundLib.Networking;
using UnboundLib;
using Photon.Pun;
using MapEmbiggener.Networking;
namespace MapEmbiggener.Controllers
{
    public class ControllerManager : MonoBehaviour
    {
        private const int SyncPeriod = 10; // how often to sync, in frames
        private int currentFrame = 0;

        public const float DefaultZoom = 20f;
        public static readonly Vector3 DefaultCameraPosition = new Vector3(0f, 0f, -100f);
        public static readonly Quaternion DefaultCameraRotation = Quaternion.Euler(Vector3.zero);

        public const string DefaultCameraControllerID = "DefaultCamera";
        public const string DefaultMapControllerID = "DefaultMap";
        public const string DefaultBoundsControllerID = "DefaultBounds";

        public static ControllerManager instance = null;

        private static Dictionary<string, ICameraController> _CameraControllers = new Dictionary<string, ICameraController>() { };
        private static Dictionary<string, IMapController> _MapControllers = new Dictionary<string, IMapController>() { };
        private static Dictionary<string, IBoundsController> _BoundsControllers = new Dictionary<string, IBoundsController>() { };

        public static ReadOnlyDictionary<string, ICameraController> CameraControllers => new ReadOnlyDictionary<string, ICameraController>(_CameraControllers);
        public static ReadOnlyDictionary<string, IMapController> MapControllers => new ReadOnlyDictionary<string, IMapController>(_MapControllers);
        public static ReadOnlyDictionary<string, IBoundsController> BoundsControllers => new ReadOnlyDictionary<string, IBoundsController>(_BoundsControllers);

        public static string CurrentCameraControllerID { get; private set; }
        public static string CurrentMapControllerID { get; private set; }
        public static string CurrentBoundsControllerID { get; private set; }

        public static ICameraController CurrentCameraController
        {
            get
            {
                return CurrentCameraControllerID == null ? null : _CameraControllers[CurrentCameraControllerID];
            }
        }
        public static IMapController CurrentMapController
        {
            get
            {
                return CurrentMapControllerID == null ? null : _MapControllers[CurrentMapControllerID];
            }
        }
        public static IBoundsController CurrentBoundsController
        {
            get
            {
                return CurrentBoundsControllerID == null ? null : _BoundsControllers[CurrentBoundsControllerID];
            }
        }
        public static void AddCameraController(string ID, ICameraController controller)
        {
            _CameraControllers.Add(ID, controller);
        }
        public static void AddMapController(string ID, IMapController controller)
        {
            _MapControllers.Add(ID, controller);
        }
        public static void AddBoundsController(string ID, IBoundsController controller)
        {
            _BoundsControllers.Add(ID, controller);
        }
        public static void RemoveCameraController(string ID)
        {
            if (_CameraControllers.ContainsKey(ID)) { _CameraControllers.Remove(ID); }
        }
        public static void RemoveMapController(string ID)
        {
            if (_MapControllers.ContainsKey(ID)) { _MapControllers.Remove(ID); }
        }
        public static void RemoveBoundsController(string ID)
        {
            if (_BoundsControllers.ContainsKey(ID)) { _BoundsControllers.Remove(ID); }
        }
        public static void SetCameraController(string ID)
        {
            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            {
                NetworkingManager.RPC(typeof(ControllerManager), nameof(RPCA_SetCameraController), ID);
            }
        }
        [UnboundRPC]
        private static void RPCA_SetCameraController(string ID)
        {
            if (ID != null && !_CameraControllers.ContainsKey(ID))
            {
                throw new ArgumentException($"No such Camera Controller: {ID}");
            }
            if (CurrentCameraControllerID == ID)
            {
                return;
            }
            CurrentCameraControllerID = ID ?? DefaultCameraControllerID;
        }
        public static void SetMapController(string ID)
        {
            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            {
                NetworkingManager.RPC(typeof(ControllerManager), nameof(RPCA_SetMapController), ID);
            }
        }
        [UnboundRPC]
        private static void RPCA_SetMapController(string ID)
        {
            if (ID != null && !_MapControllers.ContainsKey(ID))
            {
                throw new ArgumentException($"No such Map Controller: {ID}");
            }
            if (CurrentMapControllerID == ID)
            {
                return;
            }
            CurrentMapControllerID = ID ?? DefaultMapControllerID;
        }
        public static void SetBoundsController(string ID)
        {
            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            {
                NetworkingManager.RPC(typeof(ControllerManager), nameof(RPCA_SetBoundsController), ID);
            }
        }
        [UnboundRPC]
        private static void RPCA_SetBoundsController(string ID)
        {
            if (ID != null && !_BoundsControllers.ContainsKey(ID))
            {
                throw new ArgumentException($"No such Bounds Controller: {ID}");
            }
            if (CurrentBoundsControllerID == ID)
            {
                return;
            }
            CurrentBoundsControllerID = ID ?? DefaultBoundsControllerID;
        }
        [UnboundRPC]
        public static void RPC_RequestSync(int requestingPlayer)
        {
            NetworkingManager.RPC(typeof(ControllerManager), nameof(ControllerManager.RPC_SyncResponse), requestingPlayer, PhotonNetwork.LocalPlayer.ActorNumber);
        }

        [UnboundRPC]
        public static void RPC_SyncResponse(int requestingPlayer, int readyPlayer)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == requestingPlayer)
            {
                ControllerManager.instance.RemovePendingRequest(readyPlayer, nameof(ControllerManager.RPC_RequestSync));
            }
        }

        protected virtual IEnumerator WaitForSyncUp()
        {
            if (PhotonNetwork.OfflineMode)
            {
                yield break;
            }
            yield return this.SyncMethod(nameof(ControllerManager.RPC_RequestSync), null, PhotonNetwork.LocalPlayer.ActorNumber);
        }
        #region SyncCameraControllerProperties
        private static void SyncCameraControllerProperties()
        {
            if (PhotonNetwork.OfflineMode) { return; }

            if (PhotonNetwork.IsMasterClient)
            {
                CurrentCameraController?.SetDataToSync();
                NetworkingManager.RPC_Others(typeof(ControllerManager), nameof(RPCO_SyncCameraControllerProperties), CurrentCameraController?.CallUpdate, CurrentCameraController?.PositionTarget, CurrentCameraController?.MovementSpeed, CurrentCameraController?.RotationTarget, CurrentCameraController?.RotationSpeed, CurrentCameraController?.ZoomTarget, CurrentCameraController?.ZoomSpeed, CurrentCameraController?.SyncedIntData, CurrentCameraController?.SyncedFloatData, CurrentCameraController?.SyncedStringData);
            }
        }
        [UnboundRPC]
        private static void RPCO_SyncCameraControllerProperties(bool? callUpdate, Vector3? posTarget, float? movSpeed, Quaternion? rotTarget, float? rotSpeed, float? zoomTarget, float? zoomSpeed, Dictionary<string, int> intsToSync, Dictionary<string, float> floatsToSync, Dictionary<string, string> stringsToSync)
        {
            if (callUpdate != null)
            {
                CurrentCameraController?.ReceiveSyncedCameraData((bool)callUpdate, posTarget, movSpeed, rotTarget, rotSpeed, zoomTarget, zoomSpeed);
            }
            if (intsToSync != null) { CurrentCameraController.SyncedIntData = intsToSync; }
            if (floatsToSync != null) { CurrentCameraController.SyncedFloatData = floatsToSync; }
            if (stringsToSync != null) { CurrentCameraController.SyncedStringData = stringsToSync; }

            CurrentCameraController?.ReadSyncedData();
        }
        #endregion
        #region SyncMapControllerProperties
        private static void SyncMapControllerProperties()
        {
            if (PhotonNetwork.OfflineMode) { return; }

            if (PhotonNetwork.IsMasterClient)
            {
                CurrentMapController?.SetDataToSync();
                NetworkingManager.RPC_Others(typeof(ControllerManager), nameof(RPCO_SyncMapControllerProperties), CurrentMapController?.CallUpdate, CurrentMapController?.MapSize, CurrentMapController?.MapAngleTarget, CurrentMapController?.MapAngularSpeed, CurrentMapController?.SyncedIntData, CurrentMapController?.SyncedFloatData, CurrentMapController?.SyncedStringData);
            }
        }
        [UnboundRPC]
        private static void RPCO_SyncMapControllerProperties(bool? callUpdate, float? mapSize, float? mapAngleTarget, float? mapAngularSpeed, Dictionary<string, int> intsToSync, Dictionary<string, float> floatsToSync, Dictionary<string, string> stringsToSync)
        {
            if (callUpdate != null)
            {
                CurrentMapController?.ReceiveSyncedMapData((bool)callUpdate, mapSize, mapAngleTarget, mapAngularSpeed);
            }
            if (intsToSync != null) { CurrentMapController.SyncedIntData = intsToSync; }
            if (floatsToSync != null) { CurrentMapController.SyncedFloatData = floatsToSync; }
            if (stringsToSync != null) { CurrentMapController.SyncedStringData = stringsToSync; }

            CurrentMapController?.ReadSyncedData();
        }
        #endregion
        #region SyncBoundsControllerProperties
        private static void SyncBoundsControllerProperties()
        {
            if (PhotonNetwork.OfflineMode) { return; }

            if (PhotonNetwork.IsMasterClient)
            {
                CurrentBoundsController?.SetDataToSync();
                NetworkingManager.RPC_Others(typeof(ControllerManager), nameof(RPCO_SyncBoundsControllerProperties), CurrentBoundsController?.CallUpdate, (byte?)CurrentBoundsController?.Damage, CurrentBoundsController?.MaxXTarget, CurrentBoundsController?.MaxYTarget, CurrentBoundsController?.MinXTarget, CurrentBoundsController?.MinYTarget, CurrentBoundsController?.AngleTarget, CurrentBoundsController?.XSpeed, CurrentBoundsController?.YSpeed, CurrentBoundsController?.AngularSpeed, CurrentBoundsController?.SyncedIntData, CurrentBoundsController?.SyncedFloatData, CurrentBoundsController?.SyncedStringData);
            }
        }
        [UnboundRPC]
        private static void RPCO_SyncBoundsControllerProperties(bool? callUpdate, byte? damage, float? xMaxTarget, float? yMaxTarget, float? xMinTarget, float? yMinTarget, float? angleTarget, float? xSpeed, float? ySpeed, float? angularSpeed, Dictionary<string, int> intsToSync, Dictionary<string, float> floatsToSync, Dictionary<string, string> stringsToSync)
        {
            if (callUpdate != null)
            {
                CurrentBoundsController?.ReceiveSyncedBoundsData((bool)callUpdate, (OutOfBoundsDamage?)damage, xMaxTarget, yMaxTarget, xMinTarget, yMinTarget, angleTarget, xSpeed, ySpeed, angularSpeed);
            }
            if (intsToSync != null) { CurrentBoundsController.SyncedIntData = intsToSync; }
            if (floatsToSync != null) { CurrentBoundsController.SyncedFloatData = floatsToSync; }
            if (stringsToSync != null) { CurrentBoundsController.SyncedStringData = stringsToSync; }

            CurrentBoundsController?.ReadSyncedData();
        }
        #endregion
        #region SyncControllerIDs 
        private static bool WaitingForControllersSync = true;
        private static IEnumerator SyncControllerIDs()
        {
            if (PhotonNetwork.OfflineMode) { yield break; }

            if (PhotonNetwork.IsMasterClient)
            {
                yield return ControllerManager.instance.SyncMethod(nameof(RPCA_SyncAllControllers), null, PhotonNetwork.LocalPlayer.ActorNumber, CurrentCameraControllerID, CurrentMapControllerID, CurrentBoundsControllerID);
            }

            yield return new WaitUntil(() => !WaitingForControllersSync);

            WaitingForControllersSync = true;
        }
        [UnboundRPC]
        private static void RPCA_SyncAllControllers(int authoritativePlayer, string cameraControllerID, string mapControllerID, string boundsControllerID)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber != authoritativePlayer)
            {
                ControllerManager.SetCameraController(cameraControllerID);
                ControllerManager.SetMapController(mapControllerID);
                ControllerManager.SetBoundsController(boundsControllerID);
            }

            WaitingForControllersSync = false;

            NetworkingManager.RPC(typeof(ControllerManager), nameof(SyncControllerResponse), authoritativePlayer, PhotonNetwork.LocalPlayer.ActorNumber);
        }
        [UnboundRPC]
        private static void SyncControllerResponse(int authoritativePlayer, int respondingPlayer)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == authoritativePlayer)
            {
                ControllerManager.instance.RemovePendingRequest(respondingPlayer, nameof(ControllerManager.RPCA_SyncAllControllers));
            }
        }
        #endregion
        public static float Zoom { get; private set; } = DefaultZoom;
        public static Vector3 CameraPosition { get; private set; } = DefaultCameraPosition;
        public static Quaternion CameraRotation { get; private set; } = DefaultCameraRotation;
        public static float MapSize { get; private set; } = 1f;
        public static float MapAngle { get; private set; } = 0f;
        public static OutOfBoundsDamage Damage { get; private set; } = OutOfBoundsDamage.Normal;
        public static float MaxX { get; private set; } = OutOfBoundsUtils.defaultX;
        public static float MaxY{ get; private set; } = OutOfBoundsUtils.defaultY;
        public static float MinX{ get; private set; } = -OutOfBoundsUtils.defaultX;
        public static float MinY{ get; private set; } = -OutOfBoundsUtils.defaultY;
        public static float Angle{ get; private set; } = OutOfBoundsUtils.defaultAngle;
        public static Color ParticleColorMin { get; private set; } = OutOfBoundsParticles.DefaultColorMin;
        public static Color ParticleColorMax { get; private set; } = OutOfBoundsParticles.DefaultColorMax;
        public static Color BorderColor { get; private set; } = Color.red;
        public static float ParticleGravity { get; private set; } = OutOfBoundsParticles.DefaultGravity;
        internal void Init()
        {
            ControllerManager.AddCameraController(DefaultCameraControllerID, new DefaultCameraController());
            ControllerManager.AddMapController(DefaultMapControllerID, new DefaultMapController());
            ControllerManager.AddBoundsController(DefaultBoundsControllerID, new DefaultBoundsController());

            ControllerManager.SetCameraController(DefaultCameraControllerID);
            ControllerManager.SetMapController(DefaultMapControllerID);
            ControllerManager.SetBoundsController(DefaultBoundsControllerID);
        }

        void Awake()
        {
            if (ControllerManager.instance != null)
            {
                Destroy(this);
            }
            else
            {
                ControllerManager.instance = this;
            }
        }
        void Update()
        {
            // special handling for map editor
            GameObject firstChild = MapManager.instance?.currentMap?.Map?.transform?.GetChild(0)?.gameObject;
            bool isMapEditor = firstChild?.name?.Equals("Content") ?? false;
            bool isMapEditorSimulating = isMapEditor && !(firstChild?.activeSelf ?? false);

            if (!GameManager.instance.isPlaying || isMapEditor)
            {
                // if no game in progress, hide the bounds and reset the camera
                MaxX = OutOfBoundsUtils.defaultX;
                MaxY = OutOfBoundsUtils.defaultY;
                MinX = -OutOfBoundsUtils.defaultX;
                MinY = -OutOfBoundsUtils.defaultY;
                Angle = OutOfBoundsUtils.defaultAngle;
                Damage = OutOfBoundsUtils.DefaultDamage;
                ParticleColorMax = OutOfBoundsParticles.DefaultColorMax;
                ParticleColorMin = OutOfBoundsParticles.DefaultColorMin;
                BorderColor = Color.red;
                ParticleGravity = OutOfBoundsParticles.DefaultGravity;

                if (isMapEditor && !isMapEditorSimulating)
                {
                    if (Input.GetKey(KeyCode.W))
                    {
                        CameraPosition += Vector3.up * Time.deltaTime * 20f;
                    }
                    if (Input.GetKey(KeyCode.S))
                    {
                        CameraPosition -= Vector3.up * Time.deltaTime * 20f;
                    }
                    if (Input.GetKey(KeyCode.A))
                    {
                        CameraPosition -= Vector3.right * Time.deltaTime * 20f;
                    }
                    if (Input.GetKey(KeyCode.D))
                    {
                        CameraPosition += Vector3.right * Time.deltaTime * 20f;
                    }
                    if (Input.mouseScrollDelta.y != 0f)
                    {
                        Zoom = UnityEngine.Mathf.Clamp(Zoom - Input.mouseScrollDelta.y * Time.deltaTime * 20f, 1f, float.MaxValue);
                    }
                }
                else
                {
                    Zoom = DefaultZoom;
                    CameraPosition = DefaultCameraPosition;
                }
                CameraRotation = DefaultCameraRotation;

                // update all the bounds properties
                OutOfBoundsUtils.SetOOB(MinX, MaxX, MinY, MaxY, Angle);
                OutOfBoundsParticles.instance?.SetColor(ParticleColorMax, ParticleColorMin);
                OutOfBoundsParticles.instance?.SetGravity(ParticleGravity);
                if (OutOfBoundsUtils.border == null) { return; }
                OutOfBoundsUtils.border.GetComponentInChildren<ProceduralImage>().color = BorderColor;

                // don't run any controllers
                return;

            }
            // update the camera
            if (CurrentCameraController != null)
            {
                if (CurrentCameraController.CallUpdate) { CurrentCameraController.OnUpdate(); }
                // figure out what to use for the zoom target
                float? zoomTarget = CurrentCameraController.ZoomTarget;
                if (zoomTarget == null)
                {
                    // try the current map size first, either through the map itself, or through the map controller
                    if (MapManager.instance.currentMap != null && MapManager.instance.currentMap.Map.size != 0f) { zoomTarget = MapManager.instance.currentMap.Map.size; }
                    else { zoomTarget = DefaultZoom; }
                }

                // null speed means instant
                if (CurrentCameraController.ZoomSpeed == null)
                {
                    Zoom = (float)zoomTarget;
                }
                else
                {
                    Zoom = UnityEngine.Mathf.MoveTowards(Zoom, (float)zoomTarget, TimeHandler.deltaTime * (float)CurrentCameraController.ZoomSpeed);
                }

                Vector3 posTarget = CurrentCameraController.PositionTarget ?? DefaultCameraPosition;
                if (CurrentCameraController.MovementSpeed == null)
                {
                    CameraPosition = posTarget;
                }
                else
                {
                    CameraPosition = Vector3.MoveTowards(CameraPosition, posTarget, TimeHandler.deltaTime * (float)CurrentCameraController.MovementSpeed);
                }
                Quaternion rotTarget = CurrentCameraController.RotationTarget ?? DefaultCameraRotation;
                if (CurrentCameraController.RotationSpeed == null)
                {
                    CameraRotation = rotTarget;
                }
                else
                {
                    CameraRotation = Quaternion.RotateTowards(CameraRotation, rotTarget, (float)CurrentCameraController.RotationSpeed * TimeHandler.deltaTime);
                }
            }
            
            // update the map
            if (CurrentMapController != null)
            {
                if (CurrentMapController.CallUpdate) { CurrentMapController.OnUpdate(); }
                // if the set size is null, use 1f
                MapSize = CurrentMapController.MapSize ?? 1f;

                float mapAngleTarget = CurrentMapController.MapAngleTarget ?? 0f;

                if (CurrentMapController.MapAngularSpeed == null)
                {
                    MapAngle = mapAngleTarget;
                }
                else
                {
                    MapAngle = Mathf.MoveTowards(MapAngle, mapAngleTarget, TimeHandler.deltaTime * (float)CurrentMapController.MapAngularSpeed);
                }

            }

            // update the bounds
            if (CurrentBoundsController != null)
            {
                if (CurrentBoundsController.CallUpdate) { CurrentBoundsController.OnUpdate(); }

                // update bounds damage type
                Damage = CurrentBoundsController.Damage ?? OutOfBoundsDamage.Normal;

                // figure out what to use for the targets
                float maxXTarget = CurrentBoundsController.MaxXTarget ?? OutOfBoundsUtils.defaultX;
                float maxYTarget = CurrentBoundsController.MaxYTarget ?? OutOfBoundsUtils.defaultY;
                float minXTarget = CurrentBoundsController.MinXTarget ?? -OutOfBoundsUtils.defaultX;
                float minYTarget = CurrentBoundsController.MinYTarget ?? -OutOfBoundsUtils.defaultY;
                float angleTarget = CurrentBoundsController.AngleTarget ?? OutOfBoundsUtils.defaultAngle;
                Color particleColorMaxTarget = CurrentBoundsController.ParticleColorMaxTarget ?? OutOfBoundsParticles.DefaultColorMax;
                Color particleColorMinTarget = CurrentBoundsController.ParticleColorMinTarget ?? OutOfBoundsParticles.DefaultColorMin;
                Color borderColorTarget = CurrentBoundsController.BorderColorTarget ?? Color.red;
                float gravityTarget = CurrentBoundsController.ParticleGravityTarget ?? OutOfBoundsParticles.DefaultGravity;

                // X bounds

                // null speed means instant
                if (CurrentBoundsController.XSpeed == null)
                {
                    MaxX = (float)maxXTarget;
                    MinX = (float)minXTarget;
                }
                else
                {
                    MaxX = Mathf.MoveTowards(MaxX, maxXTarget, TimeHandler.deltaTime * (float)CurrentBoundsController.XSpeed);
                    MinX = Mathf.MoveTowards(MinX, minXTarget, TimeHandler.deltaTime * (float)CurrentBoundsController.XSpeed);
                }

                // Y bounds

                if (CurrentBoundsController.YSpeed == null)
                {
                    MaxY = (float)maxYTarget;
                    MinY = (float)minYTarget;
                }
                else
                {
                    MaxY = Mathf.MoveTowards(MaxY, maxYTarget, TimeHandler.deltaTime * (float)CurrentBoundsController.YSpeed);
                    MinY = Mathf.MoveTowards(MinY, minYTarget, TimeHandler.deltaTime * (float)CurrentBoundsController.YSpeed);
                }

                // bounds angle

                if (CurrentBoundsController.AngularSpeed == null)
                {
                    Angle = (float)angleTarget;
                }
                else
                {
                    Angle = Mathf.MoveTowards(Angle, angleTarget, TimeHandler.deltaTime * (float)CurrentBoundsController.AngularSpeed);
                }

                // colors - making use of Zeno's paradox
                if (CurrentBoundsController.ColorSpeed == null)
                {
                    ParticleColorMax = particleColorMaxTarget;
                    ParticleColorMin = particleColorMinTarget;
                    BorderColor = borderColorTarget;
                }
                else
                {
                    ParticleColorMax = Color.Lerp(ParticleColorMax, particleColorMaxTarget, TimeHandler.deltaTime * (float)CurrentBoundsController.ColorSpeed);
                    ParticleColorMin = Color.Lerp(ParticleColorMin, particleColorMinTarget, TimeHandler.deltaTime * (float)CurrentBoundsController.ColorSpeed);
                    BorderColor = Color.Lerp(BorderColor, borderColorTarget, TimeHandler.deltaTime * (float)CurrentBoundsController.ColorSpeed);
                }

                // particle gravity

                if (CurrentBoundsController.ParticleGravitySpeed == null)
                {
                    ParticleGravity = (float)gravityTarget;
                }
                else
                {
                    ParticleGravity = Mathf.MoveTowards(ParticleGravity, gravityTarget, TimeHandler.deltaTime * (float)CurrentBoundsController.ParticleGravitySpeed);
                }
            }


            // syncing
            this.currentFrame++;
            if (this.currentFrame > SyncPeriod)
            {
                this.currentFrame = 0;
                if (!PhotonNetwork.OfflineMode && PhotonNetwork.CurrentRoom != null && PhotonNetwork.IsMasterClient) 
                {

                    bool? syncCameraController = CurrentCameraController?.SyncDataNow();
                    bool? syncMapController = CurrentMapController?.SyncDataNow();
                    bool? syncBoundsController = CurrentBoundsController?.SyncDataNow();

                    // sync core properties only from controllers that have requested syncing (force them if they return null)
                    float? zoomToSync = (syncCameraController ?? true) ? (float?)Zoom : null;
                    Vector3? posToSync = (syncCameraController ?? true) ? (Vector3?)CameraPosition : null;
                    Quaternion? rotToSync = (syncCameraController ?? true) ? (Quaternion?)CameraRotation : null;
                    float? mapSizeToSync = (syncMapController ?? true) ? (float?)MapSize : null;
                    byte? damageToSync = (syncBoundsController ?? true) ? (byte?)Damage : null;
                    float? maxXToSync = (syncBoundsController ?? true) ? (float?)MaxX : null;
                    float? maxYToSync = (syncBoundsController ?? true) ? (float?)MaxY : null;
                    float? minXToSync = (syncBoundsController ?? true) ? (float?)MinX : null;
                    float? minYToSync = (syncBoundsController ?? true) ? (float?)MinY : null;
                    float? angleToSync = (syncBoundsController ?? true) ? (float?)Angle : null;

                    NetworkingManager.RPC_Others(typeof(ControllerManager), nameof(RPCA_SyncCurrentProperties), zoomToSync, posToSync, rotToSync, mapSizeToSync, damageToSync, maxXToSync, maxYToSync, minXToSync, minYToSync, angleToSync);

                    if (syncCameraController ?? false) { ControllerManager.SyncCameraControllerProperties(); }
                    if (syncMapController ?? false) { ControllerManager.SyncMapControllerProperties(); }
                    if (syncBoundsController ?? false) { ControllerManager.SyncBoundsControllerProperties(); }
                }
            }

            // update all the bounds properties
            OutOfBoundsUtils.SetOOB(MinX, MaxX, MinY, MaxY, Angle);
            OutOfBoundsParticles.instance?.SetColor(ParticleColorMax, ParticleColorMin);
            OutOfBoundsParticles.instance?.SetGravity(ParticleGravity);
            OutOfBoundsUtils.border.GetComponentInChildren<ProceduralImage>().color = BorderColor;
        }

        [UnboundRPC]
        private static void RPCA_SyncCurrentProperties(float? zoom, Vector3? cameraPos, Quaternion? cameraRot, float? mapSize, byte? damage, float? maxX, float? maxY, float? minX, float? minY, float? angle)
        {
            if (zoom.HasValue)
            {
                Zoom = zoom.Value;
            }
            if (cameraPos.HasValue)
            {
                CameraPosition = cameraPos.Value;
            }
            if (cameraRot.HasValue)
            {
                CameraRotation = cameraRot.Value;
            }
            if (mapSize.HasValue)
            {
                MapSize = mapSize.Value;
            }
            if (damage.HasValue)
            {
                Damage = (OutOfBoundsDamage)damage.Value;
            }
            if (maxX.HasValue)
            {
                MaxX = maxX.Value;
            }
            if (maxY.HasValue)
            {
                MaxY = maxY.Value;
            }
            if (minX.HasValue)
            {
                MinX = minX.Value;
            }
            if (minY.HasValue)
            {
                MinY = minY.Value;
            }
            if (angle.HasValue)
            {
                Angle = angle.Value;
            }
        }


        #region Game Mode Hooks
        public static IEnumerator OnInitStart(IGameModeHandler gm)
        {
            yield return CurrentCameraController?.OnInitStart(gm);
            yield return CurrentMapController?.OnInitStart(gm);
            yield return CurrentBoundsController?.OnInitStart(gm);
        }
        public static IEnumerator OnInitEnd(IGameModeHandler gm)
        {
            yield return CurrentCameraController?.OnInitEnd(gm);
            yield return CurrentMapController?.OnInitEnd(gm);
            yield return CurrentBoundsController?.OnInitEnd(gm);
        }
        public static IEnumerator OnGameStart(IGameModeHandler gm)
        {
            yield return ControllerManager.SyncControllerIDs();
            yield return CurrentCameraController?.OnGameStart(gm);
            yield return CurrentMapController?.OnGameStart(gm);
            yield return CurrentBoundsController?.OnGameStart(gm);
        }
        public static IEnumerator OnGameEnd(IGameModeHandler gm)
        {
            yield return CurrentCameraController?.OnGameEnd(gm);
            yield return CurrentMapController?.OnGameEnd(gm);
            yield return CurrentBoundsController?.OnGameEnd(gm);
        }
        public static IEnumerator OnRoundStart(IGameModeHandler gm)
        {
            yield return CurrentCameraController?.OnRoundStart(gm);
            yield return CurrentMapController?.OnRoundStart(gm);
            yield return CurrentBoundsController?.OnRoundStart(gm);
        }
        public static IEnumerator OnRoundEnd(IGameModeHandler gm)
		{
            yield return CurrentCameraController?.OnRoundEnd(gm);
            yield return CurrentMapController?.OnRoundEnd(gm);
            yield return CurrentBoundsController?.OnRoundEnd(gm);
		}
        public static IEnumerator OnPointStart(IGameModeHandler gm)
		{
            yield return CurrentCameraController?.OnPointStart(gm);
            yield return CurrentMapController?.OnPointStart(gm);
            yield return CurrentBoundsController?.OnPointStart(gm);
		}
        public static IEnumerator OnPointEnd(IGameModeHandler gm)
		{
            yield return CurrentCameraController?.OnPointEnd(gm);
            yield return CurrentMapController?.OnPointEnd(gm);
            yield return CurrentBoundsController?.OnPointEnd(gm);
		}
        public static IEnumerator OnBattleStart(IGameModeHandler gm)
		{
            yield return CurrentCameraController?.OnBattleStart(gm);
            yield return CurrentMapController?.OnBattleStart(gm);
            yield return CurrentBoundsController?.OnBattleStart(gm);
		}
        public static IEnumerator OnPickStart(IGameModeHandler gm)
		{
            yield return CurrentCameraController?.OnPickStart(gm);
            yield return CurrentMapController?.OnPickStart(gm);
            yield return CurrentBoundsController?.OnPickStart(gm);
		}
        public static IEnumerator OnPickEnd(IGameModeHandler gm)
		{
            yield return CurrentCameraController?.OnPickEnd(gm);
            yield return CurrentMapController?.OnPickEnd(gm);
            yield return CurrentBoundsController?.OnPickEnd(gm);
		}
        public static IEnumerator OnPlayerPickStart(IGameModeHandler gm)
		{
            yield return CurrentCameraController?.OnPlayerPickStart(gm);
            yield return CurrentMapController?.OnPlayerPickStart(gm);
            yield return CurrentBoundsController?.OnPlayerPickStart(gm);
		}
        public static IEnumerator OnPlayerPickEnd(IGameModeHandler gm)
		{
            yield return CurrentCameraController?.OnPlayerPickEnd(gm);
            yield return CurrentMapController?.OnPlayerPickEnd(gm);
            yield return CurrentBoundsController?.OnPlayerPickEnd(gm);
		}
        #endregion
    }
}
