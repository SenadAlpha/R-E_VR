// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using PassthroughCameraSamples.StartScene;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MainApp.Interaction
{
    public class ItemMiniMenu : MonoBehaviour
    {
        [SerializeField] private Button sizeButton;
        [SerializeField] private Button colorButton;
        [SerializeField] private Button editButton;
        [SerializeField] private Button warpButton;
        [SerializeField] private Button trashButton;

        private SpawnableItem m_target;
        private float m_menuWorldHeight = 0.12f;

        public void Initialize(SpawnableItem target)
        {
            m_target = target;

            var raycaster = GetComponent<OVRRaycaster>();
            var laserPointer = FindFirstObjectByType<LaserPointer>(FindObjectsInactive.Include);
            if (raycaster && laserPointer)
            {
                raycaster.pointer = laserPointer.gameObject;
            }

            ApplyFont(sizeButton);
            ApplyFont(colorButton);
            ApplyFont(editButton);
            ApplyFont(warpButton);
            ApplyFont(trashButton);

            if (colorButton)
            {
                colorButton.gameObject.SetActive(target.SupportsColor);
            }

            if (editButton)
            {
                editButton.gameObject.SetActive(target.SupportsEdit);
            }

            if (warpButton)
            {
                warpButton.gameObject.SetActive(target.SupportsWarp);
            }

            PackButtons();

            if (sizeButton)
            {
                sizeButton.onClick.AddListener(() =>
                {
                    if (SpawnableInteractionDriver.Instance)
                    {
                        SpawnableInteractionDriver.Instance.BeginPullScale(target);
                    }
                });
            }

            if (colorButton)
            {
                colorButton.onClick.AddListener(() => target.RequestColor());
            }

            if (editButton)
            {
                editButton.onClick.AddListener(() => target.RequestEdit());
            }

            if (warpButton)
            {
                warpButton.onClick.AddListener(() => target.RequestWarp());
            }

            if (trashButton)
            {
                trashButton.onClick.AddListener(() => target.DeleteSelf());
            }
        }

        private void PackButtons()
        {
            var visible = new List<Button>();
            if (sizeButton && sizeButton.gameObject.activeSelf) visible.Add(sizeButton);
            if (colorButton && colorButton.gameObject.activeSelf) visible.Add(colorButton);
            if (editButton && editButton.gameObject.activeSelf) visible.Add(editButton);
            if (warpButton && warpButton.gameObject.activeSelf) visible.Add(warpButton);
            if (trashButton && trashButton.gameObject.activeSelf) visible.Add(trashButton);

            const float width = 150f;
            const float height = 48f;
            const float gap = 6f;
            const float pad = 10f;
            var panelHeight = visible.Count * height + Mathf.Max(0, visible.Count - 1) * gap + pad * 2f;
            var panelWidth = width + pad * 2f;

            var rt = GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(panelWidth, panelHeight);
            m_menuWorldHeight = panelHeight * rt.lossyScale.y;

            var box = GetComponent<BoxCollider>();
            if (box)
            {
                box.size = new Vector3(panelWidth, panelHeight, 16f);
                box.center = Vector3.zero;
            }

            var y = panelHeight * 0.5f - pad - height * 0.5f;
            foreach (var button in visible)
            {
                var buttonRt = button.GetComponent<RectTransform>();
                buttonRt.anchoredPosition = new Vector2(0f, y);
                buttonRt.sizeDelta = new Vector2(width, height);
                var col = button.GetComponent<BoxCollider>();
                if (col)
                {
                    col.size = new Vector3(width, height, 40f);
                    col.center = Vector3.zero;
                }

                y -= height + gap;
            }
        }

        private static void ApplyFont(Button button)
        {
            if (!button)
            {
                return;
            }

            MrFonts.Apply(button.GetComponentInChildren<Text>(true));
        }

        private void LateUpdate()
        {
            if (!m_target)
            {
                Destroy(gameObject);
                return;
            }

            var rt = GetComponent<RectTransform>();
            if (rt)
            {
                m_menuWorldHeight = rt.sizeDelta.y * Mathf.Abs(rt.lossyScale.y);
            }

            var col = m_target.GetComponentInChildren<Collider>();
            var renderer = m_target.GetComponentInChildren<Renderer>();
            var center = renderer ? renderer.bounds.center : (col ? col.bounds.center : m_target.transform.position);
            var bottom = renderer ? renderer.bounds.min.y : (col ? col.bounds.min.y : center.y - 0.1f);
            const float gap = 0.05f;
            var pos = new Vector3(center.x, bottom - m_menuWorldHeight * 0.5f - gap, center.z);

            var cam = Camera.main;
            if (cam)
            {
                var toCam = cam.transform.position - pos;
                if (toCam.sqrMagnitude > 0.0001f)
                {
                    pos += toCam.normalized * 0.025f;
                    transform.position = pos;
                    transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
                    return;
                }
            }

            transform.position = pos;
        }
    }
}
