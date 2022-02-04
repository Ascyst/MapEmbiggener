using UnboundLib.GameModes;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace MapEmbiggener.Controllers
{
    public abstract class CameraController : ICameraController
    {
        public bool CallUpdate { get; protected set; } = true;
        public Vector3? PositionTarget { get; protected set; } = null;
        public float? MovementSpeed { get; protected set; } = null;
        public Quaternion? RotationTarget { get; protected set; } = null;
        public float? RotationSpeed { get; protected set; } = null;
        public float? ZoomTarget { get; protected set; } = null;
        public float? ZoomSpeed { get; protected set; } = null;

        public Dictionary<string, int> SyncedIntData { get; set; } = new Dictionary<string, int>() { };
        public Dictionary<string, float> SyncedFloatData { get; set; } = new Dictionary<string, float>() { };
        public Dictionary<string, string> SyncedStringData { get; set; } = new Dictionary<string, string>() { };

        private Vector3? savedPositionTarget;
        private float? savedMovementSpeed;
        private Quaternion? savedRotationTarget;
        private float? savedRotationSpeed;
        private float? savedZoomTarget;
        private float? savedZoomSpeed;
        private bool savedCallUpdate;

        public abstract void SetDataToSync();
        public abstract void ReadSyncedData();
        public abstract bool SyncDataNow();

        void ICameraController.ReceiveSyncedCameraData(bool callUpdate, Vector3? positionTarget, float? movementSpeed, Quaternion? rotationTarget, float? rotationSpeed, float? zoomTarget, float? zoomSpeed)
        {
            this.CallUpdate = callUpdate;
            this.PositionTarget = positionTarget;
            this.MovementSpeed = movementSpeed;
            this.RotationTarget = rotationTarget;
            this.RotationSpeed = rotationSpeed;
            this.ZoomTarget = zoomTarget;
            this.ZoomSpeed = zoomSpeed;
        }

        public virtual IEnumerator OnBattleStart(IGameModeHandler gm)
        {
            yield break;
        }
        public virtual IEnumerator OnGameEnd(IGameModeHandler gm)
        {
            yield break;
        }
        public virtual IEnumerator OnGameStart(IGameModeHandler gm)
        {
            this.PositionTarget = null;
            this.MovementSpeed = null;
            this.RotationTarget = null;
            this.RotationSpeed = null;
            this.ZoomTarget = ControllerManager.DefaultZoom;
            this.ZoomSpeed = null;
            yield break;
        }
        public virtual IEnumerator OnInitEnd(IGameModeHandler gm)
        {
            yield break;
        }
        public virtual IEnumerator OnInitStart(IGameModeHandler gm)
        {
            yield break;
        }
        public virtual IEnumerator OnPickStart(IGameModeHandler gm)
        {
            // set pick phase camera
            this.savedCallUpdate = this.CallUpdate;
            this.CallUpdate = false;
            this.savedPositionTarget = this.PositionTarget;
            this.savedMovementSpeed = this.MovementSpeed;
            this.savedRotationTarget = this.RotationTarget;
            this.savedRotationSpeed = this.RotationSpeed;
            this.savedZoomTarget = this.ZoomTarget;
            this.savedZoomSpeed = this.ZoomSpeed;
            this.PositionTarget = null;
            this.MovementSpeed = null;
            this.RotationTarget = null;
            this.RotationSpeed = null;
            this.ZoomTarget = ControllerManager.DefaultZoom;
            this.ZoomSpeed = null;
            yield break;
        }
        public virtual IEnumerator OnPickEnd(IGameModeHandler gm)
        {
            // restore previous values
            this.CallUpdate = this.savedCallUpdate;
            this.PositionTarget = this.savedPositionTarget;
            this.MovementSpeed = this.savedMovementSpeed;
            this.RotationTarget = this.savedRotationTarget;
            this.RotationSpeed = this.savedRotationSpeed;
            this.ZoomTarget = this.savedZoomTarget;
            this.ZoomSpeed = this.savedZoomSpeed;
            yield break;
        }
        public virtual IEnumerator OnPlayerPickEnd(IGameModeHandler gm)
        {
            yield break;
        }
        public virtual IEnumerator OnPlayerPickStart(IGameModeHandler gm)
        {
            yield break;
        }
        public virtual IEnumerator OnPointEnd(IGameModeHandler gm)
        {
            yield break;
        }
        public virtual IEnumerator OnPointStart(IGameModeHandler gm)
        {
            yield break;
        }
        public virtual IEnumerator OnRoundEnd(IGameModeHandler gm)
        {
            yield break;
        }
        public virtual IEnumerator OnRoundStart(IGameModeHandler gm)
        {
            yield break;
        }
        public virtual void OnUpdate()
        {

        }
    }
}
