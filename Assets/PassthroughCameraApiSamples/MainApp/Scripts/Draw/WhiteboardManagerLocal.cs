// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Draw
{
    /// <summary>
    /// Local (single-player) replacement for MRMotif's Fusion-networked
    /// WhiteboardManagerMotif. Owns the drawable Texture2D and exposes brush-stamp
    /// drawing along a line segment in board-local UV space.
    /// </summary>
    public class WhiteboardManagerLocal : MonoBehaviour
    {
        [SerializeField] private Color whiteboardColor = Color.white;
        [SerializeField] private Renderer whiteboardRenderer;
        [SerializeField] private int textureWidth = 1024;
        [SerializeField] private int textureHeight = 1024;

        private Texture2D m_texture;
        private bool m_dirty;

        private void Awake()
        {
            m_texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            Clear();
            m_texture.Apply();

            if (whiteboardRenderer)
            {
                whiteboardRenderer.material.mainTexture = m_texture;
            }
        }

        /// <summary>Assigns the target renderer at runtime for code-spawned boards.</summary>
        public void SetRenderer(Renderer targetRenderer)
        {
            whiteboardRenderer = targetRenderer;
            if (whiteboardRenderer && m_texture)
            {
                whiteboardRenderer.material.mainTexture = m_texture;
            }
        }

        private void LateUpdate()
        {
            if (m_dirty)
            {
                m_texture.Apply();
                m_dirty = false;
            }
        }

        /// <summary>Converts a world-space hit point (on this board's plane) to 0..1 UV.</summary>
        public Vector2 WorldToUv(Vector3 worldPos)
        {
            var local = transform.InverseTransformPoint(worldPos);
            return new Vector2(local.x + 0.5f, local.y + 0.5f);
        }

        public void DrawLine(Vector2 startUv, Vector2 endUv, Color color, int brushRadius)
        {
            var pixelDistance = Vector2.Distance(startUv, endUv) * textureWidth;
            var steps = Mathf.Max(1, Mathf.CeilToInt(pixelDistance));

            for (var i = 0; i <= steps; i++)
            {
                var uv = Vector2.Lerp(startUv, endUv, i / (float)steps);
                DrawBrushStamp(uv, color, brushRadius);
            }

            m_dirty = true;
        }

        private void DrawBrushStamp(Vector2 uv, Color color, int brushRadius)
        {
            var centerX = Mathf.RoundToInt(uv.x * textureWidth);
            var centerY = Mathf.RoundToInt(uv.y * textureHeight);
            var radiusSqr = brushRadius * brushRadius;

            for (var y = centerY - brushRadius; y <= centerY + brushRadius; y++)
            {
                if (y < 0 || y >= textureHeight)
                {
                    continue;
                }

                for (var x = centerX - brushRadius; x <= centerX + brushRadius; x++)
                {
                    if (x < 0 || x >= textureWidth)
                    {
                        continue;
                    }

                    var dx = x - centerX;
                    var dy = y - centerY;
                    if (dx * dx + dy * dy <= radiusSqr)
                    {
                        m_texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        public void Clear()
        {
            var fill = new Color32[textureWidth * textureHeight];
            var c = (Color32)whiteboardColor;
            for (var i = 0; i < fill.Length; i++)
            {
                fill[i] = c;
            }

            m_texture.SetPixels32(fill);
            m_dirty = true;
        }
    }
}
