// Copyright (c) Meta Platforms, Inc. and affiliates.

using PassthroughCameraSamples.MainApp.Interaction;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Admin
{
    public class AdminImageSpawner : MonoBehaviour
    {
        [SerializeField] private ItemMiniMenu miniMenuPrefab;
        [SerializeField] private OVRCameraRig cameraRig;
        [SerializeField] private float spawnDistance = 0.68f;
        [SerializeField] private float spawnHeight = 0.50f;

        public void Spawn(string resourcePath)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (!texture)
            {
                Debug.LogError("[AdminImageSpawner] Missing Resources texture: " + resourcePath +
                               ". Put a PNG at Assets/Resources/" + resourcePath + ".png");
                return;
            }

            if (!cameraRig)
            {
                cameraRig = FindFirstObjectByType<OVRCameraRig>();
            }

            var head = cameraRig && cameraRig.centerEyeAnchor ? cameraRig.centerEyeAnchor : Camera.main?.transform;
            if (!head)
            {
                return;
            }

            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = head.forward;
            }

            var root = GameObject.CreatePrimitive(PrimitiveType.Quad);
            root.name = "Document_" + resourcePath.Replace('/', '_');
            root.transform.SetParent(MainAppPlacement.SpawnRoot(), true);
            root.transform.position = head.position + forward * spawnDistance;
            root.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);

            var aspect = (float)texture.width / Mathf.Max(1, texture.height);
            float width;
            float height;
            if (aspect >= 1f)
            {
                width = spawnHeight;
                height = spawnHeight / aspect;
            }
            else
            {
                height = spawnHeight;
                width = spawnHeight * aspect;
            }

            root.transform.localScale = new Vector3(width, height, 1f);

            var renderer = root.GetComponent<MeshRenderer>();
            renderer.material = MrShaders.CreateUnlitTexture(texture);

            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(1f, 1f, 0.04f);

            var item = root.AddComponent<SpawnableItem>();
            item.Initialize(SpawnableItemType.Document);
            item.ConfigureMiniMenu(miniMenuPrefab);

            var surface = root.AddComponent<DocumentSurface>();
            surface.Configure(texture);
            item.Select();
        }
    }
}
