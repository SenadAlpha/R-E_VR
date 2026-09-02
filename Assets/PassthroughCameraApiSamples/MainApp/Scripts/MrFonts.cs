// Copyright (c) Meta Platforms, Inc. and affiliates.

using TMPro;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp
{
    public static class MrFonts
    {
        private static Font s_uiFont;
        private static TMP_FontAsset s_tmpFont;
        private static bool s_tmpTried;

        public static Font UiFont()
        {
            if (s_uiFont)
            {
                return s_uiFont;
            }

            try
            {
                s_uiFont = Resources.Load<Font>("Fonts/malgun")
                           ?? Resources.Load<Font>("Fonts/NotoSansKR-VF");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[MrFonts] Resources.Load failed: " + ex.Message);
            }

            if (!s_uiFont)
            {
                try
                {
                    s_uiFont = Font.CreateDynamicFontFromOSFont(new[]
                    {
                        "Malgun Gothic",
                        "Noto Sans CJK KR",
                        "Noto Sans KR",
                        "Roboto",
                        "Arial"
                    }, 42);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[MrFonts] OS font failed: " + ex.Message);
                }
            }

            if (!s_uiFont)
            {
                s_uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return s_uiFont;
        }

        public static TMP_FontAsset TmpFont()
        {
            if (s_tmpFont || s_tmpTried)
            {
                return s_tmpFont;
            }

            s_tmpTried = true;
            try
            {
                var source = UiFont();
                if (source)
                {
                    s_tmpFont = TMP_FontAsset.CreateFontAsset(source);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[MrFonts] TMP font create failed: " + ex.Message);
                s_tmpFont = TMP_Settings.defaultFontAsset;
            }

            return s_tmpFont;
        }

        public static void Apply(TextMeshProUGUI tmp)
        {
            if (!tmp)
            {
                return;
            }

            try
            {
                var font = TmpFont();
                if (font)
                {
                    tmp.font = font;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[MrFonts] Apply TMP UGUI failed: " + ex.Message);
            }
        }

        public static void Apply(TextMeshPro tmp)
        {
            if (!tmp)
            {
                return;
            }

            try
            {
                var font = TmpFont();
                if (font)
                {
                    tmp.font = font;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[MrFonts] Apply TMP failed: " + ex.Message);
            }
        }

        public static void Apply(UnityEngine.UI.Text text)
        {
            if (!text)
            {
                return;
            }

            try
            {
                var font = UiFont();
                if (font)
                {
                    text.font = font;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[MrFonts] Apply UI.Text failed: " + ex.Message);
            }
        }

        public static void Apply(TextMesh text)
        {
            if (!text)
            {
                return;
            }

            try
            {
                var font = UiFont();
                if (!font)
                {
                    return;
                }

                text.font = font;
                var size = Mathf.Max(64, text.fontSize);
                text.fontSize = size;
                font.RequestCharactersInTexture(text.text ?? string.Empty, size, text.fontStyle);
                if (font.material && font.material.mainTexture)
                {
                    font.material.mainTexture.filterMode = FilterMode.Bilinear;
                }

                var renderer = text.GetComponent<MeshRenderer>();
                if (renderer && font.material)
                {
                    renderer.sharedMaterial = font.material;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[MrFonts] Apply TextMesh failed: " + ex.Message);
            }
        }

        public static TextMesh Configure(TextMesh textMesh, string text, float characterSize, Color color)
        {
            if (!textMesh)
            {
                return null;
            }

            textMesh.fontSize = 64;
            textMesh.characterSize = characterSize;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;
            textMesh.text = text ?? string.Empty;
            Apply(textMesh);
            return textMesh;
        }
    }
}
