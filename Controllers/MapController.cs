using System.Collections;
using System.Collections.Generic;
using UnboundLib.GameModes;
namespace MapEmbiggener.Controllers
{
    public abstract class MapController : IMapController
    {
        public bool CallUpdate { get; protected set; } = true;
        public float? MapSize { get; protected set; } = null;
        public Dictionary<string, int> SyncedIntData { get; set; } = new Dictionary<string, int>() { };
        public Dictionary<string, float> SyncedFloatData { get; set; } = new Dictionary<string, float>() { };
        public Dictionary<string, string> SyncedStringData { get; set; } = new Dictionary<string, string>() { };
        public abstract void ReadSyncedData();
        public abstract void SetDataToSync();
        public abstract bool SyncDataNow();
        void IMapController.ReceiveSyncedMapData(bool callUpdate, float? mapSize)
        {
            this.CallUpdate = callUpdate;
            this.MapSize = mapSize;
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
            //MapManager.instance?.currentMap?.Map?.gameObject?.SetActive(true);
            yield break;
        }
        public virtual IEnumerator OnPickStart(IGameModeHandler gm)
        {
            //MapManager.instance?.currentMap?.Map?.gameObject?.SetActive(false);
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
