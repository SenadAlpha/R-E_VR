// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Play
{
    /// <summary>
    /// Landing on real MRUK tables/floors. Fake cube floors are removed.
    /// </summary>
    public static class PlayPhysicsGround
    {
        public static void DestroyFakes()
        {
            var root = MainAppPlacement.SpawnRoot();
            DestroyChild(root, "HandInteractionFloor");
            DestroyChild(root, "CubePlayFloor");
        }

        public static Vector3 LandingInFront(Transform head, float spawnDistance)
        {
            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.ProjectOnPlane(head.forward, Vector3.right).normalized;
            }

            var origin = head.position + forward * spawnDistance;
            var room = MRUK.Instance != null ? MRUK.Instance.GetCurrentRoom() : null;
            if (room != null)
            {
                var furniture = LabelFilter.Included(
                    MRUKAnchor.SceneLabels.TABLE
                    | MRUKAnchor.SceneLabels.COUCH
                    | MRUKAnchor.SceneLabels.STORAGE
                    | MRUKAnchor.SceneLabels.BED
                    | MRUKAnchor.SceneLabels.SCREEN
                    | MRUKAnchor.SceneLabels.OTHER);
                var dist = room.TryGetClosestSurfacePosition(origin, out var pos, out _, out var normal, furniture);
                if (dist < 1.8f && Vector3.Dot(normal, Vector3.up) > 0.45f)
                {
                    return pos + normal * 0.1f;
                }

                var floor = LabelFilter.Included(MRUKAnchor.SceneLabels.FLOOR);
                dist = room.TryGetClosestSurfacePosition(origin, out pos, out _, out normal, floor);
                if (dist < 3.5f)
                {
                    return pos + Vector3.up * 0.12f;
                }
            }

            return origin;
        }

        public static float ResolveFloorY(Meta.XR.EnvironmentRaycastManager raycast, Vector3 headPos, Vector3 inFront)
        {
            _ = raycast;
            var room = MRUK.Instance != null ? MRUK.Instance.GetCurrentRoom() : null;
            if (room != null)
            {
                var dist = room.TryGetClosestSurfacePosition(inFront, out var pos, out _, out _,
                    LabelFilter.Included(MRUKAnchor.SceneLabels.FLOOR));
                if (dist < 4f)
                {
                    return pos.y;
                }
            }

            return headPos.y - 1.55f;
        }

        private static void DestroyChild(Transform root, string name)
        {
            var child = root.Find(name);
            if (child)
            {
                Object.Destroy(child.gameObject);
            }
        }
    }
}
