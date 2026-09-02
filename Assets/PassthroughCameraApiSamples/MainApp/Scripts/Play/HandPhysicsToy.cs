// Copyright (c) Meta Platforms, Inc. and affiliates.

using PassthroughCameraSamples.MainApp.Interaction;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Play
{
    /// <summary>
    /// Near-hand grab + rigidbody throw. Only lives on objects spawned by
    /// HandInteraction — never attached at app start.
    /// </summary>
    public class HandPhysicsToy : MonoBehaviour
    {
        [SerializeField] private float grabPadding = 0.04f;
        [SerializeField] private float throwBoost = 1.15f;

        private static OVRHand[] s_hands = System.Array.Empty<OVRHand>();
        private static OVRCameraRig s_rig;
        private static float s_cacheTime = -1f;

        private Rigidbody m_body;
        private Collider m_collider;
        private Transform m_grabSource;
        private Vector3 m_localPos;
        private Quaternion m_localRot;
        private readonly Vector3[] m_velSamples = new Vector3[8];
        private readonly Vector3[] m_angSamples = new Vector3[8];
        private int m_sampleIndex;
        private Vector3 m_lastPos;
        private Quaternion m_lastRot;
        private bool m_hasLast;

        public bool IsGrabbed => m_grabSource;

        private void Awake()
        {
            m_collider = GetComponent<Collider>();
            m_body = GetComponent<Rigidbody>();
            if (!m_body)
            {
                m_body = gameObject.AddComponent<Rigidbody>();
            }

            m_body.useGravity = true;
            m_body.isKinematic = false;
            m_body.interpolation = RigidbodyInterpolation.Interpolate;
            m_body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            m_body.mass = 0.35f;
        }

        private void OnDisable()
        {
            if (m_grabSource)
            {
                PlayGrabSources.Release(m_grabSource);
                m_grabSource = null;
            }
        }

        private void Update()
        {
            RefreshCache();

            if (m_grabSource)
            {
                if (!StillHolding(m_grabSource))
                {
                    Release();
                    return;
                }

                FollowGrab();
                SampleMotion();
                return;
            }

            TryBeginGrab();
        }

        private static void RefreshCache()
        {
            if (Time.unscaledTime - s_cacheTime < 0.05f && s_hands != null)
            {
                return;
            }

            s_cacheTime = Time.unscaledTime;
            s_hands = FindObjectsByType<OVRHand>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (!s_rig)
            {
                s_rig = FindFirstObjectByType<OVRCameraRig>();
            }
        }

        private void TryBeginGrab()
        {
            Transform best = null;
            var bestDist = float.MaxValue;
            var radius = GrabRadius();

            foreach (var hand in s_hands)
            {
                if (!hand || !hand.IsTracked || !hand.GetFingerIsPinching(OVRHand.HandFinger.Index))
                {
                    continue;
                }

                var pose = hand.PointerPose ? hand.PointerPose : hand.transform;
                if (!pose || !PlayGrabSources.IsFree(pose))
                {
                    continue;
                }

                var dist = Vector3.Distance(pose.position, transform.position);
                if (dist <= radius && dist < bestDist)
                {
                    bestDist = dist;
                    best = pose;
                }
            }

            TryController(OVRInput.Controller.LTouch, radius, ref best, ref bestDist);
            TryController(OVRInput.Controller.RTouch, radius, ref best, ref bestDist);

            if (!best)
            {
                return;
            }

            m_grabSource = best;
            PlayGrabSources.Claim(best);
            m_localPos = Quaternion.Inverse(best.rotation) * (transform.position - best.position);
            m_localRot = Quaternion.Inverse(best.rotation) * transform.rotation;
            m_body.isKinematic = true;
            m_hasLast = false;
            m_sampleIndex = 0;
            for (var i = 0; i < m_velSamples.Length; i++)
            {
                m_velSamples[i] = Vector3.zero;
                m_angSamples[i] = Vector3.zero;
            }
        }

        private void TryController(OVRInput.Controller controller, float radius, ref Transform best, ref float bestDist)
        {
            if (!s_rig || !OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, controller))
            {
                return;
            }

            var pose = ControllerPose(controller);
            if (!pose || !PlayGrabSources.IsFree(pose))
            {
                return;
            }

            var dist = Vector3.Distance(pose.position, transform.position);
            if (dist <= radius && dist < bestDist)
            {
                bestDist = dist;
                best = pose;
            }
        }

        private static Transform ControllerPose(OVRInput.Controller controller)
        {
            if (!s_rig)
            {
                return null;
            }

            if (controller == OVRInput.Controller.LTouch)
            {
                return s_rig.leftControllerAnchor;
            }

            return s_rig.rightControllerAnchor;
        }

        private bool StillHolding(Transform source)
        {
            foreach (var hand in s_hands)
            {
                if (!hand || !hand.IsTracked)
                {
                    continue;
                }

                var pose = hand.PointerPose ? hand.PointerPose : hand.transform;
                if (pose == source)
                {
                    return hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
                }
            }

            if (s_rig && source == s_rig.leftControllerAnchor)
            {
                return OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);
            }

            if (s_rig && source == s_rig.rightControllerAnchor)
            {
                return OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);
            }

            return false;
        }

        private void FollowGrab()
        {
            transform.SetPositionAndRotation(
                m_grabSource.position + m_grabSource.rotation * m_localPos,
                m_grabSource.rotation * m_localRot);
        }

        private void SampleMotion()
        {
            var dt = Time.deltaTime;
            if (dt < 0.0001f)
            {
                return;
            }

            if (m_hasLast)
            {
                m_velSamples[m_sampleIndex % m_velSamples.Length] = (transform.position - m_lastPos) / dt;
                var delta = transform.rotation * Quaternion.Inverse(m_lastRot);
                delta.ToAngleAxis(out var angle, out var axis);
                if (angle > 180f)
                {
                    angle -= 360f;
                }

                m_angSamples[m_sampleIndex % m_angSamples.Length] =
                    axis.sqrMagnitude > 0.0001f ? axis.normalized * (angle * Mathf.Deg2Rad / dt) : Vector3.zero;
                m_sampleIndex++;
            }

            m_lastPos = transform.position;
            m_lastRot = transform.rotation;
            m_hasLast = true;
        }

        private void Release()
        {
            if (m_grabSource)
            {
                PlayGrabSources.Release(m_grabSource);
            }

            m_grabSource = null;
            m_body.isKinematic = false;
            m_body.linearVelocity = Average(m_velSamples) * throwBoost;
            m_body.angularVelocity = Average(m_angSamples);
        }

        private float GrabRadius()
        {
            if (!m_collider)
            {
                return 0.14f;
            }

            return Mathf.Max(0.1f, m_collider.bounds.extents.magnitude + grabPadding);
        }

        private static Vector3 Average(Vector3[] samples)
        {
            var sum = Vector3.zero;
            var n = 0;
            foreach (var s in samples)
            {
                if (s.sqrMagnitude < 0.000001f)
                {
                    continue;
                }

                sum += s;
                n++;
            }

            return n == 0 ? Vector3.zero : sum / n;
        }
    }
}
