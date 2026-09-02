// Copyright (c) Meta Platforms, Inc. and affiliates.

using PassthroughCameraSamples.MainApp.Interaction;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Draw
{
    /// <summary>
    /// Simplified local replacement for MRMotif's PointerDrawingMotif +
    /// RaycastDrawingMotif pair: a single controller-raycast pen. Enabled/disabled by
    /// SpawnableItem's "수정" (edit) mini-menu button. While enabled and the right
    /// controller trigger is held, raycasts from the controller onto this board and
    /// paints a line as it moves.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WhiteboardDrawingLocal : MonoBehaviour
    {
        [SerializeField] private WhiteboardManagerLocal whiteboard;
        [SerializeField] private Color penColor = new(0.1f, 0.3f, 0.9f, 1f);
        [SerializeField] private int brushRadius = 6;
        [SerializeField] private float maxDrawDistance = 2f;

        public bool DrawingEnabled { get; set; }

        private OVRCameraRig m_cameraRig;
        private bool m_isDrawing;
        private Vector2 m_lastUv;

        private void Awake()
        {
            m_cameraRig = FindFirstObjectByType<OVRCameraRig>();
        }

        /// <summary>Assigns the target board at runtime for code-spawned setups.</summary>
        public void SetWhiteboard(WhiteboardManagerLocal target)
        {
            whiteboard = target;
        }

        private void Update()
        {
            if (!DrawingEnabled || !whiteboard)
            {
                m_isDrawing = false;
                return;
            }

            var controller = OVRInput.Controller.RTouch;
            var pressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, controller);
            if (!pressed)
            {
                m_isDrawing = false;
                return;
            }

            var ray = m_cameraRig
                ? ControllerAim.GetRay(controller, m_cameraRig)
                : new Ray(OVRInput.GetLocalControllerPosition(controller),
                    OVRInput.GetLocalControllerRotation(controller) * Vector3.forward);

            if (Physics.Raycast(ray, out var hit, maxDrawDistance)
                && hit.collider.GetComponentInParent<WhiteboardDrawingLocal>() == this)
            {
                var uv = whiteboard.WorldToUv(hit.point);
                if (!m_isDrawing)
                {
                    m_lastUv = uv;
                }

                whiteboard.DrawLine(m_lastUv, uv, penColor, brushRadius);
                m_lastUv = uv;
                m_isDrawing = true;
            }
            else
            {
                m_isDrawing = false;
            }
        }
    }
}
