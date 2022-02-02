using System.Collections;
using UnboundLib.GameModes;
namespace MapEmbiggener.Controllers
{
    public abstract class MapController : IMapController
    {
        public bool CallUpdate { get; protected set; } = true;
        public float? MapSize { get; protected set; } = null;

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
        public virtual IEnumerator OnPickEnd(IGameModeHandler gm)
        {
            yield break;
        }
        public virtual IEnumerator OnPickStart(IGameModeHandler gm)
        {
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
