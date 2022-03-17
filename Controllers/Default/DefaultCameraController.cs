using UnboundLib.GameModes;
using System.Collections;
using System.Linq;
using Photon.Pun;
using UnityEngine;

namespace MapEmbiggener.Controllers.Default
{
    public class DefaultCameraController : CameraController
    {
        public const float SuddenDeathZoomSpeed = 1f;
        //public const float ChaosModeZoomSpeed = 0.25f;
        public const float ChaosModeClassicZoomSpeed = 0.5f;
        public const float ChaosModeClassicRotationSpeed = 3f;
        private int PlayersAlive => PlayerManager.instance.players.Where(p => !p.data.dead).Select(p => p.playerID).Distinct().Count();
        private bool battleOnGoing = false;
        private bool suddenDeathMode = false;
        private bool chaosMode = false;
        private bool chaosModeClassic = false;
        private int chaosModeClassicSign = -1;
        public override IEnumerator OnInitEnd(IGameModeHandler gm)
        {
            this.CallUpdate = true;
            return base.OnInitEnd(gm);
        }
        public override IEnumerator OnGameStart(IGameModeHandler gm)
        {
            this.chaosModeClassicSign = -1;
            this.RotationTarget = null;
            this.RotationSpeed = null;
            this.ZoomTarget = null;
            this.ZoomSpeed = null;
            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            {
                this.suddenDeathMode = MapEmbiggener.suddenDeathMode;
                this.chaosMode = MapEmbiggener.chaosMode;
                this.chaosModeClassic = MapEmbiggener.chaosModeClassic;
            }
            return base.OnGameStart(gm);
        }
        public override IEnumerator OnBattleStart(IGameModeHandler gm)
        {
            this.battleOnGoing = true;
            this.chaosModeClassicSign *= -1;
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
                this.RotationTarget = null;
                this.RotationSpeed = null;
                this.PositionTarget = null;
                this.MovementSpeed = null;
            }
            else if (this.chaosMode && this.battleOnGoing)
            {
                //this.ZoomTarget = 0f;
                //this.ZoomSpeed = ChaosModeZoomSpeed;
                this.ZoomTarget = null;
                this.ZoomSpeed = null;
                this.RotationTarget = null;
                this.RotationSpeed = null;
                this.PositionTarget = null;
                this.MovementSpeed = null;
            }
            else if (this.chaosModeClassic && this.battleOnGoing)
            {
                this.ZoomTarget = 0f;
                this.ZoomSpeed = ChaosModeClassicZoomSpeed;
                this.RotationTarget = Quaternion.Euler(ControllerManager.CameraRotation.eulerAngles + new Vector3(0f, 0f, this.chaosModeClassicSign));
                this.RotationSpeed = ChaosModeClassicRotationSpeed;
                this.PositionTarget = null;
                this.MovementSpeed = null;
            }
            else
            {
                this.ZoomTarget = null;
                this.ZoomSpeed = null;
                this.RotationTarget = null;
                this.RotationSpeed = null;
                this.PositionTarget = null;
                this.MovementSpeed = null;
            }
        }

        public override void SetDataToSync()
        {
            this.SyncedIntData["SD"] = this.suddenDeathMode ? 1 : 0;
            this.SyncedIntData["CM"] = this.chaosMode ? 1 : 0;
            this.SyncedIntData["CMC"] = this.chaosModeClassic ? 1 : 0;
            this.SyncedIntData["CMCS"] = this.chaosModeClassicSign;
        }

        public override void ReadSyncedData()
        {
            this.suddenDeathMode = this.SyncedIntData["SD"] == 1;
            this.chaosMode = this.SyncedIntData["CM"] == 1;
            this.chaosModeClassic = this.SyncedIntData["CMC"] == 1;
            this.chaosModeClassicSign = this.SyncedIntData["CMCS"];
        }

        public override bool SyncDataNow()
        {
            return true;
        }
    }
}
