using UnboundLib.GameModes;
using System.Collections;
using System.Linq;

namespace MapEmbiggener.Controllers.Default
{
    public class DefaultCameraController : CameraController
    {
        public const float SuddenDeathZoomSpeed = 1f;
        public const float ChaosModeZoomSpeed = 0.25f;
        private int PlayersAlive => PlayerManager.instance.players.Where(p => !p.data.dead).Select(p => p.playerID).Distinct().Count();
        private bool battleOnGoing = false;
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
            if (MapEmbiggener.suddenDeathMode && this.battleOnGoing && this.PlayersAlive == 2)
            {
                this.ZoomTarget = 0f;
                this.ZoomSpeed = SuddenDeathZoomSpeed;
            }
            else if (MapEmbiggener.chaosMode && this.battleOnGoing)
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
    }
}
