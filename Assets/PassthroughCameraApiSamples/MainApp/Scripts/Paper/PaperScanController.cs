// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR;
using PassthroughCameraSamples.MainApp;
using PassthroughCameraSamples.MainApp.Interaction;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MainApp.Paper
{
    /// <summary>
    /// Live passthrough preview + CPU document detect/warp (Jinsan DocumentScanner path).
    /// A or the 캡처 button grabs a flattened paper. B / 취소 closes.
    /// </summary>
    public class PaperScanController : MonoBehaviour
    {
        [SerializeField] private PassthroughCameraAccess cameraAccess;
        [SerializeField] private OVRCameraRig cameraRig;
        [SerializeField] private Shader edgeShader;
        [SerializeField] private ItemMiniMenu miniMenuPrefab;
        [SerializeField] private Button captureButton;
        [SerializeField] private Button cancelButton;

        [Header("Corner Markers")]
        [SerializeField] private GameObject scanRoot;
        [SerializeField] private Transform[] cornerMarkers = new Transform[4];

        [Header("Detection")]
        [SerializeField] private float detectionInterval = 0.25f;

        [Header("Framing")]
        [SerializeField] private float scanDistance = 0.5f;
        [SerializeField] private float scanPlaneSize = 0.65f;
        [SerializeField] private GameObject instructionRoot;

        private bool m_isOpen;
        private float m_timeSinceLastDetect;
        private Vector2[] m_lastCornersUv;
        private Vector2[] m_lastCornersPx;
        private int m_lastWidth;
        private int m_lastHeight;

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

            if (scanRoot)
            {
                scanRoot.SetActive(false);
            }

            if (instructionRoot)
            {
                instructionRoot.SetActive(false);
            }
        }

        public void OpenScan()
        {
            StopAllCoroutines();
            StartCoroutine(OpenWhenReady());
        }

        private System.Collections.IEnumerator OpenWhenReady()
        {
            if (!cameraAccess)
            {
                cameraAccess = FindFirstObjectByType<PassthroughCameraAccess>();
            }

            var waited = 0f;
            while (cameraAccess && !cameraAccess.IsPlaying && waited < 4f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            EnsureScanPreview();
            WireButtons();

            m_isOpen = true;
            m_lastCornersUv = null;
            m_lastCornersPx = null;
            m_timeSinceLastDetect = detectionInterval;
            if (scanRoot)
            {
                scanRoot.SetActive(true);
            }

            if (instructionRoot)
            {
                instructionRoot.SetActive(true);
                RuntimeUguiBuilder.BindLaser(instructionRoot);
                var label = instructionRoot.GetComponentInChildren<Text>(true);
                if (label)
                {
                    label.text = cameraAccess && cameraAccess.IsPlaying
                        ? "종이를 프레임에. 초록 점이면 보정 캡처. A/캡처, B/취소"
                        : "카메라 아직 없음. 권한/Quest 3 확인. B 닫기";
                }
            }

            FollowScanFrame();
            MrPointer.Show();
        }

        private void WireButtons()
        {
            if (captureButton)
            {
                captureButton.onClick.RemoveListener(CapturePaper);
                captureButton.onClick.AddListener(CapturePaper);
            }

            if (cancelButton)
            {
                cancelButton.onClick.RemoveListener(CloseScan);
                cancelButton.onClick.AddListener(CloseScan);
            }
        }

        private void CloseScan()
        {
            m_isOpen = false;
            if (scanRoot)
            {
                scanRoot.SetActive(false);
            }

            if (instructionRoot)
            {
                instructionRoot.SetActive(false);
            }
        }

        private void Update()
        {
            if (!m_isOpen)
            {
                return;
            }

            FollowScanFrame();

            m_timeSinceLastDetect += Time.deltaTime;
            if (cameraAccess && cameraAccess.IsPlaying && m_timeSinceLastDetect >= detectionInterval)
            {
                m_timeSinceLastDetect = 0f;
                DetectPaper();
            }

            UpdateMarkers();

            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                CapturePaper();
            }
            else if (OVRInput.GetDown(OVRInput.Button.Two))
            {
                CloseScan();
            }
        }

        private void DetectPaper()
        {
            if (!TryReadFlippedFrame(out var pixels, out var width, out var height))
            {
                m_lastCornersUv = null;
                m_lastCornersPx = null;
                return;
            }

            m_lastWidth = width;
            m_lastHeight = height;
            if (!PaperDocumentDetector.TryFind(pixels, width, height, out var corners))
            {
                m_lastCornersUv = null;
                m_lastCornersPx = null;
                return;
            }

            m_lastCornersPx = corners;
            m_lastCornersUv = new Vector2[4];
            for (var i = 0; i < 4; i++)
            {
                m_lastCornersUv[i] = new Vector2(
                    1f - corners[i].x / Mathf.Max(1, width - 1),
                    corners[i].y / Mathf.Max(1, height - 1));
            }
        }

        private bool TryReadFlippedFrame(out Color32[] pixels, out int width, out int height)
        {
            pixels = null;
            width = 0;
            height = 0;
            if (!cameraAccess || !cameraAccess.IsPlaying)
            {
                return false;
            }

            var resolution = cameraAccess.CurrentResolution;
            NativeArray<Color32> native = default;
            try
            {
                native = cameraAccess.GetColors();
            }
            catch (System.Exception)
            {
                native = default;
            }

            if (native.IsCreated && native.Length >= resolution.x * resolution.y)
            {
                pixels = native.ToArray();
                width = resolution.x;
                height = resolution.y;
                return true;
            }

            var source = cameraAccess.GetTexture();
            if (!source)
            {
                return false;
            }

            width = source.width;
            height = source.height;
            var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt);
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            pixels = tex.GetPixels32();
            Object.Destroy(tex);
            return true;
        }

        private (Vector3 center, Vector3 right, Vector3 up) GetScanFrame()
        {
            var head = cameraRig.centerEyeAnchor;
            return (head.position + head.forward * scanDistance - head.up * 0.1f, head.right, head.up);
        }

        private Vector3 UvToWorld(Vector2 uv, Vector3 center, Vector3 right, Vector3 up)
        {
            var localX = (uv.x - 0.5f) * scanPlaneSize;
            var localY = (0.5f - uv.y) * scanPlaneSize;
            return center + right * localX + up * localY;
        }

        private void UpdateMarkers()
        {
            if (m_lastCornersUv == null || cornerMarkers == null || cornerMarkers.Length < 4)
            {
                if (cornerMarkers == null)
                {
                    return;
                }

                foreach (var marker in cornerMarkers)
                {
                    if (marker)
                    {
                        marker.gameObject.SetActive(false);
                    }
                }

                return;
            }

            var (center, right, up) = GetScanFrame();
            for (var i = 0; i < 4; i++)
            {
                if (!cornerMarkers[i])
                {
                    continue;
                }

                cornerMarkers[i].gameObject.SetActive(true);
                cornerMarkers[i].position = UvToWorld(m_lastCornersUv[i], center, right, up);
            }
        }

        private void FollowScanFrame()
        {
            if (!scanRoot || !cameraRig)
            {
                return;
            }

            MainAppPlacement.PlaceInFrontOfUser(scanRoot.transform, cameraRig, scanDistance);
            MainAppPlacement.NudgeDown(scanRoot.transform, cameraRig, 0.1f);
            if (instructionRoot)
            {
                instructionRoot.transform.position = scanRoot.transform.position + Vector3.up * 0.28f;
                instructionRoot.transform.rotation = scanRoot.transform.rotation;
            }
        }

        private void EnsureScanPreview()
        {
            if (!scanRoot)
            {
                return;
            }

            var frame = scanRoot.transform.Find("ScanFrame");
            if (!frame)
            {
                return;
            }

            var renderer = frame.GetComponent<MeshRenderer>();
            if (!renderer)
            {
                return;
            }

            renderer.material = MrShaders.CreateUnlitTexture(Texture2D.whiteTexture) ?? renderer.material;
            var binder = frame.GetComponent<CameraTextureBinder>();
            if (!binder && cameraAccess)
            {
                binder = frame.gameObject.AddComponent<CameraTextureBinder>();
            }

            binder?.Configure(cameraAccess, renderer, true, true);
        }

        private void CapturePaper()
        {
            if (!TryReadFlippedFrame(out var pixels, out var width, out var height))
            {
                Debug.LogWarning("[PaperScanController] No camera frame. Capture aborted.");
                return;
            }

            Texture2D capture = null;
            var lookingDown = cameraRig && cameraRig.centerEyeAnchor
                              && Vector3.Dot(cameraRig.centerEyeAnchor.forward, Vector3.down) > 0.28f;
            var corners = m_lastCornersPx;
            if ((corners == null || corners.Length < 4) &&
                PaperDocumentDetector.TryFind(pixels, width, height, out var found))
            {
                corners = found;
            }

            if (corners != null && corners.Length >= 4)
            {
                if (lookingDown || PaperDocumentDetector.IsAlmostRectangle(corners))
                {
                    capture = PaperDocumentDetector.CropAABB(pixels, width, height, corners);
                }
                else
                {
                    capture = PaperDocumentDetector.Warp(pixels, width, height, corners);
                }
            }

            if (!capture)
            {
                capture = PaperDocumentDetector.ToTexture(pixels, width, height);
            }

            var material = MrShaders.CreateUnlitTexture(capture);
            if (!material)
            {
                Debug.LogError("[PaperScanController] Unlit texture shader missing. Capture aborted.");
                return;
            }

            material.mainTextureScale = new Vector2(-1f, 1f);
            material.mainTextureOffset = new Vector2(1f, 0f);
            if (material.HasProperty("_MainTex"))
            {
                material.SetTextureScale("_MainTex", new Vector2(-1f, 1f));
                material.SetTextureOffset("_MainTex", new Vector2(1f, 0f));
            }

            SpawnPaper(capture, material);
            CloseScan();
        }

        private void SpawnPaper(Texture2D capture, Material material)
        {
            var (center, _, _) = GetScanFrame();
            var head = cameraRig.centerEyeAnchor;
            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = head.forward;
            }

            var root = GameObject.CreatePrimitive(PrimitiveType.Quad);
            root.name = "PaperItem";
            root.transform.SetParent(MainAppPlacement.SpawnRoot(), true);
            root.transform.position = center;
            root.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);
            var aspect = (float)capture.width / Mathf.Max(1, capture.height);
            root.transform.localScale = new Vector3(0.42f * aspect, 0.42f, 1f);
            root.GetComponent<MeshRenderer>().material = material;

            var item = root.AddComponent<SpawnableItem>();
            item.Initialize(SpawnableItemType.Paper);
            item.ConfigureMiniMenu(miniMenuPrefab);
            item.Select();
        }

        private void OnDestroy()
        {
            _ = edgeShader;
        }
    }
}
