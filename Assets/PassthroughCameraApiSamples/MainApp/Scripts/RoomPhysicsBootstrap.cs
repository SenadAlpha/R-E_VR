// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp
{
    /// <summary>
    /// Invisible MRUK EffectMesh colliders on real floor/walls/tables. No GLOBAL_MESH.
    /// Space Setup is requested when play physics items are spawned, not on app start.
    /// </summary>
    public class RoomPhysicsBootstrap : MonoBehaviour
    {
        private static RoomPhysicsBootstrap s_instance;
        private bool m_busy;
        private EffectMesh m_effect;

        private void Awake()
        {
            s_instance = this;
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
        }

        private void Start()
        {
            StartCoroutine(SetupExistingRoom());
        }

        /// <summary>
        /// Opens Space Setup, reloads the room, then runs <paramref name="onReady"/>.
        /// </summary>
        public static void RequestScanThen(Action onReady)
        {
            if (!s_instance)
            {
                s_instance = FindFirstObjectByType<RoomPhysicsBootstrap>();
            }

            if (!s_instance)
            {
                onReady?.Invoke();
                return;
            }

            s_instance.StartCoroutine(s_instance.ScanThen(onReady));
        }

        private IEnumerator SetupExistingRoom()
        {
            yield return EnsureReady();
            var mruk = MRUK.Instance;
            if (mruk && mruk.GetCurrentRoom() == null)
            {
                var load = mruk.LoadSceneFromDevice(false);
                while (!load.IsCompleted)
                {
                    yield return null;
                }
            }

            RebuildColliders();
            Play.PlayPhysicsGround.DestroyFakes();
        }

        private IEnumerator ScanThen(Action onReady)
        {
            while (m_busy)
            {
                yield return null;
            }

            m_busy = true;
            try
            {
                yield return EnsureReady();

                var capture = OVRScene.RequestSpaceSetup();
                while (!capture.IsCompleted)
                {
                    yield return null;
                }

                try
                {
                    if (capture.IsCompleted)
                    {
                        _ = capture.GetResult();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[RoomPhysicsBootstrap] Space Setup: " + ex.Message);
                }

                var mruk = MRUK.Instance;
                if (mruk)
                {
                    var load = mruk.LoadSceneFromDevice(false);
                    while (!load.IsCompleted)
                    {
                        yield return null;
                    }
                }

                RebuildColliders();
                Play.PlayPhysicsGround.DestroyFakes();
                onReady?.Invoke();
            }
            finally
            {
                m_busy = false;
            }
        }

        private IEnumerator EnsureReady()
        {
            if (!OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.Scene))
            {
                OVRPermissionsRequester.Request(new[] { OVRPermissionsRequester.Permission.Scene });
                var waited = 0f;
                while (waited < 8f
                       && !OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.Scene))
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            var mruk = MRUK.Instance;
            if (!mruk)
            {
                var go = new GameObject("MRUK");
                go.transform.SetParent(transform, false);
                mruk = go.AddComponent<MRUK>();
                mruk.SceneSettings = new MRUK.MRUKSettings
                {
                    LoadSceneOnStartup = false
                };
            }

            m_effect = GetComponent<EffectMesh>();
            if (!m_effect)
            {
                m_effect = gameObject.AddComponent<EffectMesh>();
            }

            m_effect.SpawnOnStart = MRUK.RoomFilter.CurrentRoomOnly;
            m_effect.Colliders = true;
            m_effect.CastShadow = false;
            m_effect.HideMesh = true;
            m_effect.Labels = MRUKAnchor.SceneLabels.FLOOR
                            | MRUKAnchor.SceneLabels.CEILING
                            | MRUKAnchor.SceneLabels.WALL_FACE
                            | MRUKAnchor.SceneLabels.INVISIBLE_WALL_FACE
                            | MRUKAnchor.SceneLabels.TABLE
                            | MRUKAnchor.SceneLabels.COUCH
                            | MRUKAnchor.SceneLabels.STORAGE
                            | MRUKAnchor.SceneLabels.BED
                            | MRUKAnchor.SceneLabels.SCREEN
                            | MRUKAnchor.SceneLabels.OTHER;

            yield return null;
        }

        private void RebuildColliders()
        {
            if (!m_effect)
            {
                m_effect = GetComponent<EffectMesh>();
            }

            var mruk = MRUK.Instance;
            if (!m_effect || !mruk || mruk.GetCurrentRoom() == null)
            {
                return;
            }

            m_effect.DestroyMesh();
            m_effect.CreateMesh();
        }
    }
}
