// Copyright (c) Meta Platforms, Inc. and affiliates.

using PassthroughCameraSamples.MainApp.Interaction;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp
{
    public static class MainAppPlacement
    {
        public static void PlaceInFrontOfUser(Transform target, OVRCameraRig cameraRig, float distance)
        {
            PlaceAlongHead(target, cameraRig, distance);
        }

        public static void PlaceAlongHead(Transform target, OVRCameraRig cameraRig, float distance)
        {
            if (!target)
            {
                return;
            }

            if (!cameraRig)
            {
                cameraRig = Object.FindFirstObjectByType<OVRCameraRig>();
            }

            var head = cameraRig ? cameraRig.centerEyeAnchor : Camera.main?.transform;
            if (!head)
            {
                return;
            }

            var direction = head.forward;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.forward;
            }

            direction.Normalize();
            var up = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(direction, up)) > 0.94f)
            {
                up = head.up;
            }

            target.position = head.position + direction * distance;
            target.rotation = Quaternion.LookRotation(direction, up);
        }

        public static void NudgeDown(Transform target, OVRCameraRig cameraRig, float amount = 0.1f)
        {
            if (!target)
            {
                return;
            }

            var head = cameraRig && cameraRig.centerEyeAnchor
                ? cameraRig.centerEyeAnchor
                : Camera.main?.transform;
            if (head)
            {
                target.position += -head.up * amount;
            }
        }

        public static void PlaceAlongPointer(Transform target, OVRCameraRig cameraRig, float distance)
        {
            if (!target)
            {
                return;
            }

            if (!cameraRig)
            {
                cameraRig = Object.FindFirstObjectByType<OVRCameraRig>();
            }

            var head = cameraRig ? cameraRig.centerEyeAnchor : Camera.main?.transform;
            var ray = cameraRig
                ? ControllerAim.GetRay(OVRInput.Controller.RTouch, cameraRig)
                : new Ray(head ? head.position : Vector3.zero, head ? head.forward : Vector3.forward);

            var controllerValid = ray.direction.sqrMagnitude > 0.0001f
                                  && OVRInput.IsControllerConnected(OVRInput.Controller.RTouch);

            Vector3 origin;
            Vector3 direction;
            if (controllerValid)
            {
                origin = ray.origin;
                direction = ray.direction.normalized;
            }
            else if (head)
            {
                origin = head.position;
                direction = head.forward;
            }
            else
            {
                return;
            }

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.forward;
            }

            direction.Normalize();
            var up = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(direction, up)) > 0.94f)
            {
                up = head ? head.up : Vector3.forward;
            }

            target.position = origin + direction * (distance + 0.18f);
            target.rotation = Quaternion.LookRotation(direction, up);
        }

        public static Transform SpawnRoot()
        {
            if (!s_spawnRoot)
            {
                var go = new GameObject("SpawnedWorldItems");
                Object.DontDestroyOnLoad(go);
                s_spawnRoot = go.transform;
            }

            return s_spawnRoot;
        }

        private static Transform s_spawnRoot;
    }
}
