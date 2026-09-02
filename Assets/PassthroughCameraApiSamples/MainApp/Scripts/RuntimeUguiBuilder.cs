// Copyright (c) Meta Platforms, Inc. and affiliates.

using PassthroughCameraSamples.StartScene;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MainApp
{
    /// <summary>
    /// Runtime UGUI helpers. Uses Unity UI.Text (not TMP) so Malgun Gothic actually
    /// renders Hangul and Latin on device.
    /// </summary>
    public static class RuntimeUguiBuilder
    {
        private const float WorldScale = 0.001f;

        public static GameObject CreateWorldPanel(string name, Vector2 sizePixels, Transform parent = null,
            bool addPhysicsCollider = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasRenderer), typeof(OVRRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;

            var raycaster = go.GetComponent<OVRRaycaster>();
            raycaster.ignoreReversedGraphics = false;
            BindLaser(go);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = sizePixels;
            rt.localScale = Vector3.one * WorldScale;
            if (parent)
            {
                rt.SetParent(parent, false);
            }

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.SetParent(rt, false);
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            bgRt.anchoredPosition = Vector2.zero;
            var bgImage = bg.GetComponent<Image>();
            bgImage.color = new Color(0.12f, 0.12f, 0.14f, 0.95f);
            bgImage.raycastTarget = false;

            if (addPhysicsCollider)
            {
                var box = go.AddComponent<BoxCollider>();
                box.size = new Vector3(sizePixels.x, sizePixels.y, 16f);
                box.center = Vector3.zero;
            }

            return go;
        }

        public static void BindLaser(GameObject canvasGo)
        {
            var raycaster = canvasGo.GetComponent<OVRRaycaster>();
            if (!raycaster)
            {
                return;
            }

            var laser = Object.FindFirstObjectByType<LaserPointer>(FindObjectsInactive.Include);
            if (laser)
            {
                raycaster.pointer = laser.gameObject;
            }
        }

        public static Button CreateButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            var image = go.GetComponent<Image>();
            image.color = new Color(0.87f, 0.87f, 0.87f, 1f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(0.87f, 0.87f, 0.87f, 1f);
            colors.highlightedColor = new Color(0.55f, 0.75f, 1f, 1f);
            colors.pressedColor = new Color(0.2f, 0.59f, 1f, 1f);
            colors.selectedColor = new Color(0.55f, 0.75f, 1f, 1f);
            button.colors = colors;
            CreateLabelInto(rt, label, TextAnchor.MiddleCenter, new Color(0.12f, 0.12f, 0.12f, 1f), 28);
            AddButtonCollider(rt, size);
            image.raycastTarget = false;
            return button;
        }

        public static Button CreateColorSwatch(Transform parent, Color color, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject("Swatch", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            var image = go.GetComponent<Image>();
            image.color = color;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            AddButtonCollider(rt, size);
            image.raycastTarget = false;
            return button;
        }

        private static void AddButtonCollider(RectTransform rt, Vector2 size)
        {
            var box = rt.gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(size.x, size.y, 40f);
            box.center = Vector3.zero;
        }

        public static Text CreateLabel(Transform parent, string text, Vector2 anchoredPos, Vector2 size,
            TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            var color = alignment is TextAnchor.MiddleCenter or TextAnchor.UpperCenter or TextAnchor.LowerCenter
                ? new Color(0.12f, 0.12f, 0.12f, 1f)
                : Color.white;
            if (parent && parent.GetComponentInParent<Canvas>())
            {
                // Labels on dark world panels should stay white except when sitting on a gray button.
                color = Color.white;
            }

            return CreateLabelInto(rt, text, alignment, color, 22);
        }

        public static Text CreateLabelInto(RectTransform rt, string text, TextAnchor alignment, Color color, int fontSize)
        {
            var textGo = new GameObject("Text", typeof(RectTransform));
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.SetParent(rt, false);
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = new Vector2(-8, -4);
            textRt.anchoredPosition = Vector3.zero;

            var ui = textGo.AddComponent<Text>();
            MrFonts.Apply(ui);
            ui.text = text;
            ui.alignment = alignment;
            ui.color = color;
            ui.fontSize = fontSize;
            ui.resizeTextForBestFit = true;
            ui.resizeTextMinSize = 12;
            ui.resizeTextMaxSize = Mathf.Max(fontSize, 36);
            ui.horizontalOverflow = HorizontalWrapMode.Wrap;
            ui.verticalOverflow = VerticalWrapMode.Overflow;
            ui.raycastTarget = false;
            ui.supportRichText = false;
            return ui;
        }

        public static Slider CreateSlider(Transform parent, Vector2 anchoredPos, Vector2 size, float min, float max, float defaultValue)
        {
            var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            var slider = go.GetComponent<Slider>();

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.SetParent(rt, false);
            bgRt.anchorMin = new Vector2(0, 0.25f);
            bgRt.anchorMax = new Vector2(1, 0.75f);
            bgRt.sizeDelta = Vector2.zero;
            bgGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

            var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
            var fillAreaRt = fillAreaGo.GetComponent<RectTransform>();
            fillAreaRt.SetParent(rt, false);
            fillAreaRt.anchorMin = new Vector2(0, 0.25f);
            fillAreaRt.anchorMax = new Vector2(1, 0.75f);
            fillAreaRt.sizeDelta = new Vector2(-20, 0);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.SetParent(fillAreaRt, false);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0, 1);
            fillRt.sizeDelta = new Vector2(10, 0);
            fillGo.GetComponent<Image>().color = new Color(0.25f, 0.6f, 1f, 1f);

            var handleAreaGo = new GameObject("Handle Slide Area", typeof(RectTransform));
            var handleAreaRt = handleAreaGo.GetComponent<RectTransform>();
            handleAreaRt.SetParent(rt, false);
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.sizeDelta = new Vector2(-20, 0);

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            var handleRt = handleGo.GetComponent<RectTransform>();
            handleRt.SetParent(handleAreaRt, false);
            handleRt.sizeDelta = new Vector2(20, 0);
            handleGo.GetComponent<Image>().color = Color.white;

            slider.targetGraphic = handleGo.GetComponent<Image>();
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = defaultValue;
            return slider;
        }

        public static InputField CreateInputField(Transform parent, Vector2 anchoredPos, Vector2 size, string defaultText, string placeholder)
        {
            var go = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(InputField));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            var bgImage = go.GetComponent<Image>();
            bgImage.color = new Color(1f, 1f, 1f, 0.18f);
            var inputField = go.GetComponent<InputField>();
            inputField.targetGraphic = bgImage;

            var textAreaGo = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            var textAreaRt = textAreaGo.GetComponent<RectTransform>();
            textAreaRt.SetParent(rt, false);
            textAreaRt.anchorMin = Vector2.zero;
            textAreaRt.anchorMax = Vector2.one;
            textAreaRt.offsetMin = new Vector2(8, 4);
            textAreaRt.offsetMax = new Vector2(-8, -4);

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
            var placeholderRt = placeholderGo.GetComponent<RectTransform>();
            placeholderRt.SetParent(textAreaRt, false);
            placeholderRt.anchorMin = Vector2.zero;
            placeholderRt.anchorMax = Vector2.one;
            placeholderRt.sizeDelta = Vector2.zero;
            var placeholderText = placeholderGo.AddComponent<Text>();
            MrFonts.Apply(placeholderText);
            placeholderText.text = placeholder;
            placeholderText.fontStyle = FontStyle.Italic;
            placeholderText.color = new Color(1f, 1f, 1f, 0.45f);
            placeholderText.fontSize = 22;
            placeholderText.alignment = TextAnchor.MiddleLeft;
            placeholderText.raycastTarget = false;

            var textGo = new GameObject("Text", typeof(RectTransform));
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.SetParent(textAreaRt, false);
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            MrFonts.Apply(text);
            text.color = Color.white;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            inputField.textComponent = text;
            inputField.placeholder = placeholderText;
            inputField.text = defaultText;
            inputField.lineType = InputField.LineType.MultiLineSubmit;
            inputField.shouldHideMobileInput = false;
            return inputField;
        }
    }
}
