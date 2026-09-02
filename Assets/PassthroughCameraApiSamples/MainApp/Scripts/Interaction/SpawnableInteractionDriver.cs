// Copyright (c) Meta Platforms, Inc. and affiliates.

using PassthroughCameraSamples.StartScene;
using PassthroughCameraSamples.MainApp;
using PassthroughCameraSamples.MainApp.TextTools;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MainApp.Interaction
{
    /// <summary>
    /// StartScene 카메라 리그에는 Interaction SDK Ray/Grab Interactor가 없습니다.
    /// 컨트롤러 레이 + 트리거/그립으로 생성 오브젝트를 선택·이동·스케일합니다.
    /// </summary>
    [DefaultExecutionOrder(300)]
    public class SpawnableInteractionDriver : MonoBehaviour
    {
        public static SpawnableInteractionDriver Instance { get; private set; }

        [SerializeField] private float maxRayDistance = 8f;
        [SerializeField] private float handGrabRadius = 0.09f;

        private OVRCameraRig m_rig;
        private SpawnableItem m_selected;
        private bool m_dragging;
        private OVRInput.Controller m_dragController = OVRInput.Controller.RTouch;
        private Transform m_handGrab;
        private Vector3 m_grabLocalPos;
        private Quaternion m_grabLocalRot;
        private SpawnableItem m_scaleItem;
        private Vector3 m_scaleStart;
        private Vector3 m_scaleStartPos;
        private Vector3 m_scalePivotWorld;
        private OVRInput.Controller m_scaleController = OVRInput.Controller.RTouch;
        private float m_twoHandStartDistance;
        private Vector3 m_twoHandStartScale;

        private float m_nextUiClickTime;

        public SpawnableItem Selected => m_selected;
        public bool IsMoving(SpawnableItem item) => m_dragging && m_selected == item;

        private void Awake()
        {
            Instance = this;
            m_rig = FindFirstObjectByType<OVRCameraRig>();
        }

        private void Update()
        {
            if (!m_rig)
            {
                m_rig = FindFirstObjectByType<OVRCameraRig>();
                if (!m_rig)
                {
                    return;
                }
            }

            if (TextConfigPanel.Instance && TextConfigPanel.Instance.IsOpen
                && OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            {
                TextConfigPanel.Instance.Close();
                return;
            }

            if (m_selected && OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            {
                m_scaleItem = null;
                m_selected.Deselect();
                return;
            }

            HandlePullScale();
            HandleTwoHandScale();
            HandleHandGrab();
            HandleDrag();
        }

        private void LateUpdate()
        {
            if (!m_rig)
            {
                return;
            }

            HandleSelect();
        }

        public void BeginPullScale(SpawnableItem item)
        {
            if (!item)
            {
                return;
            }

            m_scaleItem = item;
            m_scaleStart = item.transform.localScale;
            m_scaleController = OVRInput.Controller.RTouch;
            m_scaleStartPos = ControllerAim.GetPosition(m_scaleController, m_rig);
            var col = item.GetComponentInChildren<Collider>();
            m_scalePivotWorld = col
                ? new Vector3(col.bounds.center.x, col.bounds.max.y, col.bounds.center.z)
                : item.transform.position;
        }

        private void HandlePullScale()
        {
            if (!m_scaleItem)
            {
                return;
            }

            var held = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, m_scaleController)
                       || OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, m_scaleController);
            if (!held)
            {
                m_scaleItem = null;
                return;
            }

            var pos = ControllerAim.GetPosition(m_scaleController, m_rig);
            var deltaDown = m_scaleStartPos.y - pos.y;
            var ratio = Mathf.Clamp(1f + deltaDown * 3.5f, 0.25f, 4f);
            m_scaleItem.transform.localScale = m_scaleStart * ratio;

            var col = m_scaleItem.GetComponentInChildren<Collider>();
            if (col)
            {
                m_scaleItem.transform.position += Vector3.up * (m_scalePivotWorld.y - col.bounds.max.y);
            }

            m_scaleItem.RefreshOutline();
        }

        private void HandleSelect()
        {
            if (!OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch)
                && !OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
            {
                return;
            }

            if (Time.unscaledTime < m_nextUiClickTime)
            {
                return;
            }

            var controller = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch)
                ? OVRInput.Controller.LTouch
                : OVRInput.Controller.RTouch;

            var uiButton = FindUiButtonUnderPointer();
            if (uiButton)
            {
                m_nextUiClickTime = Time.unscaledTime + 0.35f;
                uiButton.onClick.Invoke();
                return;
            }

            var phys = FindPhysicsClickableUnderPointer();
            if (phys)
            {
                m_nextUiClickTime = Time.unscaledTime + 0.35f;
                phys.Click();
                return;
            }

            var ray = GetPointerRay(controller);
            var hits = Physics.RaycastAll(ray, maxRayDistance);
            if (hits.Length == 0)
            {
                return;
            }

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponent<Button>() || hit.collider.GetComponent<PhysicsClickable>())
                {
                    continue;
                }

                if (hit.collider.GetComponentInParent<Canvas>() && !hit.collider.GetComponentInParent<SpawnableItem>())
                {
                    return;
                }

                var item = hit.collider.GetComponentInParent<SpawnableItem>();
                if (item)
                {
                    SelectItem(item);
                    return;
                }
            }
        }

        private Ray GetPointerRay(OVRInput.Controller controller)
        {
            var laser = FindFirstObjectByType<LaserPointer>(FindObjectsInactive.Include);
            if (laser)
            {
                var line = laser.GetComponent<LineRenderer>();
                if (line && line.positionCount >= 2)
                {
                var start = line.useWorldSpace ? line.GetPosition(0) : line.transform.TransformPoint(line.GetPosition(0));
                var end = line.useWorldSpace ? line.GetPosition(1) : line.transform.TransformPoint(line.GetPosition(1));
                    var dir = end - start;
                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        return new Ray(start, dir.normalized);
                    }
                }
            }

            return ControllerAim.GetRay(controller, m_rig);
        }

        private Button FindUiButtonUnderPointer()
        {
            Button best = null;
            var bestDist = float.MaxValue;

            var laser = FindFirstObjectByType<LaserPointer>(FindObjectsInactive.Include);
            if (laser && laser.CursorVisual && laser.CursorVisual.activeInHierarchy)
            {
                var cursor = laser.CursorVisual.transform.position;
                var overlaps = Physics.OverlapSphere(cursor, 0.08f);
                foreach (var col in overlaps)
                {
                    var button = col.GetComponent<Button>() ?? col.GetComponentInParent<Button>();
                    if (!button || !button.interactable || !button.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    var dist = Vector3.Distance(cursor, col.ClosestPoint(cursor));
                    if (dist < bestDist)
                    {
                        best = button;
                        bestDist = dist;
                    }
                }

                if (best)
                {
                    return best;
                }
            }

            ConsiderButtonsOnRay(GetPointerRay(OVRInput.Controller.RTouch), ref best, ref bestDist);
            ConsiderButtonsOnRay(ControllerAim.GetRay(OVRInput.Controller.RTouch, m_rig), ref best, ref bestDist);
            ConsiderButtonsOnRay(ControllerAim.GetRay(OVRInput.Controller.LTouch, m_rig), ref best, ref bestDist);
            return best;
        }

        private PhysicsClickable FindPhysicsClickableUnderPointer()
        {
            PhysicsClickable best = null;
            var bestDist = float.MaxValue;

            var laser = FindFirstObjectByType<LaserPointer>(FindObjectsInactive.Include);
            if (laser && laser.CursorVisual && laser.CursorVisual.activeInHierarchy)
            {
                var cursor = laser.CursorVisual.transform.position;
                var overlaps = Physics.OverlapSphere(cursor, 0.08f);
                foreach (var col in overlaps)
                {
                    var clickable = col.GetComponent<PhysicsClickable>() ?? col.GetComponentInParent<PhysicsClickable>();
                    if (!clickable || !clickable.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    var dist = Vector3.Distance(cursor, col.ClosestPoint(cursor));
                    if (dist < bestDist)
                    {
                        best = clickable;
                        bestDist = dist;
                    }
                }

                if (best)
                {
                    return best;
                }
            }

            ConsiderClickableOnRay(GetPointerRay(OVRInput.Controller.RTouch), ref best, ref bestDist);
            ConsiderClickableOnRay(ControllerAim.GetRay(OVRInput.Controller.RTouch, m_rig), ref best, ref bestDist);
            ConsiderClickableOnRay(ControllerAim.GetRay(OVRInput.Controller.LTouch, m_rig), ref best, ref bestDist);
            return best;
        }

        private void ConsiderClickableOnRay(Ray ray, ref PhysicsClickable best, ref float bestDist)
        {
            var hits = Physics.RaycastAll(ray, maxRayDistance);
            foreach (var hit in hits)
            {
                var clickable = hit.collider.GetComponent<PhysicsClickable>();
                if (!clickable || !clickable.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (hit.distance < bestDist)
                {
                    best = clickable;
                    bestDist = hit.distance;
                }
            }
        }

        private void ConsiderButtonsOnRay(Ray ray, ref Button best, ref float bestDist)
        {
            var hits = Physics.RaycastAll(ray, maxRayDistance);
            foreach (var hit in hits)
            {
                var button = hit.collider.GetComponent<Button>();
                if (!button || !button.interactable || !button.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (hit.distance < bestDist)
                {
                    best = button;
                    bestDist = hit.distance;
                }
            }
        }

        private SpawnableItem RaycastItem(OVRInput.Controller controller)
        {
            var ray = ControllerAim.GetRay(controller, m_rig);
            if (!Physics.Raycast(ray, out var hit, maxRayDistance))
            {
                return null;
            }

            if (hit.collider.GetComponentInParent<Button>())
            {
                return null;
            }

            if (hit.collider.GetComponentInParent<Canvas>() && !hit.collider.GetComponentInParent<SpawnableItem>())
            {
                return null;
            }

            return hit.collider.GetComponentInParent<SpawnableItem>();
        }

        private void SelectItem(SpawnableItem item)
        {
            if (m_selected && m_selected != item)
            {
                m_selected.Deselect();
            }

            item.Select();
            m_selected = item;
        }

        public void BindSelection(SpawnableItem item)
        {
            if (m_selected && m_selected != item)
            {
                m_selected.Deselect();
            }

            m_selected = item;
        }

        public void ClearSelection(SpawnableItem item)
        {
            if (m_selected == item)
            {
                m_dragging = false;
                m_handGrab = null;
                m_selected = null;
            }
        }

        private void HandleDrag()
        {
            if (m_selected && m_selected.GetComponent<CubeHoverToy>())
            {
                m_dragging = false;
                return;
            }
            if (!m_selected || !m_selected.MoveRotateEnabled || m_handGrab)
            {
                if (!m_handGrab)
                {
                    m_dragging = false;
                }

                return;
            }

            var left = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);
            var right = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);
            if (left && right)
            {
                m_dragging = false;
                return;
            }

            if (!m_dragging)
            {
                if (right && RaycastItem(OVRInput.Controller.RTouch) == m_selected)
                {
                    BeginDrag(OVRInput.Controller.RTouch);
                }
                else if (left && RaycastItem(OVRInput.Controller.LTouch) == m_selected)
                {
                    BeginDrag(OVRInput.Controller.LTouch);
                }
                else if (right)
                {
                    BeginDrag(OVRInput.Controller.RTouch);
                }
                else if (left)
                {
                    BeginDrag(OVRInput.Controller.LTouch);
                }

                return;
            }

            var held = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, m_dragController);
            if (!held)
            {
                m_dragging = false;
                return;
            }

            ApplyPose(ControllerAim.GetPosition(m_dragController, m_rig), ControllerAim.GetRotation(m_dragController, m_rig));
        }

        private void BeginDrag(OVRInput.Controller controller)
        {
            m_dragging = true;
            m_dragController = controller;
            var controllerPos = ControllerAim.GetPosition(controller, m_rig);
            var controllerRot = ControllerAim.GetRotation(controller, m_rig);
            m_grabLocalPos = Quaternion.Inverse(controllerRot) * (m_selected.transform.position - controllerPos);
            m_grabLocalRot = Quaternion.Inverse(controllerRot) * m_selected.transform.rotation;
        }

        private void ApplyPose(Vector3 worldPos, Quaternion worldRot)
        {
            m_selected.transform.position = worldPos + worldRot * m_grabLocalPos;
            m_selected.transform.rotation = worldRot * m_grabLocalRot;
        }

        private void HandleHandGrab()
        {
            if (m_selected && m_selected.GetComponent<CubeHoverToy>())
            {
                m_handGrab = null;
                return;
            }
            if (!m_selected || !m_selected.MoveRotateEnabled)
            {
                m_handGrab = null;
                return;
            }

            var hands = FindObjectsByType<OVRHand>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Transform pinchPose = null;
            foreach (var hand in hands)
            {
                if (!hand || !hand.IsTracked || !hand.GetFingerIsPinching(OVRHand.HandFinger.Index))
                {
                    continue;
                }

                var pose = hand.PointerPose;
                if (!pose)
                {
                    continue;
                }

                if (Vector3.Distance(pose.position, m_selected.transform.position) <= GetGrabRadius(m_selected))
                {
                    pinchPose = pose;
                    break;
                }
            }

            if (!pinchPose)
            {
                m_handGrab = null;
                return;
            }

            if (m_handGrab != pinchPose)
            {
                m_handGrab = pinchPose;
                m_dragging = true;
                m_grabLocalPos = Quaternion.Inverse(pinchPose.rotation) * (m_selected.transform.position - pinchPose.position);
                m_grabLocalRot = Quaternion.Inverse(pinchPose.rotation) * m_selected.transform.rotation;
            }

            ApplyPose(pinchPose.position, pinchPose.rotation);
        }

        private float GetGrabRadius(SpawnableItem item)
        {
            var col = item.GetComponentInChildren<Collider>();
            if (!col)
            {
                return handGrabRadius;
            }

            return Mathf.Max(handGrabRadius, col.bounds.extents.magnitude * 0.65f);
        }

        private void HandleTwoHandScale()
        {
            if (!m_selected || !m_selected.MoveRotateEnabled || m_selected.GetComponent<CubeHoverToy>())
            {
                return;
            }

            var leftGrip = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);
            var rightGrip = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);
            if (!leftGrip || !rightGrip)
            {
                m_twoHandStartDistance = 0f;
                return;
            }

            var left = ControllerAim.GetPosition(OVRInput.Controller.LTouch, m_rig);
            var right = ControllerAim.GetPosition(OVRInput.Controller.RTouch, m_rig);
            var distance = Vector3.Distance(left, right);
            if (distance < 0.01f)
            {
                return;
            }

            if (m_twoHandStartDistance < 0.01f)
            {
                m_twoHandStartDistance = distance;
                m_twoHandStartScale = m_selected.transform.localScale;
                return;
            }

            var ratio = Mathf.Clamp(distance / m_twoHandStartDistance, 0.25f, 4f);
            m_selected.transform.localScale = m_twoHandStartScale * ratio;
        }
    }
}
