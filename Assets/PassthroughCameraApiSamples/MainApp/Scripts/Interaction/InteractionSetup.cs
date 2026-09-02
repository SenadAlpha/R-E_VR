// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Interaction
{
    public static class InteractionSetup
    {
        public static Bounds ComputeLocalBounds(GameObject go)
        {
            var box = go.GetComponent<BoxCollider>();
            if (box && box.enabled)
            {
                var size = box.size;
                if (size.z < 0.02f)
                {
                    size.z = 0.02f;
                }

                return new Bounds(box.center, size);
            }

            foreach (var collider in go.GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled || collider is MeshCollider)
                {
                    continue;
                }

                if (collider is BoxCollider childBox)
                {
                    var worldCenter = collider.transform.TransformPoint(childBox.center);
                    var worldSize = Vector3.Scale(childBox.size, collider.transform.lossyScale);
                    var localCenter = go.transform.InverseTransformPoint(worldCenter);
                    var localSize = go.transform.InverseTransformVector(worldSize);
                    localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Max(0.02f, Mathf.Abs(localSize.z)));
                    return new Bounds(localCenter, localSize);
                }
            }

            return new Bounds(Vector3.zero, new Vector3(0.3f, 0.18f, 0.04f));
        }
    }
}
