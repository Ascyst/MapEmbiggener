using UnityEngine;
using UnboundLib;
using MapEmbiggener.Controllers;

namespace MapEmbiggener.UI
{

    public class OutOfBoundsParticles : MonoBehaviour
    {

        public static OutOfBoundsParticles instance;

        void Awake()
        {
            OutOfBoundsParticles.instance = this;
        }

        private const int layer = 26;
        public const float DefaultGravity = 0f;
        public static readonly Color DefaultColorMin = new Color(0f, 0f, 0f, 0.15f);
        public static readonly Color DefaultColorMax = new Color(1f, 0f, 0f, 0.15f);

        private static GameObject _Cam = null;

        public static GameObject Cam
        {
            get
            {
                if (OutOfBoundsParticles._Cam != null) { return OutOfBoundsParticles._Cam; }

                OutOfBoundsParticles._Cam = new GameObject("OutOfBoundsParticlesCam", typeof(Camera));
                OutOfBoundsParticles._Cam.transform.SetParent(UnityEngine.GameObject.Find("/Game/Visual/Rendering ").transform);
                OutOfBoundsParticles._Cam.GetComponent<Camera>().CopyFrom(MainCam.instance.cam);
                OutOfBoundsParticles._Cam.GetComponent<Camera>().depth = 4;
                OutOfBoundsParticles._Cam.GetComponent<Camera>().cullingMask = (1 << OutOfBoundsParticles.layer);
                DontDestroyOnLoad(OutOfBoundsParticles._Cam);

                UnityEngine.GameObject.Find("/Game/Visual/Rendering ").gameObject.GetComponent<CameraZoomHandler>().InvokeMethod("Start");

                return OutOfBoundsParticles._Cam;
            }
        }

        private GameObject _Mask = null;

        public GameObject Mask
        {
            get
            {
                if (this._Mask != null) { return this._Mask; }

                this._Mask = GameObject.Instantiate(ListMenu.instance.bar, this.gameObject.transform);
                this._Mask.name = "Mask";
                this._Mask.SetActive(true);
                this._Mask.layer = OutOfBoundsParticles.layer;
                this._Mask.GetOrAddComponent<RectTransform>();
                this._Mask.transform.localScale = Vector3.one;
                this._Mask.transform.position = new Vector3(0f, 0f, 1f);
                DontDestroyOnLoad(this._Mask);

                return this._Mask;
            }
        }

        private GameObject _Particles = null;

        public GameObject Particles
        {
            get
            {
                if (this._Particles != null) { return this._Particles; }
                if (this == null || this.gameObject == null || this.gameObject.transform == null) { return null; }
                if (UnityEngine.GameObject.Find("Game/UI/UI_MainMenu/Canvas/Particle") == null) { return null; }

                GameObject ParticleHolder = new GameObject("Particles");
                ParticleHolder.transform.SetParent(this.gameObject.transform);
                this._Particles = GameObject.Instantiate(UnityEngine.GameObject.Find("Game/UI/UI_MainMenu/Canvas/Particle"), ParticleHolder.transform);
                this._Particles.name = "OutOfBoundsParticles";
                //this._Particles.transform.SetParent(OutOfBoundsParticles.Group.transform);
                //this._Particles.SetActive(false);
                this._Particles.SetActive(true);
                /*
                this._Particles.transform.localScale = 100f * Vector3.one;
                this._Particles.GetComponent<SpriteRenderer>().sortingOrder = 0;
                this._Particles.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
                */
                this._Particles.layer = OutOfBoundsParticles.layer;
                DontDestroyOnLoad(this._Particles);

                return this._Particles;
            }
        }

        private ParticleSystem ParticleSystem => this?.Particles?.GetComponentInChildren<ParticleSystem>();

        private const float Factor = 27.05f;
        public static void SetSpriteSize(SpriteMask spriteRenderer, Vector2 size)
        {
            float xMult = size.x/Factor;
            float yMult = size.y/Factor;
            Vector3 currentScale = spriteRenderer.transform.localScale;
            spriteRenderer.transform.localScale = new Vector3(xMult, yMult, currentScale.z);

            return;
        }

        public void SetColor(Color? colorMax = null, Color? colorMin = null)
        {
            // set the particle color
            if (this.ParticleSystem == null) { return; }
            ParticleSystem.MainModule main = this.ParticleSystem.main;
            ParticleSystem.MinMaxGradient startColor = main.startColor;
            startColor.colorMax = colorMax ?? DefaultColorMax;
            startColor.colorMin = colorMin ?? DefaultColorMin;
            main.startColor = startColor;
            this.ParticleSystem.Play();
        }

        public void SetGravity(float gravity = DefaultGravity)
        {
            if (this.ParticleSystem == null) { return; }
            ParticleSystem.MainModule main = this.ParticleSystem.main;
            main.gravityModifier = gravity;
            this.ParticleSystem.Play();
        }

        void Start()
        {

            // force create the camera
            GameObject Cam = OutOfBoundsParticles.Cam;
            Cam.transform.position = Vector3.zero;

            // set the particle sorting layers properly
            this.Particles.GetComponentInChildren<ParticleSystemRenderer>().sortingOrder = 2;
            this.Mask.GetComponent<SpriteMask>().frontSortingOrder = 3;
            this.Mask.GetComponent<SpriteMask>().backSortingOrder = 2;
            this.Particles.GetComponentInChildren<ParticleSystemRenderer>().maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
            this.Particles.layer = OutOfBoundsParticles.layer;

            this.SetColor(DefaultColorMax, DefaultColorMin);

            this.gameObject.transform.position = Vector3.zero;
            this.Particles.transform.position = new Vector3(0f,0f,1f);
            this.Particles.transform.localScale = Vector3.one;
            foreach (Transform child in this.Particles.transform)
            {
                child.localPosition = new Vector3(0f,0f,100f);
                child.gameObject.layer = OutOfBoundsParticles.layer;
            }
            this.gameObject.layer = OutOfBoundsParticles.layer;

            OutOfBoundsUtils.particleMask = this.Mask;
        }

        void Update()
        {
            // scale particles with map size
            this.Particles.transform.localScale = ControllerManager.MapSize * Vector3.one;
        }
        void LateUpdate()
        {
            // center particles with camera, delayed (Late Update), and smoothly
            Vector3 target = new Vector3(ControllerManager.CameraPosition.x, ControllerManager.CameraPosition.y, this.Particles.transform.position.z);

            // constrain the target position to be within the bounds
            target = OutOfBoundsUtils.GetPoint(OutOfBoundsUtils.InverseGetPoint(target));

            this.Particles.transform.position = Vector3.MoveTowards(this.Particles.transform.position, target, Vector3.Distance(this.Particles.transform.position, target) * Time.deltaTime);
        }
    }
}
