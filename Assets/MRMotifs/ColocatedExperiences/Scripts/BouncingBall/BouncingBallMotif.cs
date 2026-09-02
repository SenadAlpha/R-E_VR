// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace MRMotifs.ColocatedExperiences.BouncingBall
{
    // Note: This script never actually used the Fusion API - it was previously
    // wrapped in "#if FUSION2" unnecessarily. Un-guarded so the local (non-networked)
    // BallSpawnerLocal (MainApp/Play submenu) can use this prefab directly.
    public class BouncingBallMotif : MonoBehaviour
    {
        [SerializeField] private AudioClip spawnSound;
        [SerializeField] private AudioClip ballSound;

        private AudioSource m_audioSource;

        private void Awake()
        {
            m_audioSource = GetComponent<AudioSource>();
            if (spawnSound)
            {
                m_audioSource.PlayOneShot(spawnSound);
            }
        }

        private void OnCollisionEnter(Collision other)
        {
            var impactStrength = other.relativeVelocity.magnitude;

            if (!(impactStrength > 5.0f))
            {
                return;
            }

            if (ballSound)
            {
                m_audioSource.PlayOneShot(ballSound);
            }
        }
    }
}
