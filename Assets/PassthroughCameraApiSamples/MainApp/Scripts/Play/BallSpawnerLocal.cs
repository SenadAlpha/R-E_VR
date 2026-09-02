// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Play
{
    /// <summary>
    /// Local (single-player) replacement for MRMotif's Fusion-networked
    /// BallSpawnerMotif. Fires a physical ball prefab (e.g. BouncingBall.prefab with
    /// its now-unwrapped BouncingBallMotif audio hook) forward from the headset on
    /// right trigger press. Plain Instantiate/Destroy - no Runner.Spawn required.
    /// </summary>
    public class BallSpawnerLocal : MonoBehaviour
    {
        [SerializeField] private Rigidbody projectilePrefab;
        [SerializeField] private float fireForce = 4f;
        [SerializeField] private float lifeTimeSeconds = 8f;

        private Transform m_firePoint;

        private void Start()
        {
            var cameraRig = FindFirstObjectByType<OVRCameraRig>();
            m_firePoint = cameraRig ? cameraRig.centerEyeAnchor : transform;
        }

        private void Update()
        {
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            {
                SpawnProjectile();
            }
        }

        private void SpawnProjectile()
        {
            if (!projectilePrefab)
            {
                Debug.LogError("[BallSpawnerLocal] No projectile prefab assigned.");
                return;
            }

            var ball = Instantiate(projectilePrefab, m_firePoint.position, m_firePoint.rotation);
            ball.gameObject.SetActive(true);
            ball.AddForce(m_firePoint.forward * fireForce, ForceMode.Impulse);
            Destroy(ball.gameObject, lifeTimeSeconds);
        }
    }
}
