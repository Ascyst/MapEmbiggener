using System.Collections;
using System.Linq;
using UnboundLib.GameModes;
using Photon.Pun;
using UnityEngine;
using MapEmbiggener.UI;

namespace MapEmbiggener.Controllers.Default
{
    public class DefaultBoundsController : BoundsController
    {
        public static readonly float SuddenDeathXSpeed = (16f / 9f) * SuddenDeathYSpeed;
        public const float SuddenDeathYSpeed = 1.5f;

        public static readonly float ChaosModeXSpeed = (16f / 9f) * ChaosModeYSpeed;
        public const float ChaosModeYSpeed = 0.25f;
        public const float ChaosModeAngularSpeed = 5f;

        public static readonly float ChaosModeClassicXSpeed = (16f / 9f) * ChaosModeClassicYSpeed;
        public const float ChaosModeClassicYSpeed = 2f;

        public const float ClosingFrac = 0.9f;

        private int PlayersAlive => PlayerManager.instance.players.Where(p => !p.data.dead).Select(p => p.playerID).Distinct().Count();
        private bool battleOnGoing = false;
        private bool suddenDeathMode = false;
        private bool chaosMode = false;
        private bool chaosModeClassic = false;
        private int chaosModeSign = -1;
        public override void SetDataToSync()
        {
            this.SyncedIntData["SD"] = this.suddenDeathMode ? 1 : 0;
            this.SyncedIntData["CM"] = this.chaosMode ? 1 : 0;
            this.SyncedIntData["CMS"] = this.chaosModeSign;
            this.SyncedIntData["CMC"] = this.chaosModeClassic ? 1 : 0;
        }

        public override void ReadSyncedData()
        {
            this.suddenDeathMode = this.SyncedIntData["SD"] == 1;
            this.chaosMode = this.SyncedIntData["CM"] == 1;
            this.chaosModeSign = this.SyncedIntData["CMS"];
            this.chaosModeClassic = this.SyncedIntData["CMC"] == 1;
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
            this.chaosModeSign = -1;
            this.AngleTarget = 0f;
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
            if (this.suddenDeathMode && this.battleOnGoing)
            {
                this.Damage = OutOfBoundsDamage.Normal;
                if (this.PlayersAlive == 2)
                {
                    this.MaxXTarget = ClosingFrac * OutOfBoundsUtils.defaultX * ControllerManager.MapSize * ControllerManager.Zoom / (MapManager.instance?.currentMap?.Map?.size ?? 1f ) ;
                    this.MinXTarget = -ClosingFrac * OutOfBoundsUtils.defaultX * ControllerManager.MapSize * ControllerManager.Zoom / (MapManager.instance?.currentMap?.Map?.size ?? 1f ) ;
                    this.MaxYTarget = 0f;
                    this.MinYTarget = 0f;
                    this.ParticleGravityTarget = -0.1f;
                    this.ParticleGravitySpeed = null;
                }
                else
                {
                    this.MaxXTarget = OutOfBoundsUtils.defaultX * ControllerManager.MapSize;
                    this.MinXTarget = -OutOfBoundsUtils.defaultX * ControllerManager.MapSize;
                    this.MaxYTarget = OutOfBoundsUtils.defaultY * ControllerManager.MapSize;
                    this.MinYTarget = -OutOfBoundsUtils.defaultY * ControllerManager.MapSize;
                    this.ParticleGravityTarget = 0f;
                    this.ParticleGravitySpeed = null;
                }
                this.XSpeed = SuddenDeathXSpeed;
                this.YSpeed = SuddenDeathYSpeed;
                this.AngleTarget = 0f;
                this.AngularSpeed = null;
            }
            else if (this.chaosMode && this.battleOnGoing)
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
            else if (this.chaosModeClassic && this.battleOnGoing)
            {
                this.Damage = OutOfBoundsDamage.Normal;
                this.MaxXTarget =  ClosingFrac * OutOfBoundsUtils.defaultX * ControllerManager.MapSize * ControllerManager.Zoom / (MapManager.instance?.currentMap?.Map?.size ?? 1f ) ;
                this.MinXTarget =  -ClosingFrac * OutOfBoundsUtils.defaultX * ControllerManager.MapSize * ControllerManager.Zoom / (MapManager.instance?.currentMap?.Map?.size ?? 1f ) ;
                this.MaxYTarget =  ClosingFrac * OutOfBoundsUtils.defaultY * ControllerManager.MapSize * ControllerManager.Zoom / (MapManager.instance?.currentMap?.Map?.size ?? 1f ) ;
                this.MinYTarget =  -ClosingFrac * OutOfBoundsUtils.defaultY * ControllerManager.MapSize * ControllerManager.Zoom / (MapManager.instance?.currentMap?.Map?.size ?? 1f ) ;
                this.XSpeed = ChaosModeClassicXSpeed;
                this.YSpeed = ChaosModeClassicYSpeed;
                this.AngleTarget = ControllerManager.CameraRotation.eulerAngles.z;
                this.AngularSpeed = null;
                if (UnityEngine.Random.Range(0f,1f) < 0.005f)
                {
                    this.ParticleGravityTarget = UnityEngine.Random.Range(-1f,1f);
                }
                this.ParticleGravitySpeed = null;
                this.ParticleColorMaxTarget = new Color(1f, UnityEngine.Random.Range(0f, 0.5f), UnityEngine.Random.Range(0f, 0.5f), OutOfBoundsParticles.DefaultColorMax.a);
                this.ColorSpeed = null;
            }
            else
            {
                this.Damage = OutOfBoundsDamage.Normal;
                this.MaxXTarget = OutOfBoundsUtils.defaultX * ControllerManager.MapSize;
                this.MinXTarget = -OutOfBoundsUtils.defaultX * ControllerManager.MapSize;
                this.MaxYTarget = OutOfBoundsUtils.defaultY * ControllerManager.MapSize;
                this.MinYTarget = -OutOfBoundsUtils.defaultY * ControllerManager.MapSize;
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
