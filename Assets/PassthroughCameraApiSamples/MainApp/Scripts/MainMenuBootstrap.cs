// Copyright (c) Meta Platforms, Inc. and affiliates.

using PassthroughCameraSamples.MainApp.Draw;
using PassthroughCameraSamples.MainApp.Help;
using PassthroughCameraSamples.MainApp.Paper;
using PassthroughCameraSamples.MainApp.Photo;
using PassthroughCameraSamples.MainApp.Play;
using PassthroughCameraSamples.MainApp.TextTools;
using PassthroughCameraSamples.StartScene;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp
{
    /// <summary>
    /// Registers the 6 main-menu buttons on the scene's <see cref="DebugUIBuilder"/> -
    /// the same world-space panel + laser-pointer system every other sample scene's
    /// debug UI already uses - instead of a bespoke Canvas. Drives open/close on the
    /// Left Controller Menu button (☰) and the 1.5s hover-exit auto-close.
    /// </summary>
    public class MainMenuBootstrap : MonoBehaviour
    {
        [SerializeField] private TextToolSpawner textTool;
        [SerializeField] private PhotoViewfinderController photoTool;
        [SerializeField] private PaperScanController paperScanTool;
        [SerializeField] private DrawToolSpawner drawTool;
        [SerializeField] private HelpPanelController helpPanel;
        [SerializeField] private PlaySubmenuController playSubmenu;
        [SerializeField] private float autoCloseDelaySeconds = 1.5f;

        private DebugUIBuilder m_builder;
        private MenuHoverTracker m_hoverTracker;
        private bool m_isOpen;
        private float m_pointerLeftAt = -1f;

        private void Start()
        {
            m_builder = DebugUIBuilder.Instance;
            if (!m_builder)
            {
                Debug.LogError("[MainMenuBootstrap] No DebugUIBuilder found in scene - is CanvasWithDebug present?");
                return;
            }

            _ = m_builder.AddButton("텍스트", () => textTool.SpawnTextItem(), -1, DebugUIBuilder.DEBUG_PANE_CENTER, true);
            _ = m_builder.AddButton("사진", () => photoTool.OpenViewfinder(), -1, DebugUIBuilder.DEBUG_PANE_CENTER, true);
            _ = m_builder.AddButton("종이 스캔", () => paperScanTool.OpenScan(), -1, DebugUIBuilder.DEBUG_PANE_CENTER, true);
            _ = m_builder.AddButton("그리기", () => drawTool.SpawnWhiteboard(), -1, DebugUIBuilder.DEBUG_PANE_CENTER, true);
            _ = m_builder.AddButton("도움말", () => helpPanel.Open(), -1, DebugUIBuilder.DEBUG_PANE_CENTER, true);
            _ = m_builder.AddButton("갖고놀기", () => playSubmenu.Open(), -1, DebugUIBuilder.DEBUG_PANE_CENTER, true);

            var contentPanel = m_builder.transform.Find("Content");
            if (contentPanel)
            {
                m_hoverTracker = contentPanel.gameObject.AddComponent<MenuHoverTracker>();
            }

            WireUpSecondaryPanelRaycasters();
        }

        /// <summary>
        /// The Text config / Help / Play submenu panels each have their own
        /// OVRRaycaster (so the laser pointer can click their buttons/sliders too),
        /// but the shared LaserPointer GameObject only exists once DebugUIBuilder's
        /// Awake() has instantiated UIHelpers - which has already happened by the
        /// time this Start() runs. Wire them all here, once.
        /// </summary>
        private void WireUpSecondaryPanelRaycasters()
        {
            var laserPointer = FindFirstObjectByType<LaserPointer>();
            if (!laserPointer)
            {
                return;
            }

            foreach (var raycaster in FindObjectsByType<OVRRaycaster>(FindObjectsSortMode.None))
            {
                if (!raycaster.pointer)
                {
                    raycaster.pointer = laserPointer.gameObject;
                }
            }
        }

        private void Update()
        {
            if (!m_builder)
            {
                return;
            }

            if (OVRInput.GetDown(OVRInput.Button.Start))
            {
                Toggle();
                return;
            }

            if (!m_isOpen || !m_hoverTracker)
            {
                return;
            }

            if (m_hoverTracker.IsPointerInside)
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

        public void Toggle()
        {
            if (m_isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            m_isOpen = true;
            m_pointerLeftAt = -1f;
            m_builder.Show();
        }

        public void Close()
        {
            m_isOpen = false;
            m_pointerLeftAt = -1f;
            m_builder.Hide();
        }
    }
}
