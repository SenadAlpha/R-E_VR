// Copyright (c) Meta Platforms, Inc. and affiliates.

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MainApp.EditorTools
{
    /// <summary>
    /// Small code-only UGUI construction helpers used by <see cref="MainAppSceneBuilder"/>
    /// to build the Text config / Help / Play submenu panels without depending on any
    /// existing prefab's exact internal layout. Every panel gets its own OVRRaycaster
    /// so the shared laser pointer can click its buttons/sliders (wired up at runtime -
    /// see MainMenuBootstrap.WireUpSecondaryPanelRaycasters).
    /// </summary>
    public static class MainAppUguiFactory
    {
        private const float WorldScale = 0.001f;

        public static GameObject CreateWorldPanel(string name, Vector2 sizePixels, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasRenderer), typeof(OVRRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

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
            bg.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.85f);

            return go;
        }

        public static Button CreateButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.15f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            CreateLabelInto(rt, label, TextAlignmentOptions.Center);

            return button;
        }

        public static TextMeshProUGUI CreateLabel(Transform parent, string text, Vector2 anchoredPos, Vector2 size,
            TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            return CreateLabelInto(rt, text, alignment);
        }

        private static TextMeshProUGUI CreateLabelInto(RectTransform rt, string text, TextAlignmentOptions alignment)
        {
            var textGo = new GameObject("Text", typeof(RectTransform));
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.SetParent(rt, false);
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = new Vector2(-8, -4);
            textRt.anchoredPosition = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.enableWordWrapping = true;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 8;
            tmp.fontSizeMax = 32;
            return tmp;
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
            bgRt.anchoredPosition = Vector2.zero;
            bgGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

            var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
            var fillAreaRt = fillAreaGo.GetComponent<RectTransform>();
            fillAreaRt.SetParent(rt, false);
            fillAreaRt.anchorMin = new Vector2(0, 0.25f);
            fillAreaRt.anchorMax = new Vector2(1, 0.75f);
            fillAreaRt.sizeDelta = new Vector2(-20, 0);
            fillAreaRt.anchoredPosition = Vector2.zero;

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
            handleAreaRt.anchoredPosition = Vector2.zero;

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            var handleRt = handleGo.GetComponent<RectTransform>();
            handleRt.SetParent(handleAreaRt, false);
            handleRt.sizeDelta = new Vector2(20, 0);
            handleGo.GetComponent<Image>().color = Color.white;

            slider.targetGraphic = handleGo.GetComponent<Image>();
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = defaultValue;
            return slider;
        }

        public static TMP_InputField CreateInputField(Transform parent, Vector2 anchoredPos, Vector2 size, string defaultText, string placeholder)
        {
            var go = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            var bgImage = go.GetComponent<Image>();
            bgImage.color = new Color(1f, 1f, 1f, 0.1f);
            var inputField = go.GetComponent<TMP_InputField>();
            inputField.targetGraphic = bgImage;

            var textAreaGo = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            var textAreaRt = textAreaGo.GetComponent<RectTransform>();
            textAreaRt.SetParent(rt, false);
            textAreaRt.anchorMin = Vector2.zero;
            textAreaRt.anchorMax = Vector2.one;
            textAreaRt.sizeDelta = new Vector2(-16, -8);
            textAreaRt.anchoredPosition = Vector2.zero;

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
            var placeholderRt = placeholderGo.GetComponent<RectTransform>();
            placeholderRt.SetParent(textAreaRt, false);
            placeholderRt.anchorMin = Vector2.zero;
            placeholderRt.anchorMax = Vector2.one;
            placeholderRt.sizeDelta = Vector2.zero;
            placeholderRt.anchoredPosition = Vector2.zero;
            var placeholderTmp = placeholderGo.AddComponent<TextMeshProUGUI>();
            placeholderTmp.text = placeholder;
            placeholderTmp.fontStyle = FontStyles.Italic;
            placeholderTmp.color = new Color(1f, 1f, 1f, 0.4f);
            placeholderTmp.enableWordWrapping = true;

            var textGo = new GameObject("Text", typeof(RectTransform));
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.SetParent(textAreaRt, false);
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            textRt.anchoredPosition = Vector2.zero;
            var textTmp = textGo.AddComponent<TextMeshProUGUI>();
            textTmp.color = Color.white;
            textTmp.enableWordWrapping = true;

            inputField.textViewport = textAreaRt;
            inputField.textComponent = textTmp;
            inputField.placeholder = placeholderTmp;
            inputField.text = defaultText;
            return inputField;
        }

        /// <summary>Assigns a private [SerializeField] object-reference field via SerializedObject (Editor-only).</summary>
        public static void SetField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[MainAppUguiFactory] Field '{fieldName}' not found on {target.GetType().Name}.");
                return;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }

        /// <summary>Assigns a private [SerializeField] array field (e.g. Transform[]) via SerializedObject.</summary>
        public static void SetObjectArrayField(Object target, string fieldName, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[MainAppUguiFactory] Array field '{fieldName}' not found on {target.GetType().Name}.");
                return;
            }

            prop.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            so.ApplyModifiedProperties();
        }
    }
}
