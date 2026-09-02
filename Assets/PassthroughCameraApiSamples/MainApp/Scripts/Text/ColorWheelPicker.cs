// Copyright (c) Meta Platforms, Inc. and affiliates.

using PassthroughCameraSamples.MainApp;
using PassthroughCameraSamples.MainApp.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MainApp.TextTools
{
    /// <summary>
    /// Photoshop-style HSV disk: laser/trigger acts as an eyedropper on the wheel.
    /// Also exposes background alpha and box-size controls for WorldTextNote.
    /// </summary>
    public class ColorWheelPicker : MonoBehaviour
    {
        private GameObject m_panel;
        private RawImage m_wheel;
        private RawImage m_valueBar;
        private Image m_preview;
        private Text m_modeLabel;
        private Text m_alphaLabel;
        private Text m_boxLabel;
        private Button m_textModeButton;
        private Button m_bgModeButton;
        private Texture2D m_wheelTex;
        private Texture2D m_valueTex;
        private TextMesh m_text;
        private WorldTextNote m_note;
        private bool m_editBackground;
        private bool m_sampleArmed;
        private float m_hue = 0.08f;
        private float m_sat = 0.15f;
        private float m_val = 1f;
        private OVRCameraRig m_rig;

        public bool IsOpen => m_panel && m_panel.activeSelf;

        public void Open(TextMesh text, WorldTextNote note)
        {
            m_text = text;
            m_note = note;
            m_editBackground = false;
            m_sampleArmed = false;
            EnsureUi();
            SyncFromTarget();
            m_panel.SetActive(true);
            Follow();
            RuntimeUguiBuilder.BindLaser(m_panel);
            MrPointer.Show();
        }

        public void Close()
        {
            if (m_panel)
            {
                m_panel.SetActive(false);
            }

            m_text = null;
            m_note = null;
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            Follow();
            var held = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch)
                       || OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
            if (!held)
            {
                m_sampleArmed = true;
                return;
            }

            if (m_sampleArmed)
            {
                SampleUnderLaser();
            }
        }

        private void Follow()
        {
            if (!m_rig)
            {
                m_rig = FindFirstObjectByType<OVRCameraRig>();
            }

            MainAppPlacement.PlaceAlongHead(m_panel.transform, m_rig, 0.95f);
            MainAppPlacement.NudgeDown(m_panel.transform, m_rig, 0.08f);
            var head = m_rig && m_rig.centerEyeAnchor ? m_rig.centerEyeAnchor : Camera.main?.transform;
            if (head)
            {
                m_panel.transform.position += head.right * 0.22f;
            }
        }

        private void SampleUnderLaser()
        {
            if (!m_rig)
            {
                return;
            }

            var ray = ControllerAim.GetRay(OVRInput.Controller.RTouch, m_rig);
            if (!Physics.Raycast(ray, out var hit, 4f))
            {
                return;
            }

            if (HitImage(hit, m_wheel, out var wheelUv))
            {
                var nx = (wheelUv.x - 0.5f) * 2f;
                var ny = (wheelUv.y - 0.5f) * 2f;
                var radius = Mathf.Sqrt(nx * nx + ny * ny);
                if (radius > 1.02f)
                {
                    return;
                }

                m_hue = (Mathf.Atan2(ny, nx) / (Mathf.PI * 2f) + 1f) % 1f;
                m_sat = Mathf.Clamp01(radius);
                ApplyColor();
                return;
            }

            if (HitImage(hit, m_valueBar, out var valueUv))
            {
                m_val = Mathf.Clamp01(valueUv.y);
                ApplyColor();
            }
        }

        private static bool HitImage(RaycastHit hit, RawImage image, out Vector2 uv)
        {
            uv = default;
            if (!image)
            {
                return false;
            }

            var rt = image.rectTransform;
            if (hit.collider.transform != image.transform && hit.collider.transform != rt)
            {
                return false;
            }

            var local = rt.InverseTransformPoint(hit.point);
            var size = rt.rect.size;
            uv = new Vector2(local.x / size.x + 0.5f, local.y / size.y + 0.5f);
            return uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
        }

        private void SyncFromTarget()
        {
            var color = m_editBackground && m_note
                ? m_note.BackgroundColor
                : (m_text ? m_text.color : Color.white);
            Color.RGBToHSV(color, out m_hue, out m_sat, out m_val);
            if (m_val < 0.05f)
            {
                m_val = 0.05f;
            }

            RefreshLabels();
            ApplyColor(false);
        }

        private void ApplyColor(bool write = true)
        {
            var rgb = Color.HSVToRGB(m_hue, m_sat, m_val);
            if (m_preview)
            {
                m_preview.color = new Color(rgb.r, rgb.g, rgb.b, 1f);
            }

            if (!write)
            {
                RefreshLabels();
                return;
            }

            if (m_editBackground && m_note)
            {
                var alpha = m_note.BackgroundColor.a;
                m_note.SetBackgroundColor(new Color(rgb.r, rgb.g, rgb.b, alpha));
            }
            else if (m_text)
            {
                m_text.color = rgb;
            }

            RefreshLabels();
        }

        private void RefreshLabels()
        {
            if (m_modeLabel)
            {
                m_modeLabel.text = m_editBackground ? "배경 색 — 원을 눌러 고르세요" : "글자 색 — 원을 눌러 고르세요";
            }

            if (m_alphaLabel && m_note)
            {
                m_alphaLabel.text = $"투명 {Mathf.RoundToInt(m_note.BackgroundColor.a * 100f)}%";
            }

            if (m_boxLabel && m_note)
            {
                m_boxLabel.text = $"박스 {Mathf.RoundToInt(m_note.BoxScale * 100f)}%";
            }

            HighlightModeButtons();
        }

        private void HighlightModeButtons()
        {
            SetButtonTint(m_textModeButton, !m_editBackground);
            SetButtonTint(m_bgModeButton, m_editBackground);
        }

        private static void SetButtonTint(Button button, bool selected)
        {
            if (!button)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image)
            {
                image.color = selected
                    ? new Color(0.45f, 0.75f, 1f, 1f)
                    : new Color(0.87f, 0.87f, 0.87f, 1f);
            }
        }

        private void EnsureUi()
        {
            if (m_panel)
            {
                return;
            }

            m_wheelTex = BuildWheelTexture(192);
            m_valueTex = BuildValueTexture(24, 192);

            m_panel = RuntimeUguiBuilder.CreateWorldPanel("ColorWheelPanel", new Vector2(560, 520));
            m_panel.transform.SetParent(transform, false);
            var rt = m_panel.GetComponent<RectTransform>();

            m_modeLabel = RuntimeUguiBuilder.CreateLabel(rt, "글자 색 — 원을 눌러 고르세요",
                new Vector2(0f, 230f), new Vector2(520, 36), TextAnchor.MiddleCenter);

            m_wheel = CreateRaw(rt, m_wheelTex, new Vector2(-90f, 70f), new Vector2(220f, 220f));
            m_valueBar = CreateRaw(rt, m_valueTex, new Vector2(50f, 70f), new Vector2(36f, 220f));

            m_preview = CreateSwatch(rt, new Vector2(150f, 150f), new Vector2(88f, 88f));

            m_textModeButton = RuntimeUguiBuilder.CreateButton(rt, "글자", new Vector2(150f, 70f), new Vector2(120, 48));
            m_bgModeButton = RuntimeUguiBuilder.CreateButton(rt, "배경", new Vector2(150f, 12f), new Vector2(120, 48));
            m_textModeButton.onClick.AddListener(() =>
            {
                m_editBackground = false;
                SyncFromTarget();
            });
            m_bgModeButton.onClick.AddListener(() =>
            {
                m_editBackground = true;
                SyncFromTarget();
            });

            m_alphaLabel = RuntimeUguiBuilder.CreateLabel(rt, "투명 72%", new Vector2(-80f, -80f),
                new Vector2(200, 32), TextAnchor.MiddleCenter);
            var alphaMinus = RuntimeUguiBuilder.CreateButton(rt, "-", new Vector2(70f, -80f), new Vector2(64, 48));
            var alphaPlus = RuntimeUguiBuilder.CreateButton(rt, "+", new Vector2(150f, -80f), new Vector2(64, 48));
            alphaMinus.onClick.AddListener(() => ShiftAlpha(-0.1f));
            alphaPlus.onClick.AddListener(() => ShiftAlpha(0.1f));

            m_boxLabel = RuntimeUguiBuilder.CreateLabel(rt, "박스 100%", new Vector2(-80f, -145f),
                new Vector2(200, 32), TextAnchor.MiddleCenter);
            var boxMinus = RuntimeUguiBuilder.CreateButton(rt, "-", new Vector2(70f, -145f), new Vector2(64, 48));
            var boxPlus = RuntimeUguiBuilder.CreateButton(rt, "+", new Vector2(150f, -145f), new Vector2(64, 48));
            boxMinus.onClick.AddListener(() =>
            {
                m_note?.AdjustBoxScale(1f / 1.15f);
                RefreshLabels();
            });
            boxPlus.onClick.AddListener(() =>
            {
                m_note?.AdjustBoxScale(1.15f);
                RefreshLabels();
            });

            var close = RuntimeUguiBuilder.CreateButton(rt, "완료", new Vector2(0f, -220f), new Vector2(200, 56));
            close.onClick.AddListener(Close);

            m_panel.SetActive(false);
        }

        private void ShiftAlpha(float delta)
        {
            if (!m_note)
            {
                return;
            }

            m_note.SetBackgroundAlpha(m_note.BackgroundColor.a + delta);
            RefreshLabels();
        }

        private static RawImage CreateRaw(RectTransform parent, Texture texture, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Raw", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var raw = go.GetComponent<RawImage>();
            raw.texture = texture;
            raw.raycastTarget = false;
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(size.x, size.y, 40f);
            box.center = Vector3.zero;
            return raw;
        }

        private static Image CreateSwatch(RectTransform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Preview", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = Color.white;
            return image;
        }

        private static Texture2D BuildWheelTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var nx = (x - center) / center;
                    var ny = (y - center) / center;
                    var radius = Mathf.Sqrt(nx * nx + ny * ny);
                    if (radius > 1f)
                    {
                        pixels[y * size + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    var hue = (Mathf.Atan2(ny, nx) / (Mathf.PI * 2f) + 1f) % 1f;
                    var color = Color.HSVToRGB(hue, radius, 1f);
                    pixels[y * size + x] = color;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        private static Texture2D BuildValueTexture(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                var v = y / (float)(height - 1);
                var color = (Color32)Color.HSVToRGB(0f, 0f, v);
                for (var x = 0; x < width; x++)
                {
                    pixels[y * width + x] = color;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }
    }
}
