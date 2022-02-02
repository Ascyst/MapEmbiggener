namespace MapEmbiggener.Controllers.Default
{
    public class DefaultBoundsController : BoundsController
    {
        public override void OnUpdate()
        {
            this.MaxXTarget = OutOfBoundsUtils.defaultX * MapEmbiggener.setSize;
            this.MinXTarget = -OutOfBoundsUtils.defaultX * MapEmbiggener.setSize;
            this.MaxYTarget = OutOfBoundsUtils.defaultY * MapEmbiggener.setSize;
            this.MinYTarget = -OutOfBoundsUtils.defaultY * MapEmbiggener.setSize;
            this.AngleTarget = 0f;
        }
    }
}
