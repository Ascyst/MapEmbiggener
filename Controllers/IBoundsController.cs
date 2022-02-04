using UnboundLib.GameModes;
using UnityEngine;
using System.Collections;
namespace MapEmbiggener.Controllers
{
    /// <summary>
    /// Interface for bounds controllers
    /// 
    /// Damage (OutOfBoundsDamage?) the type of damage that the out of bounds should inflict. options are: Normal, OverTime, Instakill, None, [and null for default]
    /// <Max/Min><X/Y>Target (float?) the target bound on the respective axis (null for default)
    /// AngleTarget (float?) the target angle for the bounds (null for default)
    /// XSpeed / YSpeed (float?) the movement speed for the bounds on the respective axis (null for instant)
    /// ParticleColor<Min/Max>Target (Color?) the target colors for the OOB particles (null for default)
    /// BorderColorTarget (Color?) the target color for the border (null for default)
    /// ColorSpeed (float?) the speed that the colors change to the target (null for instant)
    /// ParticleGravityTarget (float?) the target gravity multiplier for the particles (null for default)
    /// ParticleGravitySpeed (float?) the speed at which the gravity multiplier changes (null for instant)
    /// 
    /// </summary>
    public interface IBoundsController : ISyncedController
    {
        bool CallUpdate { get; }
        OutOfBoundsDamage? Damage { get; }
        float? MaxXTarget { get; }
        float? MaxYTarget { get; }
        float? MinXTarget { get; }
        float? MinYTarget { get; }
        float? AngleTarget { get; }
        float? XSpeed { get; }
        float? YSpeed { get; }
        float? AngularSpeed { get; }
        Color? ParticleColorMinTarget { get; }
        Color? ParticleColorMaxTarget { get; }
        Color? BorderColorTarget { get; }
        float? ColorSpeed { get; }
        float? ParticleGravityTarget { get; }
        float? ParticleGravitySpeed { get; }
        void ReceiveSyncedBoundsData(bool callUpdate, OutOfBoundsDamage? damage, float? maxXTarget, float? maxYTarget, float? minXTarget, float? minYTarget, float? angleTarget, float? xSpeed, float? ySpeed, float? angularSpeed);
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
