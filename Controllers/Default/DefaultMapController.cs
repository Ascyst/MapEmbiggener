using System.Collections;
using UnboundLib.GameModes;
namespace MapEmbiggener.Controllers.Default
{
    public class DefaultMapController : MapController
    {
        public override IEnumerator OnGameStart(IGameModeHandler gm)
        {
            this.MapSize = MapEmbiggener.setSize;
            yield break; 
        }
    }
}
