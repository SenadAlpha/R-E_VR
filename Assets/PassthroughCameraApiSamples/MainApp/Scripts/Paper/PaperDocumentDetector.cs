// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Paper
{
    /// <summary>
    /// CPU document finder + perspective warp, ported from JinsanRNEProject DocumentScanner.
    /// Bright paper region first, Sobel edges as fallback. No GPU readback.
    /// </summary>
    public static class PaperDocumentDetector
    {
        public static bool TryFind(Color32[] source, int sourceWidth, int sourceHeight, out Vector2[] corners)
        {
            corners = null;
            if (source == null || sourceWidth < 8 || sourceHeight < 8)
            {
                return false;
            }

            const int detectionWidth = 320;
            const float minimumDocumentArea = 0.02f;
            var width = Mathf.Min(detectionWidth, sourceWidth);
            var height = Mathf.Max(2, Mathf.RoundToInt(sourceHeight * (width / (float)sourceWidth)));
            var gray = new byte[width * height];
            var sampled = new Color32[width * height];
            long brightnessSum = 0;

            for (var y = 0; y < height; y++)
            {
                var sourceY = Mathf.Min(sourceHeight - 1, Mathf.RoundToInt(y * (sourceHeight - 1f) / (height - 1f)));
                for (var x = 0; x < width; x++)
                {
                    var sourceX = Mathf.Min(sourceWidth - 1, Mathf.RoundToInt(x * (sourceWidth - 1f) / (width - 1f)));
                    var c = source[sourceY * sourceWidth + sourceX];
                    var index = y * width + x;
                    sampled[index] = c;
                    gray[index] = (byte)((c.r * 77 + c.g * 150 + c.b * 29) >> 8);
                    brightnessSum += gray[index];
                }
            }

            var averageBrightness = Mathf.RoundToInt(brightnessSum / (float)gray.Length);
            var paperThreshold = Mathf.Clamp(averageBrightness + 12, 120, 225);
            var paperMask = new bool[gray.Length];
            for (var i = 0; i < paperMask.Length; i++)
            {
                var c = sampled[i];
                var chroma = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                paperMask[i] = gray[i] >= paperThreshold && chroma <= 80;
            }

            paperMask = Dilate(paperMask, width, height);
            var paperComponent = FindLargestComponent(paperMask, width, height);
            if (IsPaperCandidate(paperComponent, width, height) &&
                TryExtractCorners(paperComponent, width, height, minimumDocumentArea, out var detectedPaper))
            {
                corners = ScaleCorners(detectedPaper, width, height, sourceWidth, sourceHeight);
                return true;
            }

            var blurred = BoxBlur(gray, width, height);
            var edges = SobelEdges(blurred, width, height, 55);
            edges = Dilate(edges, width, height);
            edges = Dilate(edges, width, height);
            var component = FindLargestComponent(edges, width, height);
            if (component.Count < width + height)
            {
                return false;
            }

            if (!TryExtractCorners(component, width, height, minimumDocumentArea, out var detected))
            {
                return false;
            }

            corners = ScaleCorners(detected, width, height, sourceWidth, sourceHeight);
            return true;
        }

        public static Texture2D Warp(Color32[] source, int sourceWidth, int sourceHeight, Vector2[] corners,
            int maximumOutputSize = 1536)
        {
            if (corners == null || corners.Length < 4)
            {
                return null;
            }

            var width = Mathf.RoundToInt(Mathf.Max(
                Vector2.Distance(corners[0], corners[1]),
                Vector2.Distance(corners[3], corners[2])));
            var height = Mathf.RoundToInt(Mathf.Max(
                Vector2.Distance(corners[0], corners[3]),
                Vector2.Distance(corners[1], corners[2])));
            if (width < 2 || height < 2)
            {
                return null;
            }

            var outputScale = Mathf.Min(1f, maximumOutputSize / (float)Mathf.Max(width, height));
            width = Mathf.Max(2, Mathf.RoundToInt(width * outputScale));
            height = Mathf.Max(2, Mathf.RoundToInt(height * outputScale));

            Vector2[] destination =
            {
                new(0, 0),
                new(width - 1, 0),
                new(width - 1, height - 1),
                new(0, height - 1)
            };

            var homography = SolveHomography(destination, corners);
            if (homography == null)
            {
                return null;
            }

            var output = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var divisor = homography[6] * x + homography[7] * y + 1.0;
                    var sourceX = (float)((homography[0] * x + homography[1] * y + homography[2]) / divisor);
                    var sourceY = (float)((homography[3] * x + homography[4] * y + homography[5]) / divisor);
                    output[y * width + x] = BilinearSample(source, sourceWidth, sourceHeight, sourceX, sourceY);
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(output);
            texture.Apply(false, false);
            FlipVertical(texture);
            return texture;
        }

        public static Texture2D CropAABB(Color32[] source, int sourceWidth, int sourceHeight, Vector2[] corners)
        {
            if (source == null || corners == null || corners.Length < 4)
            {
                return null;
            }

            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = 0;
            var maxY = 0;
            foreach (var corner in corners)
            {
                minX = Mathf.Min(minX, Mathf.FloorToInt(corner.x));
                minY = Mathf.Min(minY, Mathf.FloorToInt(corner.y));
                maxX = Mathf.Max(maxX, Mathf.CeilToInt(corner.x));
                maxY = Mathf.Max(maxY, Mathf.CeilToInt(corner.y));
            }

            minX = Mathf.Clamp(minX - 4, 0, sourceWidth - 2);
            minY = Mathf.Clamp(minY - 4, 0, sourceHeight - 2);
            maxX = Mathf.Clamp(maxX + 4, minX + 2, sourceWidth - 1);
            maxY = Mathf.Clamp(maxY + 4, minY + 2, sourceHeight - 1);
            var width = maxX - minX + 1;
            var height = maxY - minY + 1;
            var output = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                Array.Copy(source, (minY + y) * sourceWidth + minX, output, y * width, width);
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(output);
            texture.Apply(false, false);
            FlipVertical(texture);
            return texture;
        }

        public static bool IsAlmostRectangle(Vector2[] corners)
        {
            if (corners == null || corners.Length < 4)
            {
                return false;
            }

            var d01 = Vector2.Distance(corners[0], corners[1]);
            var d12 = Vector2.Distance(corners[1], corners[2]);
            var d23 = Vector2.Distance(corners[2], corners[3]);
            var d30 = Vector2.Distance(corners[3], corners[0]);
            var min = Mathf.Min(Mathf.Min(d01, d12), Mathf.Min(d23, d30));
            if (min < 8f)
            {
                return false;
            }

            var h = Mathf.Max(d01, d23) / Mathf.Min(d01, d23);
            var v = Mathf.Max(d12, d30) / Mathf.Min(d12, d30);
            return h < 1.28f && v < 1.28f;
        }

        public static Texture2D ToTexture(Color32[] source, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(source);
            texture.Apply(false, false);
            FlipVertical(texture);
            return texture;
        }

        public static void FlipVertical(Texture2D texture)
        {
            if (!texture)
            {
                return;
            }

            var width = texture.width;
            var height = texture.height;
            var pixels = texture.GetPixels32();
            var row = new Color32[width];
            for (var y = 0; y < height / 2; y++)
            {
                var top = y * width;
                var bottom = (height - 1 - y) * width;
                Array.Copy(pixels, top, row, 0, width);
                Array.Copy(pixels, bottom, pixels, top, width);
                Array.Copy(row, 0, pixels, bottom, width);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        public static Color32[] FlipHorizontal(Color32[] source, int width, int height)
        {
            var output = new Color32[source.Length];
            for (var y = 0; y < height; y++)
            {
                var row = y * width;
                for (var x = 0; x < width; x++)
                {
                    output[row + (width - 1 - x)] = source[row + x];
                }
            }

            return output;
        }

        private static bool TryExtractCorners(List<int> component, int width, int height, float minArea,
            out Vector2[] detected)
        {
            var topLeft = Vector2.zero;
            var topRight = Vector2.zero;
            var bottomRight = Vector2.zero;
            var bottomLeft = Vector2.zero;
            var minSum = float.MaxValue;
            var maxSum = float.MinValue;
            var minDiff = float.MaxValue;
            var maxDiff = float.MinValue;

            foreach (var index in component)
            {
                var x = index % width;
                var y = index / width;
                var sum = x + y;
                var diff = x - y;
                if (sum < minSum) { minSum = sum; topLeft = new Vector2(x, y); }
                if (sum > maxSum) { maxSum = sum; bottomRight = new Vector2(x, y); }
                if (diff > maxDiff) { maxDiff = diff; topRight = new Vector2(x, y); }
                if (diff < minDiff) { minDiff = diff; bottomLeft = new Vector2(x, y); }
            }

            detected = new[] { topLeft, topRight, bottomRight, bottomLeft };
            if (PolygonArea(detected) < width * height * minArea)
            {
                detected = null;
                return false;
            }

            return true;
        }

        private static Vector2[] ScaleCorners(Vector2[] detected, int width, int height, int sourceWidth,
            int sourceHeight)
        {
            var scaleX = sourceWidth / (float)width;
            var scaleY = sourceHeight / (float)height;
            var corners = new Vector2[4];
            for (var i = 0; i < 4; i++)
            {
                corners[i] = new Vector2(detected[i].x * scaleX, detected[i].y * scaleY);
            }

            return corners;
        }

        private static bool IsPaperCandidate(List<int> component, int width, int height)
        {
            if (component == null ||
                component.Count < width * height * 0.015f ||
                component.Count > width * height * 0.95f)
            {
                return false;
            }

            var borderPixels = 0;
            foreach (var index in component)
            {
                var x = index % width;
                var y = index / width;
                if (x <= 1 || x >= width - 2 || y <= 1 || y >= height - 2)
                {
                    borderPixels++;
                }
            }

            return borderPixels < component.Count * 0.28f;
        }

        private static byte[] BoxBlur(byte[] input, int width, int height)
        {
            var output = new byte[input.Length];
            for (var y = 1; y < height - 1; y++)
            {
                for (var x = 1; x < width - 1; x++)
                {
                    var sum = 0;
                    for (var oy = -1; oy <= 1; oy++)
                    {
                        for (var ox = -1; ox <= 1; ox++)
                        {
                            sum += input[(y + oy) * width + x + ox];
                        }
                    }

                    output[y * width + x] = (byte)(sum / 9);
                }
            }

            return output;
        }

        private static bool[] SobelEdges(byte[] input, int width, int height, int minThreshold)
        {
            var output = new bool[input.Length];
            var magnitudes = new int[input.Length];
            long magnitudeSum = 0;
            var sampleCount = 0;
            for (var y = 1; y < height - 1; y++)
            {
                for (var x = 1; x < width - 1; x++)
                {
                    var i = y * width + x;
                    var gx = -input[i - width - 1] + input[i - width + 1]
                             - 2 * input[i - 1] + 2 * input[i + 1]
                             - input[i + width - 1] + input[i + width + 1];
                    var gy = -input[i - width - 1] - 2 * input[i - width] - input[i - width + 1]
                             + input[i + width - 1] + 2 * input[i + width] + input[i + width + 1];
                    var magnitude = Mathf.Abs(gx) + Mathf.Abs(gy);
                    magnitudes[i] = magnitude;
                    magnitudeSum += magnitude;
                    sampleCount++;
                }
            }

            var adaptive = sampleCount > 0
                ? Mathf.RoundToInt(magnitudeSum / (float)sampleCount * 1.6f)
                : minThreshold;
            var threshold = Mathf.Clamp(Mathf.Max(minThreshold, adaptive), 40, 280);
            for (var i = 0; i < output.Length; i++)
            {
                output[i] = magnitudes[i] >= threshold;
            }

            return output;
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

        private static List<int> FindLargestComponent(bool[] mask, int width, int height)
        {
            var visited = new bool[mask.Length];
            var largest = new List<int>();
            var queue = new Queue<int>();
            for (var start = 0; start < mask.Length; start++)
            {
                if (!mask[start] || visited[start])
                {
                    continue;
                }

                var current = new List<int>();
                queue.Enqueue(start);
                visited[start] = true;
                while (queue.Count > 0)
                {
                    var index = queue.Dequeue();
                    current.Add(index);
                    var x = index % width;
                    var y = index / width;
                    for (var oy = -1; oy <= 1; oy++)
                    {
                        for (var ox = -1; ox <= 1; ox++)
                        {
                            if (ox == 0 && oy == 0)
                            {
                                continue;
                            }

                            var nx = x + ox;
                            var ny = y + oy;
                            if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                            {
                                continue;
                            }

                            var next = ny * width + nx;
                            if (visited[next] || !mask[next])
                            {
                                continue;
                            }

                            visited[next] = true;
                            queue.Enqueue(next);
                        }
                    }
                }

                if (current.Count > largest.Count)
                {
                    largest = current;
                }
            }

            return largest;
        }

        private static double[] SolveHomography(Vector2[] from, Vector2[] to)
        {
            var matrix = new double[8, 9];
            for (var i = 0; i < 4; i++)
            {
                double x = from[i].x;
                double y = from[i].y;
                double u = to[i].x;
                double v = to[i].y;
                var row = i * 2;
                matrix[row, 0] = x;
                matrix[row, 1] = y;
                matrix[row, 2] = 1;
                matrix[row, 6] = -u * x;
                matrix[row, 7] = -u * y;
                matrix[row, 8] = u;
                matrix[row + 1, 3] = x;
                matrix[row + 1, 4] = y;
                matrix[row + 1, 5] = 1;
                matrix[row + 1, 6] = -v * x;
                matrix[row + 1, 7] = -v * y;
                matrix[row + 1, 8] = v;
            }

            for (var column = 0; column < 8; column++)
            {
                var pivot = column;
                for (var row = column + 1; row < 8; row++)
                {
                    if (Math.Abs(matrix[row, column]) > Math.Abs(matrix[pivot, column]))
                    {
                        pivot = row;
                    }
                }

                if (Math.Abs(matrix[pivot, column]) < 0.0000001)
                {
                    return null;
                }

                for (var c = column; c < 9; c++)
                {
                    (matrix[column, c], matrix[pivot, c]) = (matrix[pivot, c], matrix[column, c]);
                }

                var scale = matrix[column, column];
                for (var c = column; c < 9; c++)
                {
                    matrix[column, c] /= scale;
                }

                for (var row = 0; row < 8; row++)
                {
                    if (row == column)
                    {
                        continue;
                    }

                    var factor = matrix[row, column];
                    for (var c = column; c < 9; c++)
                    {
                        matrix[row, c] -= factor * matrix[column, c];
                    }
                }
            }

            var result = new double[8];
            for (var i = 0; i < 8; i++)
            {
                result[i] = matrix[i, 8];
            }

            return result;
        }

        private static Color32 BilinearSample(Color32[] pixels, int width, int height, float x, float y)
        {
            x = Mathf.Clamp(x, 0, width - 1);
            y = Mathf.Clamp(y, 0, height - 1);
            var x0 = Mathf.FloorToInt(x);
            var y0 = Mathf.FloorToInt(y);
            var x1 = Mathf.Min(x0 + 1, width - 1);
            var y1 = Mathf.Min(y0 + 1, height - 1);
            var tx = x - x0;
            var ty = y - y0;
            Color a = Color.Lerp(pixels[y0 * width + x0], pixels[y0 * width + x1], tx);
            Color b = Color.Lerp(pixels[y1 * width + x0], pixels[y1 * width + x1], tx);
            return Color.Lerp(a, b, ty);
        }

        private static float PolygonArea(Vector2[] points)
        {
            var area = 0f;
            for (var i = 0; i < points.Length; i++)
            {
                var a = points[i];
                var b = points[(i + 1) % points.Length];
                area += a.x * b.y - b.x * a.y;
            }

            return Mathf.Abs(area) * 0.5f;
        }
    }
}
