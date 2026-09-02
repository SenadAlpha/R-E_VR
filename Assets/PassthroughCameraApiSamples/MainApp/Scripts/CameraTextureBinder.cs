// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp
{
    /// <summary>
    /// Keeps a spawned preview quad showing the live passthrough camera texture.
    /// </summary>
    public class CameraTextureBinder : MonoBehaviour
    {
        [SerializeField] private PassthroughCameraAccess cameraAccess;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private bool setMainTexProperty = true;
        [SerializeField] private bool flipHorizontal;

        public void Configure(PassthroughCameraAccess access, Renderer renderer, bool useMainTexProperty,
            bool flipX = false)
        {
            cameraAccess = access;
            targetRenderer = renderer;
            setMainTexProperty = useMainTexProperty;
            flipHorizontal = flipX;
            Apply();
        }

        private void LateUpdate()
        {
            Apply();
        }

        private void Apply()
        {
            if (!cameraAccess || !cameraAccess.IsPlaying || !targetRenderer)
            {
                return;
            }

            var texture = cameraAccess.GetTexture();
            if (!texture)
            {
                return;
            }

            if (setMainTexProperty)
            {
                targetRenderer.material.SetTexture("_MainTex", texture);
            }

            targetRenderer.material.mainTexture = texture;
            if (flipHorizontal)
            {
                var scale = new Vector2(-1f, 1f);
                var offset = new Vector2(1f, 0f);
                targetRenderer.material.mainTextureScale = scale;
                targetRenderer.material.mainTextureOffset = offset;
                if (targetRenderer.material.HasProperty("_MainTex"))
                {
                    targetRenderer.material.SetTextureScale("_MainTex", scale);
                    targetRenderer.material.SetTextureOffset("_MainTex", offset);
                }
            }
        }
    }
}
