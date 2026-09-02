// Copyright (c) Meta Platforms, Inc. and affiliates.

using PassthroughCameraSamples.MainApp.Interaction;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Admin
{
    /// <summary>
    /// Flat photo-like document: Unity mesh for hit/fallback plus an OVROverlay Quad
    /// (compositor, bicubic + sharpen) while selected. 왜곡 toggles a mild cylinder
    /// mesh — not OverlayShape.Cylinder, which curves far too hard.
    /// </summary>
    public class DocumentSurface : MonoBehaviour
    {
        [SerializeField] private float curveDegrees = 16f;
        [SerializeField] private int curveSegments = 16;

        private Texture2D m_texture;
        private MeshFilter m_filter;
        private MeshRenderer m_renderer;
        private OVROverlay m_overlay;
        private SpawnableItem m_item;
        private Mesh m_flat;
        private Mesh m_curved;
        private bool m_curvedOn;
        private static readonly System.Collections.Generic.List<DocumentSurface> s_overlays = new();
        private const int MaxOverlays = 3;

        public bool IsCurved => m_curvedOn;

        public void Configure(Texture2D texture)
        {
            m_texture = texture;
            m_filter = GetComponent<MeshFilter>();
            m_renderer = GetComponent<MeshRenderer>();
            m_item = GetComponent<SpawnableItem>();
            m_flat = m_filter ? m_filter.sharedMesh : null;
            m_curved = BuildCylinderStrip(curveDegrees, curveSegments);

            m_overlay = GetComponent<OVROverlay>();
            if (!m_overlay)
            {
                m_overlay = gameObject.AddComponent<OVROverlay>();
            }

            m_overlay.currentOverlayType = OVROverlay.OverlayType.Overlay;
            m_overlay.currentOverlayShape = OVROverlay.OverlayShape.Quad;
            m_overlay.isDynamic = false;
            m_overlay.useBicubicFiltering = true;
            m_overlay.useEfficientSharpen = true;
            m_overlay.useAutomaticFiltering = false;
            m_overlay.textures = new Texture[] { texture, texture };
            m_overlay.noDepthBufferTesting = true;
            m_overlay.hidden = false;
            if (m_renderer)
            {
                m_renderer.enabled = false;
            }

            ClaimOverlaySlot();
        }

        public void ToggleCurve()
        {
            m_curvedOn = !m_curvedOn;
            if (m_filter)
            {
                m_filter.sharedMesh = m_curvedOn && m_curved ? m_curved : m_flat;
            }

            ApplyOverlayState();
        }

        private void LateUpdate()
        {
            ApplyOverlayState();
        }

        private void OnDestroy()
        {
            s_overlays.Remove(this);
            if (m_curved)
            {
                Destroy(m_curved);
            }
        }

        private void ClaimOverlaySlot()
        {
            s_overlays.Remove(this);
            s_overlays.Add(this);
            while (s_overlays.Count > MaxOverlays)
            {
                var oldest = s_overlays[0];
                s_overlays.RemoveAt(0);
                if (oldest && oldest.m_overlay)
                {
                    oldest.m_overlay.hidden = true;
                    if (oldest.m_renderer)
                    {
                        oldest.m_renderer.enabled = true;
                    }
                }
            }
        }

        private void ApplyOverlayState()
        {
            if (!m_overlay)
            {
                return;
            }

            var slot = s_overlays.Contains(this);
            var useOverlay = slot && !m_curvedOn && m_texture;
            m_overlay.hidden = !useOverlay;
            m_overlay.currentOverlayShape = OVROverlay.OverlayShape.Quad;
            if (m_renderer)
            {
                m_renderer.enabled = !useOverlay;
            }
        }

        private static Mesh BuildCylinderStrip(float degrees, int segments)
        {
            segments = Mathf.Max(4, segments);
            var mesh = new Mesh { name = "DocumentCurve" };
            var verts = new Vector3[(segments + 1) * 2];
            var uvs = new Vector2[(segments + 1) * 2];
            var tris = new int[segments * 6];

            var theta = degrees * Mathf.Deg2Rad;
            var half = theta * 0.5f;
            var radius = 0.5f / Mathf.Max(0.001f, Mathf.Sin(half));

            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var angle = -half + theta * t;
                var x = Mathf.Sin(angle) * radius;
                var z = Mathf.Cos(angle) * radius - radius;
                verts[i] = new Vector3(x, 0.5f, z);
                verts[i + segments + 1] = new Vector3(x, -0.5f, z);
                uvs[i] = new Vector2(t, 1f);
                uvs[i + segments + 1] = new Vector2(t, 0f);
            }

            var ti = 0;
            for (var i = 0; i < segments; i++)
            {
                var topA = i;
                var topB = i + 1;
                var botA = i + segments + 1;
                var botB = i + segments + 2;
                tris[ti++] = topA;
                tris[ti++] = topB;
                tris[ti++] = botA;
                tris[ti++] = topB;
                tris[ti++] = botB;
                tris[ti++] = botA;
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
