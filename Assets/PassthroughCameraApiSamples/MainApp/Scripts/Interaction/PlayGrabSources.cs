// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Interaction
{
    /// <summary>
    /// Shared pinch/grip claim so play toys do not steal the same hand from each other.
    /// Empty until a play object actually starts grabbing.
    /// </summary>
    internal static class PlayGrabSources
    {
        private static readonly HashSet<Transform> s_used = new();

        public static bool IsFree(Transform source)
        {
            return source && !s_used.Contains(source);
        }

        public static void Claim(Transform source)
        {
            if (source)
            {
                s_used.Add(source);
            }
        }

        public static void Release(Transform source)
        {
            if (source)
            {
                s_used.Remove(source);
            }
        }
    }
}
