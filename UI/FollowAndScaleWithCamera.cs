using UnityEngine;
using MapEmbiggener.Controllers;
namespace MapEmbiggener.UI
{
    class FollowAndScaleWithCamera : MonoBehaviour
    {
        void Update()
        {
            // scale particles with camera zoom
            this.transform.localScale = MainCam.instance.cam.orthographicSize / ControllerManager.DefaultZoom * Vector3.one;

            // center particles with camera, instantly, so as to appear not to move at all
            this.transform.position = new Vector3(MainCam.instance.cam.transform.position.x, MainCam.instance.cam.transform.position.y, this.transform.position.z);
        }
    }
}
