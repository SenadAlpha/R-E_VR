// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Play
{
    /// <summary>
    /// Spawns a small grab-and-throw set (not the whole UnityMRStarterForQuest project):
    /// a few rigidbody toys plus a floor they can land on.
    /// </summary>
    public class HandInteractionPlay : MonoBehaviour
    {
        [SerializeField] private OVRCameraRig cameraRig;
        [SerializeField] private EnvironmentRaycastManager raycastManager;
        [SerializeField] private float spawnDistance = 0.7f;

        private GameObject m_session;

        public void SpawnSession()
        {
            if (!cameraRig)
            {
                cameraRig = FindFirstObjectByType<OVRCameraRig>();
            }

            if (!raycastManager)
            {
                raycastManager = FindAnyObjectByType<EnvironmentRaycastManager>();
            }

            if (m_session)
            {
                Destroy(m_session);
            }

            var head = cameraRig && cameraRig.centerEyeAnchor ? cameraRig.centerEyeAnchor : Camera.main?.transform;
            if (!head)
            {
                Debug.LogError("[HandInteractionPlay] No head pose.");
                return;
            }

            var origin = PlayPhysicsGround.LandingInFront(head, spawnDistance);

            m_session = new GameObject("HandInteractionSession");
            m_session.transform.SetParent(MainAppPlacement.SpawnRoot(), true);

            PlayPhysicsGround.DestroyFakes();
            BuildToys(origin);
        }

        private void BuildToys(Vector3 origin)
        {
            SpawnToy(PrimitiveType.Cube, origin + Vector3.left * 0.16f + Vector3.up * 0.08f, Vector3.one * 0.12f, new Color(0.25f, 0.72f, 1f));
            SpawnToy(PrimitiveType.Sphere, origin + Vector3.up * 0.08f, Vector3.one * 0.12f, new Color(1f, 0.45f, 0.28f));
            SpawnToy(PrimitiveType.Capsule, origin + Vector3.right * 0.16f + Vector3.up * 0.08f, new Vector3(0.08f, 0.08f, 0.08f), new Color(0.45f, 0.9f, 0.4f));
            SpawnToy(PrimitiveType.Cube, origin + Vector3.forward * 0.12f + Vector3.up * 0.22f, Vector3.one * 0.09f, new Color(0.95f, 0.85f, 0.25f));
        }

        private void SpawnToy(PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = "HandToy_" + type;
            go.transform.SetParent(m_session.transform, true);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().material = MrShaders.CreateUnlitTexture(Texture2D.whiteTexture);
            go.GetComponent<MeshRenderer>().material.color = color;

            var phys = new PhysicsMaterial
            {
                name = "HandToy",
                dynamicFriction = 0.4f,
                staticFriction = 0.5f,
                bounciness = 0.18f
            };
            go.GetComponent<Collider>().material = phys;
            go.AddComponent<HandPhysicsToy>();
        }
    }
}
