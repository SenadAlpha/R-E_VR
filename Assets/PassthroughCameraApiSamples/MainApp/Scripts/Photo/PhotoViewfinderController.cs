// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR;
using PassthroughCameraSamples.MainApp;
using PassthroughCameraSamples.MainApp.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MainApp.Photo
{
    public class PhotoViewfinderController : MonoBehaviour
    {
        [SerializeField] private PassthroughCameraAccess cameraAccess;
        [SerializeField] private OVRCameraRig cameraRig;
        [SerializeField] private GameObject viewfinderRoot;
        [SerializeField] private RawImage viewfinderImage;
        [SerializeField] private ItemMiniMenu miniMenuPrefab;

        [Header("Zoom")]
        [SerializeField] private float minZoom = 1f;
        [SerializeField] private float maxZoom = 3f;
        [SerializeField] private float zoomSpeed = 1.5f;

        [Header("Perspective")]
        [SerializeField] private float maxTilt = 0.16f;
        [SerializeField] private float tiltSpeed = 0.65f;

        [Header("Spawn")]
        [SerializeField] private float spawnDistance = 0.5f;
        [SerializeField] private float spawnHeight = 0.42f;

        private float m_zoom = 1f;
        private float m_pitch;
        private float m_yaw;
        private bool m_isOpen;
        private Material m_viewMaterial;

        private void Awake()
        {
            if (!cameraRig)
            {
                cameraRig = FindFirstObjectByType<OVRCameraRig>();
            }

            if (viewfinderRoot)
            {
                viewfinderRoot.SetActive(false);
            }

            EnsureMaterial();
        }

        public void OpenViewfinder()
        {
            if (!cameraAccess || !cameraAccess.IsPlaying)
            {
                Debug.LogWarning("[PhotoViewfinderController] Passthrough camera is not playing yet.");
                return;
            }

            m_zoom = 1f;
            m_pitch = 0f;
            m_yaw = 0f;
            m_isOpen = true;
            viewfinderRoot.SetActive(true);
            EnsureMaterial();
            ApplyView();
            FollowView();
            MrPointer.Show();
        }

        private void CloseViewfinder()
        {
            m_isOpen = false;
            if (viewfinderRoot)
            {
                viewfinderRoot.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (m_isOpen)
            {
                FollowView();
                ApplyView();
            }
        }

        private void FollowView()
        {
            MainAppPlacement.PlaceInFrontOfUser(viewfinderRoot.transform, cameraRig, spawnDistance);
            MainAppPlacement.NudgeDown(viewfinderRoot.transform, cameraRig, 0.1f);
        }

        private void Update()
        {
            if (!m_isOpen)
            {
                return;
            }

            var left = OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
            var right = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
            var primary = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

            var zoomAxis = Mathf.Abs(left.y) > 0.1f ? left.y : primary.y;
            if (Mathf.Abs(zoomAxis) > 0.1f)
            {
                m_zoom = Mathf.Clamp(m_zoom + zoomAxis * zoomSpeed * Time.deltaTime, minZoom, maxZoom);
            }

            if (right.sqrMagnitude > 0.01f)
            {
                m_yaw = Mathf.Clamp(m_yaw - right.x * tiltSpeed * Time.deltaTime, -maxTilt, maxTilt);
                m_pitch = Mathf.Clamp(m_pitch + right.y * tiltSpeed * Time.deltaTime, -maxTilt, maxTilt);
            }

            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                CapturePhoto();
            }
            else if (OVRInput.GetDown(OVRInput.Button.Two))
            {
                CloseViewfinder();
            }
        }

        private void EnsureMaterial()
        {
            if (m_viewMaterial || !viewfinderImage)
            {
                return;
            }

            m_viewMaterial = MrShaders.CreatePhotoPerspective();
            if (!m_viewMaterial)
            {
                return;
            }

            viewfinderImage.material = m_viewMaterial;
            viewfinderImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            var keystone = viewfinderImage.GetComponent<PhotoKeystoneEffect>();
            if (keystone)
            {
                Destroy(keystone);
            }
        }

        private void ApplyView()
        {
            if (!viewfinderImage || !cameraAccess)
            {
                return;
            }

            var texture = cameraAccess.GetTexture();
            viewfinderImage.texture = texture;
            if (!m_viewMaterial)
            {
                var uvSize = 1f / m_zoom;
                var offset = (1f - uvSize) * 0.5f;
                viewfinderImage.uvRect = new Rect(offset, offset, uvSize, uvSize);
                return;
            }

            m_viewMaterial.mainTexture = texture;
            m_viewMaterial.SetTexture("_MainTex", texture);
            m_viewMaterial.SetColor("_Color", Color.white);
            m_viewMaterial.SetFloat("_Zoom", m_zoom);
            m_viewMaterial.SetFloat("_Pitch", m_pitch);
            m_viewMaterial.SetFloat("_Yaw", m_yaw);
        }

        private void CapturePhoto()
        {
            var source = cameraAccess.GetTexture();
            if (source == null)
            {
                CloseViewfinder();
                return;
            }

            ApplyView();
            var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            if (m_viewMaterial && m_viewMaterial.HasProperty("_Pitch"))
            {
                Graphics.Blit(source, rt, m_viewMaterial);
            }
            else
            {
                var uvSize = 1f / m_zoom;
                var uvOffset = (1f - uvSize) * 0.5f;
                Graphics.Blit(source, rt, new Vector2(uvSize, uvSize), new Vector2(uvOffset, uvOffset));
            }

            var previousActive = RenderTexture.active;
            RenderTexture.active = rt;
            var capture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            capture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            capture.Apply();
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(rt);

            SpawnPhotoItem(capture);
            CloseViewfinder();
        }

        private void SpawnPhotoItem(Texture2D capture)
        {
            var head = cameraRig.centerEyeAnchor;
            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = head.forward;
            }

            var root = GameObject.CreatePrimitive(PrimitiveType.Quad);
            root.name = "PhotoItem";
            root.transform.SetParent(MainAppPlacement.SpawnRoot(), true);
            root.transform.position = head.position + forward * spawnDistance;
            root.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);

            var aspect = (float)capture.width / capture.height;
            root.transform.localScale = new Vector3(spawnHeight * aspect, spawnHeight, 1f);

            var material = MrShaders.CreateUnlitTexture(capture);
            material.mainTextureScale = new Vector2(-1f, 1f);
            material.mainTextureOffset = new Vector2(1f, 0f);
            if (material.HasProperty("_MainTex"))
            {
                material.SetTextureScale("_MainTex", new Vector2(-1f, 1f));
                material.SetTextureOffset("_MainTex", new Vector2(1f, 0f));
            }

            root.GetComponent<MeshRenderer>().material = material;
            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(1f, 1f, 0.04f);

            var item = root.AddComponent<SpawnableItem>();
            item.Initialize(SpawnableItemType.Photo);
            item.ConfigureMiniMenu(miniMenuPrefab);
            item.Select();
        }

        private void OnDestroy()
        {
            if (m_viewMaterial)
            {
                Destroy(m_viewMaterial);
            }
        }
    }

    // Leftover component from the trapezoid preview; destroyed at runtime if present.
    public class PhotoKeystoneEffect : BaseMeshEffect
    {
        public override void ModifyMesh(VertexHelper vh)
        {
        }
    }
}
