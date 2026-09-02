// Copyright (c) Meta Platforms, Inc. and affiliates.

using PassthroughCameraSamples.MainApp;
using PassthroughCameraSamples.MainApp.Interaction;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp.TextTools
{
    public class TextToolSpawner : MonoBehaviour
    {
        [SerializeField] private OVRCameraRig cameraRig;
        [SerializeField] private TextConfigPanel configPanel;
        [SerializeField] private ItemMiniMenu miniMenuPrefab;
        [SerializeField] private float spawnDistance = 0.82f;
        [SerializeField] private float defaultCharacterSize = 0.04f;

        private void Awake()
        {
            if (!cameraRig)
            {
                cameraRig = FindFirstObjectByType<OVRCameraRig>();
            }
        }

        public void SpawnTextItem()
        {
            if (!cameraRig)
            {
                Debug.LogError("[TextToolSpawner] No OVRCameraRig found.");
                return;
            }

            var head = cameraRig.centerEyeAnchor ? cameraRig.centerEyeAnchor : Camera.main?.transform;
            if (!head)
            {
                return;
            }

            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = head.forward;
            }

            var root = new GameObject("TextItem");
            root.transform.SetParent(MainAppPlacement.SpawnRoot(), true);
            root.transform.position = head.position + forward * spawnDistance - Vector3.up * 0.1f;
            root.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);

            var background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            background.name = "Background";
            background.transform.SetParent(root.transform, false);
            background.transform.localScale = new Vector3(0.65f, 0.24f, 1f);
            var bgRenderer = background.GetComponent<MeshRenderer>();
            bgRenderer.material = MrShaders.CreateUnlitTransparent(new Color(0.04f, 0.05f, 0.07f, 0.72f));

            var meshCollider = background.GetComponent<MeshCollider>();
            if (meshCollider)
            {
                meshCollider.enabled = false;
            }

            var textObject = new GameObject("Text");
            textObject.transform.SetParent(root.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            var textMesh = textObject.AddComponent<TextMesh>();
            textMesh.color = Color.white;
            MrFonts.Configure(textMesh, "새 메모", defaultCharacterSize, Color.white);

            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(0.65f, 0.24f, 0.025f);

            var note = root.AddComponent<WorldTextNote>();
            note.Initialize(textMesh, bgRenderer);

            var item = root.AddComponent<SpawnableItem>();
            item.Initialize(SpawnableItemType.Text);
            item.ConfigureMiniMenu(miniMenuPrefab);
            item.OnEditRequested.AddListener(() => configPanel.Open(textMesh, note));
            item.OnColorRequested.AddListener(() => configPanel.OpenColorPicker(textMesh, note));
            item.Select();
            configPanel.Open(textMesh, note);
        }
    }
}
