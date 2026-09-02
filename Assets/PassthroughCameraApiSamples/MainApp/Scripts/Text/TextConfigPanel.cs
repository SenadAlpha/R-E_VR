// Copyright (c) Meta Platforms, Inc. and affiliates.

using PassthroughCameraSamples.MainApp;
using PassthroughCameraSamples.MainApp.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MainApp.TextTools
{
    /// <summary>
    /// Quest system keyboard only — same Open() path as JinsanRNEProject MRAppManager.
    /// A world-space preview panel shows typed text; ScreenSpaceOverlay canvases are
    /// avoided because they hitch in VR and often swallow the system IME.
    /// </summary>
    public class TextConfigPanel : MonoBehaviour
    {
        public static TextConfigPanel Instance { get; private set; }

        private TouchScreenKeyboard m_keyboard;
        private GameObject m_panel;
        private InputField m_inputField;
        private ColorWheelPicker m_colorPicker;
        private TextMesh m_target;
        private WorldTextNote m_note;
        private bool m_sawKeyboardVisible;
        private bool m_retriedOpen;
        private OVRCameraRig m_rig;

        public bool IsOpen =>
            m_keyboard != null
            || (m_panel && m_panel.activeSelf)
            || (m_colorPicker && m_colorPicker.IsOpen);

        private void Awake()
        {
            Instance = this;
            m_rig = FindFirstObjectByType<OVRCameraRig>();
            MrFonts.UiFont()?.RequestCharactersInTexture("새 메모 가나다라마바사아자차카타파하 ABC 123", 64);
            EnsureEditorUi();
            if (!m_colorPicker)
            {
                m_colorPicker = gameObject.AddComponent<ColorWheelPicker>();
            }
        }

        public void Open(TextMesh target, WorldTextNote note)
        {
            if (!target)
            {
                return;
            }

            m_target = target;
            m_note = note;
            m_sawKeyboardVisible = false;
            m_retriedOpen = false;
            MrPointer.Show();
            EnsureEditorUi();
            ShowEditorUi(target.text ?? string.Empty);
            OpenSystemKeyboard(target.text ?? string.Empty);
        }

        public void CycleColor()
        {
            OpenColorPicker(m_target, m_note);
        }

        public void CycleColor(TextMesh target)
        {
            OpenColorPicker(target, target ? target.GetComponentInParent<WorldTextNote>() : m_note);
        }

        public void OpenColorPicker(TextMesh target, WorldTextNote note)
        {
            if (!target && !note)
            {
                return;
            }

            m_target = target ? target : m_target;
            m_note = note ? note : m_note;
            if (!m_colorPicker)
            {
                m_colorPicker = gameObject.AddComponent<ColorWheelPicker>();
            }

            m_colorPicker.Open(m_target, m_note);
            MrPointer.Show();
        }

        public void Close()
        {
            if (m_keyboard != null)
            {
                m_keyboard.active = false;
            }

            m_keyboard = null;
            if (m_inputField)
            {
                m_inputField.DeactivateInputField();
            }

            if (m_panel)
            {
                m_panel.SetActive(false);
            }

            m_colorPicker?.Close();
            m_target = null;
            m_note = null;
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            FollowEditorUi();
            PollKeyboard();

            if (m_panel && m_panel.activeSelf && m_inputField && m_target
                && m_target.text != m_inputField.text)
            {
                SetText(m_inputField.text);
            }
        }

        private void PollKeyboard()
        {
            if (m_keyboard == null)
            {
                return;
            }

            if (m_keyboard.status == TouchScreenKeyboard.Status.Visible)
            {
                m_sawKeyboardVisible = true;
            }

            ApplyLiveText();

            if (m_keyboard.status == TouchScreenKeyboard.Status.Canceled)
            {
                if (m_sawKeyboardVisible)
                {
                    Close();
                }
                else if (!m_retriedOpen)
                {
                    m_retriedOpen = true;
                    OpenSystemKeyboard(m_target ? m_target.text : string.Empty);
                }

                return;
            }

            if (m_sawKeyboardVisible && m_keyboard.status == TouchScreenKeyboard.Status.Done)
            {
                Close();
            }
        }

        private void ApplyLiveText()
        {
            if (!m_target || m_keyboard == null)
            {
                return;
            }

            if (m_target.text != m_keyboard.text)
            {
                SetText(m_keyboard.text);
                if (m_inputField)
                {
                    m_inputField.SetTextWithoutNotify(m_keyboard.text);
                }
            }
        }

        private void SetText(string value)
        {
            m_target.text = value ?? string.Empty;
            MrFonts.Apply(m_target);
            m_note?.RefreshLayout();
        }

        private void OpenSystemKeyboard(string seed)
        {
            TouchScreenKeyboard.hideInput = false;
            m_keyboard = TouchScreenKeyboard.Open(
                seed ?? string.Empty,
                TouchScreenKeyboardType.Default,
                false,
                true,
                false,
                false,
                "메모 입력");
        }

        private void ShowEditorUi(string seed)
        {
            if (!m_panel)
            {
                return;
            }

            m_panel.SetActive(true);
            FollowEditorUi();
            RuntimeUguiBuilder.BindLaser(m_panel);
            if (m_inputField)
            {
                m_inputField.SetTextWithoutNotify(seed);
            }
        }

        private void FollowEditorUi()
        {
            if (!m_panel || !m_panel.activeInHierarchy)
            {
                return;
            }

            if (!m_rig)
            {
                m_rig = FindFirstObjectByType<OVRCameraRig>();
            }

            MainAppPlacement.PlaceAlongHead(m_panel.transform, m_rig, 0.55f);
            MainAppPlacement.NudgeDown(m_panel.transform, m_rig, 0.1f);
        }

        private void EnsureEditorUi()
        {
            if (m_panel)
            {
                return;
            }

            m_panel = RuntimeUguiBuilder.CreateWorldPanel("TextEditPanel", new Vector2(560, 300));
            m_panel.transform.SetParent(transform, false);
            var rt = m_panel.GetComponent<RectTransform>();

            RuntimeUguiBuilder.CreateLabel(rt, "시스템 키보드로 입력", new Vector2(0f, 118f),
                new Vector2(520, 40), TextAnchor.MiddleCenter);

            m_inputField = RuntimeUguiBuilder.CreateInputField(rt, new Vector2(0f, 18f),
                new Vector2(520, 150), string.Empty, "여기에 글자가 보입니다");
            m_inputField.lineType = InputField.LineType.MultiLineNewline;
            m_inputField.shouldHideMobileInput = false;
            var fieldBox = m_inputField.gameObject.GetComponent<BoxCollider>();
            if (!fieldBox)
            {
                fieldBox = m_inputField.gameObject.AddComponent<BoxCollider>();
            }

            fieldBox.size = new Vector3(520f, 150f, 40f);
            var retry = m_inputField.gameObject.GetComponent<PhysicsClickable>()
                        ?? m_inputField.gameObject.AddComponent<PhysicsClickable>();
            retry.OnClick.AddListener(() =>
            {
                if (!m_target)
                {
                    return;
                }

                OpenSystemKeyboard(m_inputField ? m_inputField.text : m_target.text);
            });

            var done = RuntimeUguiBuilder.CreateButton(rt, "완료", new Vector2(-90f, -112f), new Vector2(200, 56));
            done.onClick.AddListener(Close);

            var retryBtn = RuntimeUguiBuilder.CreateButton(rt, "키보드", new Vector2(130f, -112f), new Vector2(200, 56));
            retryBtn.onClick.AddListener(() =>
            {
                if (m_target)
                {
                    OpenSystemKeyboard(m_target.text ?? string.Empty);
                }
            });

            m_panel.SetActive(false);
        }
    }
}
