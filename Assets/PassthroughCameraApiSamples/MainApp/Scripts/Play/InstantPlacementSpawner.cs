// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR;
using PassthroughCameraSamples.MainApp.Interaction;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Play
{
    /// <summary>
    /// Simplified local instant-placement demo for the 갖고놀기 submenu. Uses
    /// EnvironmentRaycastManager (Scene API) directly rather than reusing MRMotif's
    /// SurfacePlacementMotif, since that script has some Fusion-specific coupling
    /// not needed here. Points the right controller at a surface, previews the
    /// object snapped to it, 'A' confirms (turns it into a SpawnableItem Cube),
    /// 'B' cancels.
    /// </summary>
    public class InstantPlacementSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject placementPrefab;
        [SerializeField] private EnvironmentRaycastManager raycastManager;
        [SerializeField] private ItemMiniMenu miniMenuPrefab;
        [SerializeField] private float maxRayDistance = 5f;

        private OVRCameraRig m_cameraRig;
        private bool m_isPlacing;
        private GameObject m_previewInstance;

        private void Awake()
        {
            if (!raycastManager)
            {
                raycastManager = FindAnyObjectByType<EnvironmentRaycastManager>();
            }

            m_cameraRig = FindFirstObjectByType<OVRCameraRig>();
        }

        public void BeginPlacement()
        {
            if (!raycastManager)
            {
                Debug.LogError("[InstantPlacementSpawner] No EnvironmentRaycastManager found in scene.");
                return;
            }

            m_isPlacing = true;

            if (placementPrefab && !m_previewInstance)
            {
                m_previewInstance = Instantiate(placementPrefab);
                m_previewInstance.SetActive(true);
            }
        }

        private void Update()
        {
            if (!m_isPlacing)
            {
                return;
            }

            var controller = OVRInput.Controller.RTouch;
            var origin = ControllerAim.GetPosition(controller, m_cameraRig);
            var rotation = ControllerAim.GetRotation(controller, m_cameraRig);
            var direction = rotation * Vector3.forward;

            if (raycastManager.Raycast(new Ray(origin, direction), out var hit, maxRayDistance) && m_previewInstance)
            {
                m_previewInstance.transform.position = hit.point;
                m_previewInstance.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(direction, hit.normal).normalized, hit.normal);
            }

            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                ConfirmPlacement();
            }
            else if (OVRInput.GetDown(OVRInput.Button.Two))
            {
                CancelPlacement();
            }
        }

        private void ConfirmPlacement()
        {
            m_isPlacing = false;

            if (!m_previewInstance)
            {
                return;
            }

            var item = m_previewInstance.AddComponent<SpawnableItem>();
            item.Initialize(SpawnableItemType.Cube);
            item.ConfigureMiniMenu(miniMenuPrefab);
            m_previewInstance = null;
        }

        private void CancelPlacement()
        {
            m_isPlacing = false;

            if (m_previewInstance)
            {
                Destroy(m_previewInstance);
            }

            m_previewInstance = null;
        }
    }
}
