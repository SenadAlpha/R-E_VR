// Copyright (c) Meta Platforms, Inc. and affiliates.

using PassthroughCameraSamples.StartScene;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Admin
{
    /// <summary>
    /// Same DebugUIBuilder look as the main menu. Right pane = IMAGE / 닫기.
    /// Left pane = hardcoded PNG buttons.
    /// </summary>
    public class AdminMenuController : MonoBehaviour
    {
        [SerializeField] private AdminImageSpawner imageSpawner;
        [SerializeField] private float autoCloseDelaySeconds = 1.5f;

        private DebugUIBuilder m_builder;
        private MenuHoverTracker m_hoverTracker;
        private bool m_isOpen;
        private bool m_ready;
        private float m_pointerLeftAt = -1f;

        public bool IsOpen => m_isOpen;

        public void Bind(DebugUIBuilder builder, AdminImageSpawner spawner)
        {
            m_builder = builder;
            imageSpawner = spawner;
            if (!m_builder)
            {
                return;
            }

            _ = m_builder.AddButton("IMAGE", ShowImageList, -1, DebugUIBuilder.DEBUG_PANE_RIGHT, false);
            _ = m_builder.AddButton("닫기", Close, -1, DebugUIBuilder.DEBUG_PANE_RIGHT, false);

            foreach (var entry in AdminImageCatalog.Images)
            {
                var path = entry.ResourcePath;
                _ = m_builder.AddButton(entry.Label, () => Spawn(path), -1, DebugUIBuilder.DEBUG_PANE_LEFT, false);
            }

            _ = m_builder.AddButton("뒤로", ShowRoot, -1, DebugUIBuilder.DEBUG_PANE_LEFT, false);

            var contentPanel = m_builder.transform.Find("Content");
            if (contentPanel)
            {
                m_hoverTracker = contentPanel.gameObject.GetComponent<MenuHoverTracker>();
                if (!m_hoverTracker)
                {
                    m_hoverTracker = contentPanel.gameObject.AddComponent<MenuHoverTracker>();
                }
            }

            m_ready = true;
        }

        private void Update()
        {
            if (!m_ready || !m_isOpen || !m_builder)
            {
                return;
            }

            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            {
                Close();
                return;
            }

            if (!m_hoverTracker)
            {
                return;
            }

            if (!m_hoverTracker.HasBeenInside || m_hoverTracker.IsPointerInside)
            {
                m_pointerLeftAt = -1f;
                return;
            }

            if (m_pointerLeftAt < 0f)
            {
                m_pointerLeftAt = Time.unscaledTime;
            }
            else if (Time.unscaledTime - m_pointerLeftAt >= autoCloseDelaySeconds)
            {
                Close();
            }
        }

        public void Open()
        {
            if (!m_builder)
            {
                return;
            }

            m_isOpen = true;
            m_pointerLeftAt = -1f;
            m_builder.Show();
            MainAppPlacement.PlaceAlongPointer(m_builder.transform, FindFirstObjectByType<OVRCameraRig>(), 1.15f);
            m_builder.SetOnlyPaneActive(DebugUIBuilder.DEBUG_PANE_RIGHT);
            if (m_hoverTracker)
            {
                m_hoverTracker.ResetTracking();
            }

            MrPointer.Show();
        }

        public void Close()
        {
            m_isOpen = false;
            m_pointerLeftAt = -1f;
            if (m_builder)
            {
                m_builder.Hide();
            }
        }

        private void ShowRoot()
        {
            if (!m_isOpen || !m_builder)
            {
                return;
            }

            m_builder.SetOnlyPaneActive(DebugUIBuilder.DEBUG_PANE_RIGHT);
        }

        private void ShowImageList()
        {
            if (!m_builder)
            {
                return;
            }

            m_builder.SetOnlyPaneActive(DebugUIBuilder.DEBUG_PANE_LEFT);
        }

        private void Spawn(string resourcePath)
        {
            Close();
            if (!imageSpawner)
            {
                imageSpawner = FindAnyObjectByType<AdminImageSpawner>();
            }

            imageSpawner?.Spawn(resourcePath);
            MrPointer.Show();
        }
    }
}
