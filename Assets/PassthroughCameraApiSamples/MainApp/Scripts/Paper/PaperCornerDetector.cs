// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Paper
{
    /// <summary>
    /// Pure C# (no OpenCV) 4-corner detector for the Paper Scan feature.
    /// Pipeline: threshold the Sobel edge map -> flood-fill to find the largest
    /// connected edge blob (stand-in for "largest closed contour") -> reduce that
    /// blob straight to 4 corners by taking the extreme points along the two
    /// diagonal axes (x+y and x-y). For a roughly-rectangular blob this closed-form
    /// step is mathematically equivalent to what Douglas-Peucker would converge to
    /// when simplifying a quad's contour down to 4 points, without needing a full
    /// generic polygon-simplification implementation.
    /// </summary>
    public static class PaperCornerDetector
    {
        /// <summary>
        /// Returns 4 corners in [topLeft, topRight, bottomRight, bottomLeft] order,
        /// normalized to 0..1 image UV space, or null if no plausible quad was found.
        /// </summary>
        public static Vector2[] FindCorners(NativeArray<Color32> pixels, int width, int height, float threshold)
        {
            if (pixels.Length != width * height)
            {
                return null;
            }

            var thresholdByte = (byte)Mathf.Clamp(threshold * 255f, 0f, 255f);
            var isEdge = new bool[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                isEdge[i] = pixels[i].r > thresholdByte;
            }

            isEdge = Dilate(isEdge, width, height);
            isEdge = Dilate(isEdge, width, height);
            isEdge = Dilate(isEdge, width, height);
            var component = FindLargestComponent(isEdge, width, height);
            if (component == null || component.Count < 12)
            {
                return null;
            }

            var topLeft = component[0];
            var topRight = component[0];
            var bottomRight = component[0];
            var bottomLeft = component[0];
            var minSum = float.MaxValue;
            var maxSum = float.MinValue;
            var minDiff = float.MaxValue;
            var maxDiff = float.MinValue;

            foreach (var p in component)
            {
                var sum = p.x + p.y;
                var diff = p.x - p.y;

                if (sum < minSum) { minSum = sum; topLeft = p; }
                if (sum > maxSum) { maxSum = sum; bottomRight = p; }
                if (diff > maxDiff) { maxDiff = diff; topRight = p; }
                if (diff < minDiff) { minDiff = diff; bottomLeft = p; }
            }

            return new[]
            {
                new Vector2(topLeft.x / (float)width, topLeft.y / (float)height),
                new Vector2(topRight.x / (float)width, topRight.y / (float)height),
                new Vector2(bottomRight.x / (float)width, bottomRight.y / (float)height),
                new Vector2(bottomLeft.x / (float)width, bottomLeft.y / (float)height)
            };
        }

        private static List<Vector2Int> FindLargestComponent(bool[] isEdge, int width, int height)
        {
            var visited = new bool[width * height];
            List<Vector2Int> largest = null;
            var queue = new Queue<Vector2Int>();

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var idx = y * width + x;
                    if (!isEdge[idx] || visited[idx])
                    {
                        continue;
                    }

                    var component = new List<Vector2Int>();
                    visited[idx] = true;
                    queue.Enqueue(new Vector2Int(x, y));

                    while (queue.Count > 0)
                    {
                        var p = queue.Dequeue();
                        component.Add(p);

                        TryEnqueue(p.x + 1, p.y, isEdge, visited, width, height, queue);
                        TryEnqueue(p.x - 1, p.y, isEdge, visited, width, height, queue);
                        TryEnqueue(p.x, p.y + 1, isEdge, visited, width, height, queue);
                        TryEnqueue(p.x, p.y - 1, isEdge, visited, width, height, queue);
                        TryEnqueue(p.x + 1, p.y + 1, isEdge, visited, width, height, queue);
                        TryEnqueue(p.x - 1, p.y + 1, isEdge, visited, width, height, queue);
                        TryEnqueue(p.x + 1, p.y - 1, isEdge, visited, width, height, queue);
                        TryEnqueue(p.x - 1, p.y - 1, isEdge, visited, width, height, queue);
                    }

                    if (largest == null || component.Count > largest.Count)
                    {
                        largest = component;
                    }
                }
            }

            return largest;
        }

        private static bool[] Dilate(bool[] input, int width, int height)
        {
            var output = new bool[input.Length];
            for (var y = 1; y < height - 1; y++)
            {
                for (var x = 1; x < width - 1; x++)
                {
                    var i = y * width + x;
                    var value = false;
                    for (var oy = -1; oy <= 1 && !value; oy++)
                    {
                        for (var ox = -1; ox <= 1; ox++)
                        {
                            if (input[i + oy * width + ox])
                            {
                                value = true;
                                break;
                            }
                        }
                    }

                    output[i] = value;
                }
            }

            return output;
        }

        private static void TryEnqueue(int x, int y, bool[] isEdge, bool[] visited, int width, int height, Queue<Vector2Int> queue)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return;
            }

            var idx = y * width + x;
            if (!isEdge[idx] || visited[idx])
            {
                return;
            }

            visited[idx] = true;
            queue.Enqueue(new Vector2Int(x, y));
        }
    }
}
