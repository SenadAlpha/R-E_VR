// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Events;

namespace PassthroughCameraSamples.MainApp
{
    /// <summary>
    /// Guardian off while Insight Passthrough is actually drawing.
    /// <see cref="OVRManager.shouldBoundaryVisibilityBeSuppressed"/> is ignored unless
    /// Quest Features → Boundary Visibility Support is Supported/Required AND a
    /// passthrough layer is active. This MR APK also declares BOUNDARYLESS_APP so
    /// standalone headset installs can hide Guardian without that API.
    /// Those two OS paths are mutually exclusive; we enable both so the device
    /// can pick the one its OS version supports. Link ignores the manifest.
    /// </summary>
    public class GuardianBoundarySuppressor : MonoBehaviour
    {
        private const string BoundaryVisibilityPermission = "com.oculus.permission.BOUNDARY_VISIBILITY";

        private OVRPassthroughLayer m_passthroughLayer;
        private UnityAction<OVRPassthroughLayer> m_onLayerResumed;
        private bool m_permissionAsked;
        private bool m_loggedOk;
        private bool m_loggedBlocked;

        private void Start()
        {
            RequestBoundaryPermissionIfNeeded();
            BindPassthroughLayer();
            Apply(PassthroughIsOn());
        }

        private void OnDestroy()
        {
            if (m_passthroughLayer && m_onLayerResumed != null)
            {
                m_passthroughLayer.passthroughLayerResumed.RemoveListener(m_onLayerResumed);
            }
        }

        private void LateUpdate()
        {
            if (!m_passthroughLayer)
            {
                BindPassthroughLayer();
            }

            Apply(PassthroughIsOn());
        }

        private void BindPassthroughLayer()
        {
            var layer = FindAnyObjectByType<OVRPassthroughLayer>();
            if (!layer || layer == m_passthroughLayer)
            {
                return;
            }

            if (m_passthroughLayer && m_onLayerResumed != null)
            {
                m_passthroughLayer.passthroughLayerResumed.RemoveListener(m_onLayerResumed);
            }

            m_passthroughLayer = layer;
            m_onLayerResumed = _ => Apply(PassthroughIsOn());
            m_passthroughLayer.passthroughLayerResumed.AddListener(m_onLayerResumed);
        }

        private bool PassthroughIsOn()
        {
            var manager = OVRManager.instance;
            var layerOn = m_passthroughLayer && m_passthroughLayer.isActiveAndEnabled && !m_passthroughLayer.hidden;
            var insightOn = manager && manager.isInsightPassthroughEnabled;
            return layerOn && insightOn;
        }

        private void Apply(bool passthroughOn)
        {
            var manager = OVRManager.instance;
            if (!manager)
            {
                return;
            }

            if (passthroughOn)
            {
                if (!manager.isInsightPassthroughEnabled)
                {
                    manager.isInsightPassthroughEnabled = true;
                }

                if (m_passthroughLayer && !m_passthroughLayer.enabled)
                {
                    m_passthroughLayer.enabled = true;
                }

                if (m_passthroughLayer)
                {
                    m_passthroughLayer.hidden = false;
                }
            }

            manager.shouldBoundaryVisibilityBeSuppressed = passthroughOn;

            if (!passthroughOn || m_loggedOk || m_loggedBlocked)
            {
                return;
            }

            if (manager.isBoundaryVisibilitySuppressed)
            {
                m_loggedOk = true;
                Debug.Log("[GuardianBoundarySuppressor] System reports Guardian suppressed.");
            }
            else if (Time.unscaledTime > 8f)
            {
                m_loggedBlocked = true;
                Debug.LogWarning("[GuardianBoundarySuppressor] Passthrough is on but Guardian is still visible. " +
                                 "Needs a standalone APK (not Link), Boundary Visibility = Required, and an active Underlay passthrough layer.");
            }
        }

        private void RequestBoundaryPermissionIfNeeded()
        {
            if (m_permissionAsked)
            {
                return;
            }

            m_permissionAsked = true;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(BoundaryVisibilityPermission))
            {
                UnityEngine.Android.Permission.RequestUserPermission(BoundaryVisibilityPermission);
            }
#endif
        }
    }
}
