// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Interaction
{
    public static class ControllerAim
    {
        public static Ray GetRay(OVRInput.Controller controller, OVRCameraRig rig)
        {
            var localPos = OVRInput.GetLocalControllerPosition(controller);
            var localRot = OVRInput.GetLocalControllerRotation(controller);
            var tracking = rig ? rig.trackingSpace : null;
            if (tracking)
            {
                return new Ray(
                    tracking.TransformPoint(localPos),
                    tracking.TransformDirection(localRot * Vector3.forward));
            }

            return new Ray(localPos, localRot * Vector3.forward);
        }

        public static Vector3 GetPosition(OVRInput.Controller controller, OVRCameraRig rig)
        {
            var localPos = OVRInput.GetLocalControllerPosition(controller);
            return rig && rig.trackingSpace
                ? rig.trackingSpace.TransformPoint(localPos)
                : localPos;
        }

        public static Quaternion GetRotation(OVRInput.Controller controller, OVRCameraRig rig)
        {
            var localRot = OVRInput.GetLocalControllerRotation(controller);
            return rig && rig.trackingSpace
                ? rig.trackingSpace.rotation * localRot
                : localRot;
        }
    }
}
