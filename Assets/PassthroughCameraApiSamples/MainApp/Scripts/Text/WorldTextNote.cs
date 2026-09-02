// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace PassthroughCameraSamples.MainApp.TextTools
{
    /// <summary>
    /// World TextMesh note. Same layout idea as JinsanRNEProject MRNote:
    /// background quad follows glyph bounds, with user-adjustable padding and alpha.
    /// </summary>
    public class WorldTextNote : MonoBehaviour
    {
        [SerializeField] private TextMesh textMesh;
        [SerializeField] private Renderer backgroundRenderer;
        [SerializeField] private float paddingX = 0.05f;
        [SerializeField] private float paddingY = 0.035f;
        [SerializeField] private float boxScale = 1f;
        [SerializeField] private Color backgroundColor = new(0.04f, 0.05f, 0.07f, 0.72f);

        public TextMesh TextMesh => textMesh;
        public Renderer Background => backgroundRenderer;
        public Color BackgroundColor => backgroundColor;
        public float BoxScale => boxScale;

        public void Initialize(TextMesh text, Renderer background)
        {
            textMesh = text;
            backgroundRenderer = background;
            ApplyBackgroundColor();
            RefreshLayout();
        }

        public void SetBackgroundColor(Color color)
        {
            backgroundColor = color;
            ApplyBackgroundColor();
        }

        public void SetBackgroundAlpha(float alpha)
        {
            backgroundColor.a = Mathf.Clamp01(alpha);
            ApplyBackgroundColor();
        }

        public void AdjustBoxScale(float multiply)
        {
            boxScale = Mathf.Clamp(boxScale * multiply, 0.45f, 3.2f);
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            if (!textMesh)
            {
                return;
            }

            var textRenderer = textMesh.GetComponent<Renderer>();
            if (!textRenderer)
            {
                return;
            }

            var textBounds = textRenderer.localBounds;
            var width = Mathf.Max(0.12f, textBounds.size.x + paddingX * 2f * boxScale);
            var height = Mathf.Max(0.08f, textBounds.size.y + paddingY * 2f * boxScale);
            var center = textMesh.transform.localPosition + textBounds.center;

            if (backgroundRenderer)
            {
                var background = backgroundRenderer.transform;
                background.localPosition = new Vector3(center.x, center.y, 0f);
                background.localScale = new Vector3(width, height, 1f);
            }

            var hitBox = GetComponent<BoxCollider>();
            if (!hitBox)
            {
                hitBox = gameObject.AddComponent<BoxCollider>();
            }

            hitBox.center = new Vector3(center.x, center.y, 0f);
            hitBox.size = new Vector3(width, height, 0.025f);
        }

        private void ApplyBackgroundColor()
        {
            if (!backgroundRenderer || !backgroundRenderer.material)
            {
                return;
            }

            var material = backgroundRenderer.material;
            material.color = backgroundColor;
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", backgroundColor);
            }
        }
    }
}
