using System;
using UnityEngine;
using UnityEngine.UI.ProceduralImage;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections;
using MapEmbiggener.Controllers.Default;
using MapEmbiggener.UI;
using UnboundLib.GameModes;
namespace MapEmbiggener.Controllers
{
    class ControllerManager : MonoBehaviour
    {

        public const float DefaultZoom = 20f;
        public static readonly Vector3 DefaultCameraPosition = new Vector3(0f, 0f, -100f);
        public static readonly Vector3 DefaultCameraRotation = new Vector3(0f, 0f, 0f);

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
        public static float Zoom { get; private set; } = DefaultZoom;
        public static Vector3 CameraPosition { get; private set; } = DefaultCameraPosition;
        public static Vector3 CameraRotation { get; private set; } = DefaultCameraRotation;
        public static float MapSize { get; private set; } = 1f;
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
            if (!GameManager.instance.isPlaying) { return; }

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
                    // direction to zoom. 0 if we're already there, +1 if we're below, -1 if above
                    int sgn = (float)zoomTarget == Zoom ? 0 : (float)zoomTarget >= Zoom ? +1 : -1;
                    if (sgn != 0)
                    {
                        Zoom += TimeHandler.deltaTime * (float)CurrentCameraController.ZoomSpeed * sgn;
                    }
                }

                Vector3 posTarget = CurrentCameraController.PositionTarget ?? DefaultCameraPosition;
                if (CurrentCameraController.MovementSpeed == null)
                {
                    CameraPosition = posTarget;
                }
                else
                {
                    CameraPosition += TimeHandler.deltaTime * (posTarget - CameraPosition).normalized;
                }
                Vector3 rotTarget = CurrentCameraController.RotationTarget ?? DefaultCameraRotation;
                if (CurrentCameraController.MovementSpeed == null)
                {
                    CameraRotation = rotTarget;
                }
                else
                {
                    CameraRotation += TimeHandler.deltaTime * (rotTarget - CameraRotation).normalized;
                }
            }
            
            // update the map
            if (CurrentMapController != null)
            {
                if (CurrentMapController.CallUpdate) { CurrentMapController.OnUpdate(); }
                // if the set size is null, use mapembiggener's setting
                MapSize = CurrentMapController.MapSize ?? MapEmbiggener.setSize;

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

                // for each of the parameters, find the speed and direction, then update if required
                int sgn;

                // X bounds

                // null speed means instant
                if (CurrentBoundsController.XSpeed == null)
                {
                    MaxX = (float)maxXTarget;
                    MinX = (float)minXTarget;
                }
                else
                {
                    // 0 if we're already there, +1 if we're below, -1 if above
                    sgn = (float)maxXTarget == MaxX ? 0 : (float)maxXTarget >= MaxX ? +1 : -1;
                    if (sgn != 0)
                    {
                        MaxX += TimeHandler.deltaTime * (float)CurrentBoundsController.XSpeed * sgn;
                    }
                    sgn = (float)minXTarget == MinX ? 0 : (float)minXTarget >= MinX ? +1 : -1;
                    if (sgn != 0)
                    {
                        MinX += TimeHandler.deltaTime * (float)CurrentBoundsController.XSpeed * sgn;
                    }
                }

                // Y bounds

                if (CurrentBoundsController.YSpeed == null)
                {
                    MaxY = (float)maxYTarget;
                    MinY = (float)minYTarget;
                }
                else
                {
                    sgn = (float)maxYTarget == MaxY ? 0 : (float)maxYTarget >= MaxY ? +1 : -1;
                    if (sgn != 0)
                    {
                        MaxY += TimeHandler.deltaTime * (float)CurrentBoundsController.YSpeed * sgn;
                    }
                    sgn = (float)minYTarget == MinY ? 0 : (float)minYTarget >= MinY ? +1 : -1;
                    if (sgn != 0)
                    {
                        MinY += TimeHandler.deltaTime * (float)CurrentBoundsController.YSpeed * sgn;
                    }
                }

                // bounds angle

                if (CurrentBoundsController.AngularSpeed == null)
                {
                    Angle = (float)angleTarget;
                }
                else
                {
                    sgn = (float)angleTarget == Angle ? 0 : (float)angleTarget >= Angle ? +1 : -1;
                    if (sgn != 0)
                    {
                        Angle += TimeHandler.deltaTime * (float)CurrentBoundsController.AngularSpeed * sgn;
                    }
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
                    ParticleColorMax = Color.Lerp(ParticleColorMax, particleColorMaxTarget, TimeHandler.deltaTime);
                    ParticleColorMin = Color.Lerp(ParticleColorMin, particleColorMinTarget, TimeHandler.deltaTime);
                    BorderColor = Color.Lerp(BorderColor, borderColorTarget, TimeHandler.deltaTime);
                }

                // particle gravity

                if (CurrentBoundsController.ParticleGravitySpeed == null)
                {
                    ParticleGravity = (float)gravityTarget;
                }
                else
                {
                    sgn = (float)gravityTarget == ParticleGravity ? 0 : (float)gravityTarget >= ParticleGravity ? +1 : -1;
                    if (sgn != 0)
                    {
                        ParticleGravity += TimeHandler.deltaTime * (float)CurrentBoundsController.ParticleGravitySpeed * sgn;
                    }
                }

                // update all the bounds properties
                OutOfBoundsUtils.SetOOB(MinX, MaxX, MinY, MaxY, Angle);
                OutOfBoundsParticles.instance?.SetColor(ParticleColorMax, ParticleColorMin);
                OutOfBoundsParticles.instance?.SetGravity(ParticleGravity);
                OutOfBoundsUtils.border.GetComponentInChildren<ProceduralImage>().color = BorderColor;

            }

        }
        #region gamemodehooks     
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
