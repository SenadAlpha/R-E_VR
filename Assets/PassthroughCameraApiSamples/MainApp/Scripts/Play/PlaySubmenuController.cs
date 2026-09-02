// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR;
using PassthroughCameraSamples.MainApp;
using PassthroughCameraSamples.MainApp.Interaction;
using PassthroughCameraSamples.MainApp.TextTools;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MainApp.Play
{
    /// <summary>
    /// Play submenu. Sample scenes are NOT loaded additively — they bring a second
    /// camera rig and dump meshes at world origin (under the floor). Previews spawn
    /// in front of the user instead.
    /// </summary>
    public class PlaySubmenuController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button shaderButton;
        [SerializeField] private Button multiObjectDetectionButton;
        [SerializeField] private Button bouncingBallButton;
        [SerializeField] private Button instantPlacementButton;
        [SerializeField] private Button spawnCubeButton;
        [SerializeField] private Button handInteractionButton;
        [SerializeField] private Button closeButton;

        [Header("Refs")]
        [SerializeField] private BallSpawnerLocal ballSpawnerPrefab;
        [SerializeField] private InstantPlacementSpawner placementSpawner;
        [SerializeField] private HandInteractionPlay handInteractionPlay;
        [SerializeField] private OVRCameraRig cameraRig;
        [SerializeField] private ItemMiniMenu miniMenuPrefab;
        [SerializeField] private PassthroughCameraAccess cameraAccess;
        [SerializeField] private float cubeSpawnDistance = 0.6f;
        [SerializeField] private float openDistance = 0.88f;

        private bool m_initialized;

        private void Awake()
        {
            if (!cameraRig)
            {
                cameraRig = FindFirstObjectByType<OVRCameraRig>();
            }

            if (!cameraAccess)
            {
                cameraAccess = FindFirstObjectByType<PassthroughCameraAccess>();
            }
        }

        private void Start()
        {
            if (m_initialized || !panelRoot)
            {
                return;
            }

            m_initialized = true;
            panelRoot.SetActive(false);

            if (shaderButton) shaderButton.onClick.AddListener(SpawnShaderPreview);
            if (multiObjectDetectionButton) multiObjectDetectionButton.onClick.AddListener(SpawnDetectionPreview);
            if (bouncingBallButton) bouncingBallButton.onClick.AddListener(SpawnBallSpawner);
            if (instantPlacementButton) instantPlacementButton.onClick.AddListener(BeginInstantPlacement);
            if (spawnCubeButton) spawnCubeButton.onClick.AddListener(SpawnCube);
            if (handInteractionButton) handInteractionButton.onClick.AddListener(SpawnHandInteraction);
            if (closeButton) closeButton.onClick.AddListener(Close);
        }

        private void Update()
        {
            if (panelRoot && panelRoot.activeSelf && OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            {
                Close();
            }
        }

        public void Open()
        {
            TextConfigPanel.Instance?.Close();
            if (panelRoot)
            {
                panelRoot.SetActive(true);
                RuntimeUguiBuilder.BindLaser(panelRoot);
                MainAppPlacement.PlaceAlongHead(panelRoot.transform, cameraRig, openDistance);
                MrPointer.Show();
            }
        }

        public void Close()
        {
            if (panelRoot)
            {
                panelRoot.SetActive(false);
            }
        }

        private void SpawnShaderPreview()
        {
            var head = cameraRig.centerEyeAnchor;
            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = head.forward;
            }

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "ShaderWater";
            cube.transform.position = head.position + forward * 0.7f;
            cube.transform.localScale = new Vector3(0.35f, 0.35f, 0.08f);
            cube.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);

            var renderer = cube.GetComponent<MeshRenderer>();
            var material = MrShaders.CreateWaterMaterial();
            if (material)
            {
                renderer.material = material;
            }

            BindCamera(cube, renderer, true);
            MakeSelectable(cube, SpawnableItemType.Photo);
            Close();
        }

        private void SpawnDetectionPreview()
        {
            var quad = SpawnPreviewQuad("DetectionPreview", 0.48f, 0.3f);
            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.material = MrShaders.CreateUnlitTexture(null);
            BindCamera(quad, renderer, false);
            MakeSelectable(quad, SpawnableItemType.Photo);
            Close();
        }

        private GameObject SpawnPreviewQuad(string name, float width, float height)
        {
            var head = cameraRig.centerEyeAnchor;
            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = head.forward;
            }

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.position = head.position + forward * cubeSpawnDistance;
            quad.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);
            quad.transform.localScale = new Vector3(width, height, 1f);
            var box = quad.AddComponent<BoxCollider>();
            box.size = new Vector3(1f, 1f, 0.04f);
            return quad;
        }

        private void BindCamera(GameObject host, Renderer renderer, bool useMainTexProperty)
        {
            if (!cameraAccess)
            {
                cameraAccess = FindFirstObjectByType<PassthroughCameraAccess>();
            }

            var binder = host.AddComponent<CameraTextureBinder>();
            binder.Configure(cameraAccess, renderer, useMainTexProperty);
        }

        private void MakeSelectable(GameObject host, SpawnableItemType type)
        {
            var item = host.AddComponent<SpawnableItem>();
            item.Initialize(type);
            item.ConfigureMiniMenu(miniMenuPrefab);
            item.Select();
        }

        private void SpawnBallSpawner()
        {
            if (!ballSpawnerPrefab)
            {
                Debug.LogError("[PlaySubmenuController] No ball spawner prefab assigned.");
                return;
            }

            var head = cameraRig.centerEyeAnchor;
            var instance = Instantiate(ballSpawnerPrefab, head.position + head.forward * 0.5f, Quaternion.identity);
            instance.gameObject.SetActive(true);
            Close();
        }

        private void BeginInstantPlacement()
        {
            if (!placementSpawner)
            {
                Debug.LogError("[PlaySubmenuController] No InstantPlacementSpawner assigned.");
                return;
            }

            placementSpawner.BeginPlacement();
            Close();
        }

        private void SpawnCube()
        {
            Close();
            RoomPhysicsBootstrap.RequestScanThen(SpawnCubeNow);
        }

        private void SpawnCubeNow()
        {
            var head = cameraRig.centerEyeAnchor;
            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = head.forward;
            }

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "CubeItem";
            cube.transform.position = PlayPhysicsGround.LandingInFront(head, cubeSpawnDistance);
            cube.transform.localScale = Vector3.one * 0.15f;
            cube.GetComponent<MeshRenderer>().material = MrShaders.CreateUnlitTexture(Texture2D.whiteTexture);
            cube.GetComponent<MeshRenderer>().material.color = new Color(0.25f, 0.7f, 1f, 1f);
            cube.transform.SetParent(MainAppPlacement.SpawnRoot(), true);

            var phys = new PhysicsMaterial
            {
                name = "CubeToyMat",
                dynamicFriction = 0.4f,
                staticFriction = 0.5f,
                bounciness = 0.22f
            };
            cube.GetComponent<Collider>().material = phys;

            var item = cube.AddComponent<SpawnableItem>();
            item.Initialize(SpawnableItemType.Cube);
            item.ConfigureMiniMenu(miniMenuPrefab);
            cube.AddComponent<CubeHoverToy>();
            PlayPhysicsGround.DestroyFakes();
        }

        private void SpawnHandInteraction()
        {
            if (!handInteractionPlay)
            {
                handInteractionPlay = FindAnyObjectByType<HandInteractionPlay>();
            }

            if (!handInteractionPlay)
            {
                Debug.LogError("[PlaySubmenuController] No HandInteractionPlay assigned.");
                return;
            }

            Close();
            RoomPhysicsBootstrap.RequestScanThen(() => handInteractionPlay.SpawnSession());
        }
    }
}
