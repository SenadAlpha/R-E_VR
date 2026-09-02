// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Paper
{
    /// <summary>
    /// Builds the 4-vertex/2-triangle quad mesh used to flatten a scanned paper.
    /// The 4 vertices are placed directly at the detected corner positions in 3D;
    /// standard GPU rasterization already perspective-correctly interpolates the
    /// UVs across the two triangles, so no custom warp math is needed in a shader -
    /// a plain unlit textured material is enough to get the perspective warp.
    /// </summary>
    public static class PaperWarpMesh
    {
        public static Mesh Build(Transform root, Vector3[] worldCorners, Vector2[] uvCorners)
        {
            var mesh = new Mesh { name = "PaperWarpMesh" };
            var localVerts = new Vector3[4];
            for (var i = 0; i < 4; i++)
            {
                localVerts[i] = root.InverseTransformPoint(worldCorners[i]);
            }

            mesh.vertices = localVerts;
            mesh.uv = uvCorners;
            // Corners are ordered [topLeft, topRight, bottomRight, bottomLeft] by
            // PaperCornerDetector, so this winding produces a single, non-crossed quad.
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
