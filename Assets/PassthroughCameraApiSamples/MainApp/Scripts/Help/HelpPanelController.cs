// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MainApp.Help
{
    public class HelpPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text bodyText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform content;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button plusButton;
        [SerializeField] private Button minusButton;
        [SerializeField] private Button scrollUpButton;
        [SerializeField] private Button scrollDownButton;
        [SerializeField] private OVRCameraRig cameraRig;
        [SerializeField] private float openDistance = 0.88f;
        [SerializeField] private int fontSize = 36;
        [SerializeField] private int minFontSize = 20;
        [SerializeField] private int maxFontSize = 96;
        [SerializeField] private float viewportHeight = 780f;

        [TextArea(6, 24)]
        [SerializeField]
        private string helpContent =
			"JINSAN - 도움말 (오른쪽 버튼을 이용해 확대/축소)\n오른쪽 조이스틱으로 스크롤 가능\n"+
            "왼쪽 컨트롤러의 메뉴버튼(☰)으로 메인 메뉴를 엽니다. \n\n" +
            "글씨쓰기: 글자 상자를 만듭니다. 선택 후 수정에서 키보드/색/크기를 바꿀 수 있습니다.\n" +
            "사진찍기: 보고있는 방향의 화면이 표시됩니다. 왼손 조이스틱으로 확대/축소, 오른손 조이스틱으로 원근을 살짝 틀 수 있습니다. A로 촬영합니다.\n" +
            "종이스캔: 초록 프레임 안에 종이를 스캔합니다. A로 캡처, B로 취소.\n" +
            "가지고 놀기!: Shader, 3D Cube, HandInteraction. 메뉴를 누르기 전에는 아무 것도 생기지 않습니다.\n" +
			"3D Cube: 손 가까이 대면 달라붙고, 양손을 벌리면 커집니다. 양손 핀치면 떨어지고, 다시 근처에서 핀치하면 붙습니다. 그립(주먹)으로 잡고 던질 수 있습니다.\n" +
            "HandInteraction: 집게(핀치)나 그립으로 잡고, 놓으면 던져져 바닥에 떨어집니다.\n\n" +
            "선택 메뉴: 크기(누른 상태에서 드래그를 통해 크기 조절) / 색 / 수정 / 삭제.\n" +
            "B를 누르면 이 도움말과 선택이 닫힙니다.\n\n" +
            "정보 R&E 실험 (Made by 김건우!)";

        private bool m_initialized;

        private void Awake()
        {
            if (!cameraRig)
            {
                cameraRig = FindFirstObjectByType<OVRCameraRig>();
            }
        }

        private void Start()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (m_initialized || !panelRoot)
            {
                return;
            }

            m_initialized = true;
            Bind(closeButton, Close);
            Bind(plusButton, GrowText);
            Bind(minusButton, ShrinkText);
            Bind(scrollUpButton, () => Scroll(0.18f));
            Bind(scrollDownButton, () => Scroll(-0.18f));
            ApplyFontSize();
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (!button)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void Update()
        {
            if (!panelRoot || !panelRoot.activeSelf)
            {
                return;
            }

            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch)
                || OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LTouch))
            {
                Close();
                return;
            }

            if (!scrollRect)
            {
                return;
            }

            var stick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
            if (Mathf.Abs(stick.y) > 0.12f)
            {
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
                    scrollRect.verticalNormalizedPosition + stick.y * 0.7f * Time.unscaledDeltaTime);
            }
        }

        public void Open()
        {
            EnsureInitialized();
            if (bodyText)
            {
                bodyText.text = helpContent;
            }

            ApplyFontSize();
            if (scrollRect)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }

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

        private void GrowText()
        {
            fontSize = Mathf.Min(maxFontSize, fontSize + 6);
            ApplyFontSize();
        }

        private void ShrinkText()
        {
            fontSize = Mathf.Max(minFontSize, fontSize - 6);
            ApplyFontSize();
        }

        private void Scroll(float delta)
        {
            if (!scrollRect)
            {
                return;
            }

            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + delta);
        }

        private void ApplyFontSize()
        {
            if (!bodyText)
            {
                return;
            }

            bodyText.resizeTextForBestFit = false;
            bodyText.fontSize = fontSize;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            Canvas.ForceUpdateCanvases();
            var height = Mathf.Max(viewportHeight, bodyText.preferredHeight + 24f);
            if (content)
            {
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }

            if (scrollRect)
            {
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
            }
        }
    }
}
