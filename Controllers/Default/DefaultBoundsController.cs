using System.Collections;
using System.Linq;
using UnboundLib.GameModes;

namespace MapEmbiggener.Controllers.Default
{
    public class DefaultBoundsController : BoundsController
    {
        public static readonly float SuddenDeathXSpeed = (16f / 9f) * SuddenDeathYSpeed;
        public const float SuddenDeathYSpeed = 1.5f;

        public static readonly float ChaosModeXSpeed = (16f / 9f) * ChaosModeYSpeed;
        public const float ChaosModeYSpeed = 0.5f;
        public const float ChaosModeAngularSpeed = 5f;

        private int PlayersAlive => PlayerManager.instance.players.Where(p => !p.data.dead).Select(p => p.playerID).Distinct().Count();
        private bool battleOnGoing = false;
        private int chaosModeSign = -1;
        public override IEnumerator OnBattleStart(IGameModeHandler gm)
        {
            this.battleOnGoing = true;
            this.chaosModeSign *= -1;
            return base.OnBattleStart(gm);
        }
        public override IEnumerator OnPointEnd(IGameModeHandler gm)
        {
            this.battleOnGoing = false;
            return base.OnPointEnd(gm);
        }
        public override void OnUpdate()
        {
            if (MapEmbiggener.suddenDeathMode && this.battleOnGoing)
            {
                this.Damage = OutOfBoundsDamage.Normal;
                if (this.PlayersAlive == 2)
                {
                    this.MaxXTarget = -1f + OutOfBoundsUtils.defaultX * ControllerManager.Zoom / (MapManager.instance?.currentMap?.Map?.size ?? 1f ) ;
                    this.MinXTarget = 1f - OutOfBoundsUtils.defaultX * ControllerManager.Zoom / (MapManager.instance?.currentMap?.Map?.size ?? 1f ) ;
                    this.MaxYTarget = 0f;
                    this.MinYTarget = 0f;
                    this.ParticleGravityTarget = -0.1f;
                    this.ParticleGravitySpeed = null;
                }
                else
                {
                    this.MaxXTarget = OutOfBoundsUtils.defaultX * MapEmbiggener.setSize;
                    this.MinXTarget = -OutOfBoundsUtils.defaultX * MapEmbiggener.setSize;
                    this.MaxYTarget = OutOfBoundsUtils.defaultY * MapEmbiggener.setSize;
                    this.MinYTarget = -OutOfBoundsUtils.defaultY * MapEmbiggener.setSize;
                    this.ParticleGravityTarget = 0f;
                    this.ParticleGravitySpeed = null;
                }
                this.XSpeed = SuddenDeathXSpeed;
                this.YSpeed = SuddenDeathYSpeed;
                this.AngleTarget = 0f;
                this.AngularSpeed = null;
            }
            else if (MapEmbiggener.chaosMode && this.battleOnGoing)
            {
                this.Damage = OutOfBoundsDamage.OverTime;
                this.MaxXTarget = 0f;
                this.MinXTarget = 0f;
                this.MaxYTarget = 0f;
                this.MinYTarget = 0f;
                this.XSpeed = ChaosModeXSpeed;
                this.YSpeed = ChaosModeYSpeed;
                this.AngleTarget = ControllerManager.Angle + this.chaosModeSign;
                this.AngularSpeed = ChaosModeAngularSpeed;
                this.ParticleGravityTarget = this.chaosModeSign;
                this.ParticleGravitySpeed = null;
            }
            else
            {
                this.Damage = OutOfBoundsDamage.Normal;
                this.MaxXTarget = OutOfBoundsUtils.defaultX * MapEmbiggener.setSize;
                this.MinXTarget = -OutOfBoundsUtils.defaultX * MapEmbiggener.setSize;
                this.MaxYTarget = OutOfBoundsUtils.defaultY * MapEmbiggener.setSize;
                this.MinYTarget = -OutOfBoundsUtils.defaultY * MapEmbiggener.setSize;
                this.AngleTarget = 0f;
                this.XSpeed = null;
                this.YSpeed = null;
                this.AngularSpeed = null;
                this.ParticleGravityTarget = null;
                this.ParticleGravitySpeed = null;

            }
        }
    }
}
