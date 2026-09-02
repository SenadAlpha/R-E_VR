// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp.Interaction
{
    /// <summary>
    /// 이동 중 벽 근처면 Scene API(MRUK)로 벽에 붙입니다.
    /// </summary>
    public class WallSnapBehaviour : MonoBehaviour
    {
        [SerializeField] private float snapDistance = 0.15f;
        [SerializeField] private float snapLerpSpeed = 12f;
        [SerializeField] private float wallOffset = 0.01f;

        private SpawnableItem m_item;
        private static readonly LabelFilter s_wallFilter = LabelFilter.Included(MRUKAnchor.SceneLabels.WALL_FACE);

        private void Awake()
        {
            m_item = GetComponent<SpawnableItem>();
        }

        private void LateUpdate()
        {
            var driver = SpawnableInteractionDriver.Instance;
            if (!m_item || driver == null || !driver.IsMoving(m_item))
            {
                return;
            }

            var room = MRUK.Instance != null ? MRUK.Instance.GetCurrentRoom() : null;
            if (room == null)
            {
                return;
            }

            var distance = room.TryGetClosestSurfacePosition(
                transform.position, out _, out var closestAnchor, out var meshNormal, s_wallFilter);

            if (distance > snapDistance || closestAnchor == null)
            {
                return;
            }

            var wallNormal = closestAnchor.transform.forward;
            if (wallNormal.sqrMagnitude < 0.0001f)
            {
                wallNormal = meshNormal;
            }

            wallNormal.Normalize();
            var wallPoint = closestAnchor.transform.position;
            var projected = transform.position - Vector3.Dot(transform.position - wallPoint, wallNormal) * wallNormal;
            var targetPosition = projected + wallNormal * wallOffset;
            var targetRotation = Quaternion.LookRotation(-wallNormal, Vector3.up);
            var t = Time.deltaTime * snapLerpSpeed;
            transform.position = Vector3.Lerp(transform.position, targetPosition, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
        }
    }
}
