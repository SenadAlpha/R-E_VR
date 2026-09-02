// Copyright (c) Meta Platforms, Inc. and affiliates.

using PassthroughCameraSamples.MainApp;
using PassthroughCameraSamples.MainApp.Interaction;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Draw
{
    /// <summary>
    /// Hooked up to the Main Menu's 그리기 button. Spawns a whiteboard quad wired to
    /// WhiteboardManagerLocal + WhiteboardDrawingLocal, wrapped as a SpawnableItem
    /// (Draw type) so the global select/mini-menu/grab/wall-snap rules apply. The
    /// mini-menu's "수정" button toggles WhiteboardDrawingLocal.DrawingEnabled.
    /// </summary>
    public class DrawToolSpawner : MonoBehaviour
    {
        [SerializeField] private OVRCameraRig cameraRig;
        [SerializeField] private ItemMiniMenu miniMenuPrefab;
        [SerializeField] private float spawnDistance = 0.8f;
        [SerializeField] private Vector2 whiteboardSize = new(0.6f, 0.4f);

        private void Awake()
        {
            if (!cameraRig)
            {
                cameraRig = FindFirstObjectByType<OVRCameraRig>();
            }
        }

        public void SpawnWhiteboard()
        {
            var head = cameraRig.centerEyeAnchor;
            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = head.forward;
            }

            var root = GameObject.CreatePrimitive(PrimitiveType.Quad);
            root.name = "WhiteboardItem";
            root.transform.position = head.position + forward * (spawnDistance + 0.18f);
            root.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);
            root.transform.localScale = new Vector3(whiteboardSize.x, whiteboardSize.y, 1f);

            var meshRenderer = root.GetComponent<MeshRenderer>();
            meshRenderer.material = MrShaders.CreateUnlitTexture(Texture2D.whiteTexture);

            var manager = root.AddComponent<WhiteboardManagerLocal>();
            manager.SetRenderer(meshRenderer);

            var drawing = root.AddComponent<WhiteboardDrawingLocal>();
            drawing.SetWhiteboard(manager);
            drawing.DrawingEnabled = true;

            var item = root.AddComponent<SpawnableItem>();
            item.Initialize(SpawnableItemType.Draw);
            item.ConfigureMiniMenu(miniMenuPrefab);
            item.OnEditRequested.AddListener(() =>
            {
                drawing.DrawingEnabled = !drawing.DrawingEnabled;
                meshRenderer.material.color = drawing.DrawingEnabled
                    ? Color.white
                    : new Color(0.85f, 0.85f, 0.9f, 1f);
            });
            item.Select();
        }
    }
}
