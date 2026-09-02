// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Interaction
{
    /// <summary>
    /// Draws a thin aqua wireframe box around a spawned item's bounds using 12 tiny
    /// LineRenderers (one per box edge). Works for any item type (TMP text, image
    /// quads, procedural paper mesh, primitives) since it only depends on the
    /// object's Collider/Renderer bounds, not on its material or shader.
    /// </summary>
    public class AquaOutline : MonoBehaviour
    {
        private static readonly Vector3[] s_corners =
        {
            new(-1, -1, -1), new(1, -1, -1), new(1, -1, 1), new(-1, -1, 1),
            new(-1, 1, -1), new(1, 1, -1), new(1, 1, 1), new(-1, 1, 1)
        };

        private static readonly int[,] s_edges =
        {
            { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 },
            { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 },
            { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 }
        };

        private LineRenderer[] m_edgeRenderers;
        private Color m_color;
        private float m_width;

        public void Configure(Color color, float width)
        {
            m_color = color;
            m_width = width;

            if (m_edgeRenderers == null)
            {
                m_edgeRenderers = new LineRenderer[s_edges.GetLength(0)];
                var sharedMaterial = new Material(Shader.Find("Sprites/Default"));

                for (var i = 0; i < m_edgeRenderers.Length; i++)
                {
                    var edgeGo = new GameObject($"OutlineEdge_{i}");
                    edgeGo.transform.SetParent(transform, false);

                    var lineRenderer = edgeGo.AddComponent<LineRenderer>();
                    lineRenderer.material = sharedMaterial;
                    lineRenderer.positionCount = 2;
                    lineRenderer.useWorldSpace = false;
                    lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    lineRenderer.receiveShadows = false;
                    lineRenderer.enabled = false;
                    m_edgeRenderers[i] = lineRenderer;
                }
            }

            foreach (var lineRenderer in m_edgeRenderers)
            {
                lineRenderer.startColor = lineRenderer.endColor = m_color;
                lineRenderer.widthMultiplier = m_width;
            }
        }

        public void SetVisible(bool visible)
        {
            if (m_edgeRenderers == null)
            {
                return;
            }

            if (visible)
            {
                RecomputeBox();
            }

            foreach (var lineRenderer in m_edgeRenderers)
            {
                lineRenderer.enabled = visible;
            }
        }

        private void RecomputeBox()
        {
            var box = GetComponent<BoxCollider>();
            Vector3 center;
            Vector3 half;
            if (box)
            {
                center = box.center;
                var size = box.size;
                if (size.z < 0.02f)
                {
                    size.z = 0.02f;
                }

                half = size * 0.5f;
            }
            else
            {
                var bounds = InteractionSetup.ComputeLocalBounds(gameObject);
                center = bounds.center;
                half = bounds.extents;
                if (half.z < 0.01f)
                {
                    half.z = 0.01f;
                }
            }

            for (var i = 0; i < s_edges.GetLength(0); i++)
            {
                var a = center + Vector3.Scale(s_corners[s_edges[i, 0]], half);
                var b = center + Vector3.Scale(s_corners[s_edges[i, 1]], half);
                m_edgeRenderers[i].SetPosition(0, a);
                m_edgeRenderers[i].SetPosition(1, b);
            }
        }
    }
}
