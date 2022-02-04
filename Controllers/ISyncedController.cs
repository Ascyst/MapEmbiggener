using System.Collections.Generic;

namespace MapEmbiggener.Controllers
{
    public interface ISyncedController
    {
        Dictionary<string, float> SyncedFloatData { get; set; }        
        Dictionary<string, int> SyncedIntData { get; set; }
        Dictionary<string, string> SyncedStringData { get; set; }
        void SetDataToSync();
        void ReadSyncedData();
        bool SyncDataNow();
    }
}
