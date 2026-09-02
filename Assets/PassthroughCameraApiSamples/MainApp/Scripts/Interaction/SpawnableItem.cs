// Copyright (c) Meta Platforms, Inc. and affiliates.

using PassthroughCameraSamples.MainApp;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MainApp.Interaction
{
    public enum SpawnableItemType
    {
        Text,
        Photo,
        Paper,
        Draw,
        Cube,
        Document
    }

    /// <summary>
    /// 메뉴에서 생성된 오브젝트. StartScene에는 Interaction SDK 인터랙터가 없으므로
    /// <see cref="SpawnableInteractionDriver"/>가 컨트롤러 레이로 선택/이동합니다.
    /// </summary>
    public class SpawnableItem : MonoBehaviour
    {
        [SerializeField] private SpawnableItemType itemType;
        [SerializeField] private ItemMiniMenu miniMenuPrefab;
        [SerializeField] private Color outlineColor = new(0f, 1f, 1f, 1f);
        [SerializeField] private float outlineWidth = 0.008f;
        [SerializeField] private bool enableWallSnap = true;

        public SpawnableItemType ItemType => itemType;
        public bool SupportsEdit => itemType is SpawnableItemType.Text or SpawnableItemType.Draw;
        public bool SupportsColor => itemType is SpawnableItemType.Text;
        public bool SupportsWarp => itemType is SpawnableItemType.Document;
        public bool MoveRotateEnabled { get; private set; }
        public bool IsSelected { get; private set; }
        public UnityEvent OnEditRequested = new();
        public UnityEvent OnColorRequested = new();

        private AquaOutline m_outline;
        private ItemMiniMenu m_activeMiniMenu;

        public void Initialize(SpawnableItemType type)
        {
            itemType = type;
        }

        public void ConfigureMiniMenu(ItemMiniMenu prefab)
        {
            miniMenuPrefab = prefab;
        }

        private void Awake()
        {
            EnsureCollider();

            var rb = GetComponent<Rigidbody>();
            if (!rb)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            rb.isKinematic = true;
            rb.useGravity = false;

            if (enableWallSnap && !GetComponent<WallSnapBehaviour>())
            {
                gameObject.AddComponent<WallSnapBehaviour>();
            }

            m_outline = gameObject.AddComponent<AquaOutline>();
            m_outline.Configure(outlineColor, outlineWidth);
            m_outline.SetVisible(false);
        }

        private void EnsureCollider()
        {
            foreach (var meshCollider in GetComponentsInChildren<MeshCollider>(true))
            {
                meshCollider.enabled = false;
            }

            foreach (var childBox in GetComponentsInChildren<BoxCollider>(true))
            {
                if (childBox.gameObject == gameObject)
                {
                    continue;
                }

                if (childBox.GetComponent<Button>()
                    || childBox.GetComponent<Canvas>()
                    || childBox.GetComponent<PhysicsClickable>())
                {
                    continue;
                }

                childBox.enabled = false;
            }

            var box = GetComponent<BoxCollider>();
            if (!box)
            {
                box = gameObject.AddComponent<BoxCollider>();
            }

            box.enabled = true;
            box.center = Vector3.zero;

            if (GetComponent<PassthroughCameraSamples.MainApp.TextTools.WorldTextNote>())
            {
                return;
            }

            var canvas = GetComponentInChildren<Canvas>();
            if (canvas)
            {
                var rt = canvas.GetComponent<RectTransform>();
                var lossy = canvas.transform.lossyScale;
                var rootLossy = transform.lossyScale;
                box.size = new Vector3(
                    Mathf.Abs(rt.sizeDelta.x * lossy.x / Mathf.Max(0.0001f, rootLossy.x)),
                    Mathf.Abs(rt.sizeDelta.y * lossy.y / Mathf.Max(0.0001f, rootLossy.y)),
                    0.04f);
                return;
            }

            var renderer = GetComponent<MeshRenderer>() ?? GetComponentInChildren<MeshRenderer>();
            if (renderer && renderer.gameObject == gameObject)
            {
                var uniform = Mathf.Abs(transform.localScale.x - transform.localScale.z) < 0.02f
                              && Mathf.Abs(transform.localScale.y - transform.localScale.z) < 0.02f;
                box.size = uniform ? Vector3.one : new Vector3(1f, 1f, 0.04f);
                return;
            }

            box.size = new Vector3(0.3f, 0.18f, 0.04f);
        }

        public void RefreshOutline()
        {
            if (m_outline && IsSelected)
            {
                m_outline.SetVisible(true);
            }
        }

        public void NudgeScale()
        {
            var next = transform.localScale * 1.2f;
            var max = Mathf.Max(next.x, next.y, next.z);
            if (max > 3.5f)
            {
                next *= 0.4f;
            }

            transform.localScale = next;
            RefreshOutline();
        }

        public void SetMiniMenuVisible(bool visible)
        {
            if (m_activeMiniMenu)
            {
                m_activeMiniMenu.gameObject.SetActive(visible);
            }
        }

        public void SetHitboxEnabled(bool enabled)
        {
            var box = GetComponent<BoxCollider>();
            if (box)
            {
                box.enabled = enabled;
            }
        }

        public void RequestColor()
        {
            if (SupportsColor)
            {
                OnColorRequested?.Invoke();
            }
        }

        public void Select()
        {
            if (IsSelected)
            {
                return;
            }

            IsSelected = true;
            MoveRotateEnabled = true;
            m_outline.SetVisible(true);
            OpenMiniMenu();
            MrPointer.Show();
            if (SpawnableInteractionDriver.Instance)
            {
                SpawnableInteractionDriver.Instance.BindSelection(this);
            }
        }

        public void Deselect()
        {
            if (!IsSelected)
            {
                return;
            }

            IsSelected = false;
            MoveRotateEnabled = false;
            m_outline.SetVisible(false);
            CloseMiniMenu();
            SpawnableInteractionDriver.Instance?.ClearSelection(this);
        }

        public void ToggleMoveRotate()
        {
            MoveRotateEnabled = !MoveRotateEnabled;
        }

        public void RequestWarp()
        {
            if (!SupportsWarp)
            {
                return;
            }

            var surface = GetComponent<PassthroughCameraSamples.MainApp.Admin.DocumentSurface>();
            surface?.ToggleCurve();
        }

        public void RequestEdit()
        {
            if (SupportsEdit)
            {
                OnEditRequested?.Invoke();
                MrPointer.Show();
            }
        }

        public void DeleteSelf()
        {
            CloseMiniMenu();
            SpawnableInteractionDriver.Instance?.ClearSelection(this);
            Destroy(gameObject);
        }

        private void OpenMiniMenu()
        {
            CloseMiniMenu();
            if (!miniMenuPrefab)
            {
                Debug.LogWarning("[SpawnableItem] No mini-menu prefab assigned.", this);
                return;
            }

            m_activeMiniMenu = Instantiate(miniMenuPrefab);
            m_activeMiniMenu.gameObject.SetActive(true);
            m_activeMiniMenu.Initialize(this);
        }

        private void CloseMiniMenu()
        {
            if (m_activeMiniMenu)
            {
                Destroy(m_activeMiniMenu.gameObject);
            }

            m_activeMiniMenu = null;
        }

        private void OnDestroy()
        {
            CloseMiniMenu();
        }
    }
}
