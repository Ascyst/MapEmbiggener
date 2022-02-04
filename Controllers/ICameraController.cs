using UnityEngine;
using UnboundLib.GameModes;
using System.Collections;
namespace MapEmbiggener.Controllers
{
    /// <summary>
    /// Interface for camera controllers
    /// 
    /// PositionTarget (Vector3?) - the position the camera is moving to (null for default position)
    /// MovementSpeed (float?) - the speed (units/sec) that the camera moves at (null for instant)
    /// RotationTarget (Vector3?) - the target Euler angles for the camera (null for default)
    /// RotationSpeed (float?) - the rotation speed of the camera (null for instant)
    /// ZoomTarget (float?) - the target zoom of the camera (null for default)
    /// ZoomSpeed (float?) - the speed (units/sec) that the camera zooms at (null for instant)
    /// 
    /// </summary>
    public interface ICameraController : ISyncedController
    {
        bool CallUpdate { get; }
        Vector3? PositionTarget { get; }
        float? MovementSpeed { get; }
        Quaternion? RotationTarget { get; }
        float? RotationSpeed { get; }
        float? ZoomTarget { get; }
        float? ZoomSpeed { get; }
        void ReceiveSyncedCameraData(bool callUpdate, Vector3? positionTarget, float? movementSpeed, Quaternion? rotationTarget, float? rotationSpeed, float? zoomTarget, float? zoomSpeed);
        IEnumerator OnInitStart(IGameModeHandler gm);
        IEnumerator OnInitEnd(IGameModeHandler gm);
        IEnumerator OnGameStart(IGameModeHandler gm);
        IEnumerator OnGameEnd(IGameModeHandler gm);
        IEnumerator OnRoundStart(IGameModeHandler gm);
        IEnumerator OnRoundEnd(IGameModeHandler gm);
        IEnumerator OnPointStart(IGameModeHandler gm);
        IEnumerator OnPointEnd(IGameModeHandler gm);
        IEnumerator OnBattleStart(IGameModeHandler gm);
        IEnumerator OnPickStart(IGameModeHandler gm);
        IEnumerator OnPickEnd(IGameModeHandler gm);
        IEnumerator OnPlayerPickStart(IGameModeHandler gm);
        IEnumerator OnPlayerPickEnd(IGameModeHandler gm);
        void OnUpdate();
    }
}
