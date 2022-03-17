using System.Collections;
using UnboundLib.GameModes;
using UnboundLib;
using UnboundLib.Networking;
using Photon.Pun;
namespace MapEmbiggener.Controllers.Default
{
    public class DefaultMapController : MapController
    {
        public override void ReadSyncedData()
        {
        }
        public override void SetDataToSync()
        {
        }
        public override bool SyncDataNow()
        {
            return true;
        }
        public override IEnumerator OnInitEnd(IGameModeHandler gm)
        {
            this.CallUpdate = true;
            return base.OnInitEnd(gm);
        }
        public override IEnumerator OnGameStart(IGameModeHandler gm)
        {
            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            {
                this.MapSize = MapEmbiggener.setSize;
            }

            return base.OnGameStart(gm);
        }
    }
}
