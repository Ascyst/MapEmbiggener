using UnboundLib.GameModes;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace MapEmbiggener.Controllers
{
    public abstract class BoundsController : IBoundsController
    {
		public bool CallUpdate { get; protected set; } = true;
        private bool savedCallUpdate;
		private float? savedMaxXTarget = null;
		private float? savedMaxYTarget = null;
		private float? savedMinXTarget = null;
		private float? savedMinYTarget = null;
		private float? savedAngleTarget = null;
		private float? savedXSpeed = null;
		private float? savedYSpeed = null;
		private float? savedAngularSpeed = null;
        public OutOfBoundsDamage? Damage { get; protected set; } = null;
        public float? MaxXTarget { get; protected set; } = null;

        public float? MaxYTarget { get; protected set; } = null;

        public float? MinXTarget { get; protected set; } = null;

        public float? MinYTarget { get; protected set; } = null;

        public float? AngleTarget { get; protected set; } = null;

        public float? XSpeed { get; protected set; } = null;

        public float? YSpeed { get; protected set; } = null;

        public float? AngularSpeed { get; protected set; } = null;

		public Color? ParticleColorMinTarget { get; protected set; } = null;

        public Color? ParticleColorMaxTarget { get; protected set; } = null;

        public Color? BorderColorTarget { get; protected set; } = null;

        public float? ColorSpeed { get; protected set; } = null;

        public float? ParticleGravityTarget { get; protected set; } = null;

        public float? ParticleGravitySpeed { get; protected set; } = null;
        public Dictionary<string, int> SyncedIntData { get; set; } = new Dictionary<string, int>() { };
        public Dictionary<string, float> SyncedFloatData { get; set; } = new Dictionary<string, float>() { };
        public Dictionary<string, string> SyncedStringData { get; set; } = new Dictionary<string, string>() { };
        public abstract void SetDataToSync();
        public abstract void ReadSyncedData();
        public abstract bool SyncDataNow();
        void IBoundsController.ReceiveSyncedBoundsData(bool callUpdate, OutOfBoundsDamage? damage, float? maxXTarget, float? maxYTarget, float? minXTarget, float? minYTarget, float? angleTarget, float? xSpeed, float? ySpeed, float? angularSpeed)
        {
            this.CallUpdate = callUpdate;
            this.Damage = damage;
            this.MaxXTarget = maxXTarget;
            this.MaxYTarget = maxYTarget;
            this.MinXTarget = minXTarget;
            this.MinYTarget = minYTarget;
            this.AngleTarget = angleTarget;
            this.XSpeed = xSpeed;
            this.YSpeed = ySpeed;
            this.AngularSpeed = angularSpeed;
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
            this.MaxXTarget = null;
            this.MaxYTarget = null;
            this.MinXTarget = null;
            this.MinYTarget = null;
            this.AngleTarget = null;
            this.XSpeed = null;
            this.YSpeed = null;
            this.AngularSpeed = null;
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
        public virtual IEnumerator OnPickStart(IGameModeHandler gm)
        {
            this.savedCallUpdate = this.CallUpdate;
            this.CallUpdate = false;
			// reset during pick phase
			this.savedMaxXTarget = this.MaxXTarget;
			this.savedMaxYTarget = this.MaxYTarget;
			this.savedMinXTarget = this.MinXTarget;
			this.savedMinYTarget = this.MinYTarget;
			this.savedAngleTarget = this.AngleTarget;
			this.savedXSpeed = this.XSpeed;
			this.savedYSpeed = this.YSpeed;
			this.savedAngularSpeed = this.AngularSpeed;
            this.MaxXTarget = null;
            this.MaxYTarget = null;
            this.MinXTarget = null;
            this.MinYTarget = null;
            this.AngleTarget = null;
            this.XSpeed = null;
            this.YSpeed = null;
            this.AngularSpeed = null;
            yield break;
        }
        public virtual IEnumerator OnPickEnd(IGameModeHandler gm)
        {
            // restore settings after pick phase
            this.CallUpdate = this.savedCallUpdate;
			this.MaxXTarget = this.savedMaxXTarget;
			this.MaxYTarget = this.savedMaxYTarget;
			this.MinXTarget = this.savedMinXTarget;
			this.MinYTarget = this.savedMinYTarget;
			this.AngleTarget = this.savedAngleTarget;
			this.XSpeed = this.savedXSpeed;
			this.YSpeed = this.savedYSpeed;
			this.AngularSpeed = this.savedAngularSpeed;
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
