// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.Samples;
using PassthroughCameraSamples.MainApp;
using PassthroughCameraSamples.MainApp.Admin;
using UnityEngine;

namespace PassthroughCameraSamples.StartScene
{
    /// <summary>
    /// StartScene 메인 메뉴. DebugUIBuilder 버튼을 쓰며, 도구 초기화 실패와 무관하게
    /// ☰ 로 열고 닫힙니다.
    /// </summary>
    [MetaCodeSample("PassthroughCameraApiSamples-StartScene")]
    [DefaultExecutionOrder(200)]
    public class StartMenu : MonoBehaviour
    {
        [SerializeField] private float autoCloseDelaySeconds = 1.5f;

        private DebugUIBuilder m_builder;
        private MenuHoverTracker m_hoverTracker;
        private MrAppServices m_app;
        private bool m_isOpen;
        private bool m_ready;
        private float m_pointerLeftAt = -1f;
        private int m_menuPresses;
        private float m_menuPressAt = -999f;

        private IEnumerator Start()
        {
            yield return null;
            yield return null;

            m_builder = DebugUIBuilder.Instance;
            if (!m_builder)
            {
                m_builder = FindFirstObjectByType<DebugUIBuilder>(FindObjectsInactive.Include);
            }

            if (!m_builder)
            {
                Debug.LogError("[StartMenu] DebugUIBuilder not found.");
                yield break;
            }

            AddMenuButton("글씨쓰기", () => UseTool(() => m_app?.TextTool.SpawnTextItem()));
            AddMenuButton("사진찍기", () => UseTool(() => m_app?.PhotoTool.OpenViewfinder()));
            AddMenuButton("종이스캔", () => UseTool(() => m_app?.PaperScanTool.OpenScan()));
            AddMenuButton("그림 그리기", () => UseTool(() => m_app?.DrawTool.SpawnWhiteboard()));
            AddMenuButton("도움말", () => UseTool(() => m_app?.HelpPanel.Open()));
            AddMenuButton("가지고 놀기!", () => UseTool(() => m_app?.PlaySubmenu.Open()));

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
            Debug.Log("[StartMenu] Menu ready. Press left ☰.");

            yield return null;

            try
            {
                m_app = MrAppRuntimeSetup.EnsureCreated();
                m_app?.AdminMenu?.Bind(m_builder, m_app.ImageSpawner);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[StartMenu] Tool setup failed (menu still works): " + ex);
            }
        }

        private void AddMenuButton(string label, UnityEngine.Events.UnityAction onClick)
        {
            _ = m_builder.AddButton(label, () => onClick(), -1, DebugUIBuilder.DEBUG_PANE_CENTER, false);
        }

        private void UseTool(System.Action action)
        {
            CloseMenuKeepLaser();
            if (m_app == null)
            {
                Debug.LogError("[StartMenu] Tools not initialized.");
                return;
            }

            action?.Invoke();
            MrPointer.Show();
        }

        private void Update()
        {
            if (!m_ready || !m_builder)
            {
                return;
            }

            if (WasMenuButtonPressed())
            {
                HandleMenuButton();
                return;
            }

            if (m_isOpen && OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            {
                Close();
                return;
            }

            if (!m_isOpen || !m_hoverTracker)
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

        private void HandleMenuButton()
        {
            var now = Time.unscaledTime;
            if (now - m_menuPressAt > 1.5f)
            {
                m_menuPresses = 0;
            }

            m_menuPresses++;
            m_menuPressAt = now;

            if (m_menuPresses >= 5)
            {
                m_menuPresses = 0;
                OpenAdmin();
                return;
            }

            if (m_app?.AdminMenu != null && m_app.AdminMenu.IsOpen)
            {
                m_app.AdminMenu.Close();
            }

            Toggle();
        }

        private void OpenAdmin()
        {
            Close();
            if (m_app?.AdminMenu == null)
            {
                Debug.LogError("[StartMenu] Admin menu is not ready.");
                return;
            }

            m_app.AdminMenu.Open();
        }

        private static bool WasMenuButtonPressed()
        {
            return OVRInput.GetUp(OVRInput.Button.Start, OVRInput.Controller.LTouch)
                || OVRInput.GetUp(OVRInput.Button.Start);
        }

        private void Toggle()
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

        private void Open()
        {
            m_isOpen = true;
            m_pointerLeftAt = -1f;
            m_builder.Show();
            m_builder.SetOnlyPaneActive(DebugUIBuilder.DEBUG_PANE_CENTER);
            MainAppPlacement.PlaceAlongPointer(m_builder.transform, FindFirstObjectByType<OVRCameraRig>(), 1.15f);
            if (m_hoverTracker)
            {
                m_hoverTracker.ResetTracking();
            }
        }

        private void Close()
        {
            m_isOpen = false;
            m_pointerLeftAt = -1f;
            m_builder.Hide();
        }

        private void CloseMenuKeepLaser()
        {
            m_isOpen = false;
            m_pointerLeftAt = -1f;
            if (m_builder)
            {
                m_builder.Hide();
            }

            MrPointer.Show();
        }
    }
}
