using UnboundLib.GameModes;
using System.Collections;
namespace MapEmbiggener.Controllers
{
    /// <summary>
    /// Interface for Map controllers
    /// 
    /// MapSize (float?) - the size multiplier to apply to the NEXT map (null for default)
    /// </summary>
    public interface IMapController : ISyncedController
    {
        bool CallUpdate { get; }
        float? MapSize { get; }
        float? MapAngleTarget { get; }
        float? MapAngularSpeed { get; }
        void ReceiveSyncedMapData(bool callUpdate, float? mapSize, float? mapAngleTarget, float? mapAngularSpeed);
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
