using UnboundLib.GameModes;
using System.Collections;
using System.Linq;
using Photon.Pun;

namespace MapEmbiggener.Controllers.Default
{
    public class DefaultCameraController : CameraController
    {
        public const float SuddenDeathZoomSpeed = 1f;
        public const float ChaosModeZoomSpeed = 0.25f;
        private int PlayersAlive => PlayerManager.instance.players.Where(p => !p.data.dead).Select(p => p.playerID).Distinct().Count();
        private bool battleOnGoing = false;
        private bool suddenDeathMode = false;
        private bool chaosMode = false;
        public override IEnumerator OnGameStart(IGameModeHandler gm)
        {
            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            {
                this.suddenDeathMode = MapEmbiggener.suddenDeathMode;
                this.chaosMode = MapEmbiggener.chaosMode;
            }
            return base.OnGameStart(gm);
        }
        public override IEnumerator OnBattleStart(IGameModeHandler gm)
        {
            this.battleOnGoing = true;
            return base.OnBattleStart(gm);
        }
        public override IEnumerator OnPointEnd(IGameModeHandler gm)
        {
            this.battleOnGoing = false;
            return base.OnPointEnd(gm);
        }
        public override void OnUpdate()
        {
            if (this.suddenDeathMode && this.battleOnGoing && this.PlayersAlive == 2)
            {
                this.ZoomTarget = 0f;
                this.ZoomSpeed = SuddenDeathZoomSpeed;
            }
            else if (this.chaosMode && this.battleOnGoing)
            {
                this.ZoomTarget = 0f;
                this.ZoomSpeed = ChaosModeZoomSpeed;
            }
            else
            {
                this.ZoomTarget = null;
                this.ZoomSpeed = null;
            }
        }

        public override void SetDataToSync()
        {
            this.SyncedIntData["SD"] = this.suddenDeathMode ? 1 : 0;
            this.SyncedIntData["CM"] = this.chaosMode ? 1 : 0;
        }

        public override void ReadSyncedData()
        {
            this.suddenDeathMode = this.SyncedIntData["SD"] == 1;
            this.chaosMode = this.SyncedIntData["CM"] == 1;
        }

        public override bool SyncDataNow()
        {
            return true;
        }
    }
}
