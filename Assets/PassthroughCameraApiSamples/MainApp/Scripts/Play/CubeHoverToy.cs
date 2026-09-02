// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Interaction
{
    public class CubeHoverToy : MonoBehaviour
    {
        [SerializeField] private float spinDegreesPerSecond = 140f;
        [SerializeField] private float hoverHeight = 0.12f;
        [SerializeField] private float followLerp = 16f;
        [SerializeField] private float grabRadius = 0.16f;
        [SerializeField] private float throwBoost = 1.15f;

        private enum Mode
        {
            Attached,
            Released,
            Held
        }

        private SpawnableItem m_item;
        private Rigidbody m_body;
        private ParticleSystem m_fx;
        private TrailRenderer m_trail;
        private LineRenderer m_beam;
        private Vector3 m_baseScale;
        private OVRCameraRig m_rig;
        private Mode m_mode = Mode.Attached;
        private Transform m_grabSource;
        private Vector3 m_localPos;
        private Quaternion m_localRot;
        private readonly Vector3[] m_velSamples = new Vector3[8];
        private readonly Vector3[] m_angSamples = new Vector3[8];
        private int m_sampleIndex;
        private Vector3 m_lastPos;
        private Quaternion m_lastRot;
        private bool m_hasLast;
        private bool m_bothPinching;
        private bool m_waitPinchClear;

        private void Awake()
        {
            m_item = GetComponent<SpawnableItem>();
            m_baseScale = transform.localScale;
            m_rig = FindFirstObjectByType<OVRCameraRig>();
            m_fx = BuildSparkles();
            m_trail = BuildTrail();
            m_beam = BuildBeam();

            var snap = GetComponent<WallSnapBehaviour>();
            if (snap)
            {
                Destroy(snap);
            }

            m_body = GetComponent<Rigidbody>();
            if (!m_body)
            {
                m_body = gameObject.AddComponent<Rigidbody>();
            }

            m_body.interpolation = RigidbodyInterpolation.Interpolate;
            m_body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            m_body.mass = 0.4f;
            SetPhysics(false);
        }

        private void OnDisable()
        {
            PlayGrabSources.Release(m_grabSource);
            m_grabSource = null;
        }

        private void LateUpdate()
        {
            if (!m_rig)
            {
                m_rig = FindFirstObjectByType<OVRCameraRig>();
            }

            var palms = CollectHoverPalms();
            var pinchCount = CountNearbyPinches();
            var bothPinch = pinchCount >= 2;
            var bothPinchDown = bothPinch && !m_bothPinching;
            m_bothPinching = bothPinch;
            if (m_waitPinchClear && pinchCount == 0)
            {
                m_waitPinchClear = false;
            }

            if (m_mode == Mode.Held)
            {
                if (!StillHolding(m_grabSource))
                {
                    EnterReleased(true);
                }
                else
                {
                    FollowGrab();
                    SampleMotion();
                    SelectIfNeeded();
                }
            }
            else if (m_mode == Mode.Released)
            {
                SampleMotion();
                if (TryBeginGripHold())
                {
                    // grabbed
                }
                else if (!m_waitPinchClear && pinchCount >= 1)
                {
                    EnterAttached();
                }
            }
            else
            {
                SampleMotion();
                if (bothPinchDown)
                {
                    m_waitPinchClear = true;
                    EnterReleased(true);
                }
                else if (TryBeginGripHold())
                {
                    // grabbed
                }
                else
                {
                    ApplyHover(palms);
                    SelectIfNeeded();
                }
            }

            var active = m_mode != Mode.Released;
            var twoHand = palms.Count >= 2 && m_mode == Mode.Attached;
            var spin = spinDegreesPerSecond * (twoHand ? 3.4f : active ? 1.6f : 1f);
            if (m_mode != Mode.Released)
            {
                transform.Rotate(Time.deltaTime * spin * 0.35f, Time.deltaTime * spin, Time.deltaTime * spin * 0.2f, Space.World);
            }
            else if (m_body && m_body.angularVelocity.magnitude < 1.2f)
            {
                m_body.AddTorque(new Vector3(0.08f, 0.22f, 0.05f), ForceMode.Force);
            }

            SetFx(twoHand ? 56f : active ? 14f : 0f);
            if (m_trail)
            {
                m_trail.emitting = active;
                m_trail.time = twoHand ? 0.45f : 0.18f;
            }

            UpdateFistBeam();
        }

        private void SelectIfNeeded()
        {
            if (m_item && !m_item.IsSelected)
            {
                m_item.Select();
            }
        }

        private void ApplyHover(List<Vector3> palms)
        {
            if (palms.Count >= 2)
            {
                var mid = (palms[0] + palms[1]) * 0.5f;
                transform.position = Vector3.Lerp(transform.position, mid, Time.deltaTime * followLerp);
                var dist = Vector3.Distance(palms[0], palms[1]);
                var scale = m_baseScale * Mathf.Clamp(dist / 0.35f, 0.7f, 2.4f);
                transform.localScale = Vector3.Lerp(transform.localScale, scale, Time.deltaTime * 8f);
            }
            else if (palms.Count == 1)
            {
                var target = palms[0] + Vector3.up * hoverHeight;
                transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * followLerp);
            }
        }

        private List<Vector3> CollectHoverPalms()
        {
            var palms = new List<Vector3>(2);
            var attract = GetRadius() * 2.8f;
            var hands = FindObjectsByType<OVRHand>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var hand in hands)
            {
                if (!hand || !hand.IsTracked)
                {
                    continue;
                }

                var pose = hand.PointerPose ? hand.PointerPose : hand.transform;
                if (Vector3.Distance(pose.position, transform.position) < attract)
                {
                    palms.Add(pose.position);
                }
            }

            if (palms.Count >= 2 || !m_rig)
            {
                return palms;
            }

            TryAddControllerPalm(OVRInput.Controller.LTouch, attract, palms);
            TryAddControllerPalm(OVRInput.Controller.RTouch, attract, palms);
            return palms;
        }

        private void TryAddControllerPalm(OVRInput.Controller controller, float attract, List<Vector3> palms)
        {
            if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, controller))
            {
                return;
            }

            var pos = ControllerAim.GetPosition(controller, m_rig);
            if (Vector3.Distance(pos, transform.position) < attract)
            {
                palms.Add(pos);
            }
        }

        private int CountNearbyPinches()
        {
            var count = 0;
            var radius = GetRadius() * 2.2f;
            var hands = FindObjectsByType<OVRHand>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var hand in hands)
            {
                if (!hand || !hand.IsTracked || !hand.GetFingerIsPinching(OVRHand.HandFinger.Index))
                {
                    continue;
                }

                var pose = hand.PointerPose ? hand.PointerPose : hand.transform;
                if (Vector3.Distance(pose.position, transform.position) <= radius)
                {
                    count++;
                }
            }

            if (count >= 2 || !m_rig)
            {
                return count;
            }

            if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch)
                && Vector3.Distance(ControllerAim.GetPosition(OVRInput.Controller.LTouch, m_rig), transform.position) <= radius)
            {
                count++;
            }

            if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch)
                && Vector3.Distance(ControllerAim.GetPosition(OVRInput.Controller.RTouch, m_rig), transform.position) <= radius)
            {
                count++;
            }

            return count;
        }

        private bool TryBeginGripHold()
        {
            Transform best = null;
            var bestDist = float.MaxValue;
            var radius = GetRadius() * 1.6f;

            var hands = FindObjectsByType<OVRHand>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var hand in hands)
            {
                if (!hand || !hand.IsTracked || !IsFist(hand))
                {
                    continue;
                }

                var pose = hand.PointerPose ? hand.PointerPose : hand.transform;
                if (!PlayGrabSources.IsFree(pose))
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

            TryGripController(OVRInput.Controller.LTouch, radius, ref best, ref bestDist);
            TryGripController(OVRInput.Controller.RTouch, radius, ref best, ref bestDist);

            if (!best)
            {
                return false;
            }

            m_grabSource = best;
            PlayGrabSources.Claim(best);
            m_localPos = Quaternion.Inverse(best.rotation) * (transform.position - best.position);
            m_localRot = Quaternion.Inverse(best.rotation) * transform.rotation;
            m_mode = Mode.Held;
            SetPhysics(false);
            ResetMotionSamples();
            return true;
        }

        private void TryGripController(OVRInput.Controller controller, float radius, ref Transform best, ref float bestDist)
        {
            if (!m_rig || !OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, controller))
            {
                return;
            }

            var pose = controller == OVRInput.Controller.LTouch ? m_rig.leftControllerAnchor : m_rig.rightControllerAnchor;
            if (!PlayGrabSources.IsFree(pose))
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

        private bool StillHolding(Transform source)
        {
            if (!source)
            {
                return false;
            }

            var hands = FindObjectsByType<OVRHand>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var hand in hands)
            {
                if (!hand || !hand.IsTracked)
                {
                    continue;
                }

                var pose = hand.PointerPose ? hand.PointerPose : hand.transform;
                if (pose == source)
                {
                    return IsFist(hand);
                }
            }

            if (m_rig && source == m_rig.leftControllerAnchor)
            {
                return OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);
            }

            if (m_rig && source == m_rig.rightControllerAnchor)
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

        private void EnterAttached()
        {
            PlayGrabSources.Release(m_grabSource);
            m_grabSource = null;
            m_mode = Mode.Attached;
            SetPhysics(false);
            ResetMotionSamples();
        }

        private void EnterReleased(bool throwNow)
        {
            PlayGrabSources.Release(m_grabSource);
            m_grabSource = null;
            m_mode = Mode.Released;
            SetPhysics(true);
            if (throwNow && m_body)
            {
                m_body.linearVelocity = Average(m_velSamples) * throwBoost;
                m_body.angularVelocity = Average(m_angSamples);
            }
        }

        private void SetPhysics(bool thrown)
        {
            if (!m_body)
            {
                return;
            }

            m_body.isKinematic = !thrown;
            m_body.useGravity = thrown;
        }

        private void ResetMotionSamples()
        {
            m_hasLast = false;
            m_sampleIndex = 0;
            for (var i = 0; i < m_velSamples.Length; i++)
            {
                m_velSamples[i] = Vector3.zero;
                m_angSamples[i] = Vector3.zero;
            }
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

        private void UpdateFistBeam()
        {
            if (!m_beam)
            {
                return;
            }

            if (!TryGetFistDirection(out var direction))
            {
                m_beam.enabled = false;
                return;
            }

            m_beam.enabled = true;
            var start = transform.position;
            m_beam.SetPosition(0, start);
            m_beam.SetPosition(1, start + direction * 2.8f);
        }

        private bool TryGetFistDirection(out Vector3 direction)
        {
            direction = Vector3.forward;
            var hands = FindObjectsByType<OVRHand>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var hand in hands)
            {
                if (!hand || !hand.IsTracked || !IsFist(hand))
                {
                    continue;
                }

                var pose = hand.PointerPose ? hand.PointerPose : hand.transform;
                direction = pose.forward;
                if (direction.sqrMagnitude < 0.0001f)
                {
                    direction = transform.forward;
                }

                return true;
            }

            if (!m_rig)
            {
                m_rig = FindFirstObjectByType<OVRCameraRig>();
            }

            if (!m_rig)
            {
                return false;
            }

            foreach (var controller in new[] { OVRInput.Controller.RTouch, OVRInput.Controller.LTouch })
            {
                var grip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controller) > 0.85f;
                var trigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controller) > 0.85f;
                if (!grip || !trigger)
                {
                    continue;
                }

                direction = ControllerAim.GetRotation(controller, m_rig) * Vector3.forward;
                return direction.sqrMagnitude > 0.0001f;
            }

            return false;
        }

        private static bool IsFist(OVRHand hand)
        {
            return hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) > 0.65f
                   && hand.GetFingerPinchStrength(OVRHand.HandFinger.Middle) > 0.65f
                   && hand.GetFingerPinchStrength(OVRHand.HandFinger.Ring) > 0.65f
                   && hand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky) > 0.55f;
        }

        private LineRenderer BuildBeam()
        {
            var go = new GameObject("FistBeam");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.enabled = false;
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = 0.012f;
            line.endWidth = 0.002f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            var shader = Shader.Find("Mobile/Particles/Additive") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            if (shader)
            {
                line.material = new Material(shader) { color = new Color(0.3f, 0.95f, 1f, 1f) };
            }

            line.startColor = new Color(0.35f, 1f, 1f, 1f);
            line.endColor = new Color(1f, 0.4f, 1f, 0.15f);
            return line;
        }

        private float GetRadius()
        {
            var col = GetComponentInChildren<Collider>();
            return col ? Mathf.Max(grabRadius, col.bounds.extents.magnitude * 0.9f) : grabRadius;
        }

        private void SetFx(float rate)
        {
            if (!m_fx)
            {
                return;
            }

            var emission = m_fx.emission;
            emission.rateOverTime = rate;
            if (rate > 1f && !m_fx.isPlaying)
            {
                m_fx.Play();
            }
        }

        private TrailRenderer BuildTrail()
        {
            var trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.2f;
            trail.minVertexDistance = 0.01f;
            trail.widthMultiplier = 0.012f;
            trail.emitting = false;
            var shader = Shader.Find("Mobile/Particles/Additive") ?? Shader.Find("Sprites/Default");
            if (shader)
            {
                trail.material = new Material(shader);
            }

            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.3f, 0.95f, 1f), 0f),
                    new GradientColorKey(new Color(1f, 0.4f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            trail.colorGradient = gradient;
            return trail;
        }

        private ParticleSystem BuildSparkles()
        {
            var go = new GameObject("CubeSparkles");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 0.55f;
            main.startSpeed = 0.4f;
            main.startSize = 0.018f;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.35f, 0.95f, 1f, 1f),
                new Color(1f, 0.45f, 1f, 1f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;
            main.gravityModifier = -0.08f;

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.cyan, 0f),
                    new GradientColorKey(new Color(1f, 0.5f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Mobile/Particles/Additive") ?? Shader.Find("Sprites/Default");
            if (shader)
            {
                renderer.material = new Material(shader);
            }

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }
    }
}
