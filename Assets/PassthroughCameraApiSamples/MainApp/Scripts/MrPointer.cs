// Copyright (c) Meta Platforms, Inc. and affiliates.

using PassthroughCameraSamples.StartScene;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp
{
    /// <summary>
    /// 메인 메뉴를 닫아도 도구 UI(텍스트 편집 등)를 누를 수 있도록 레이저를 유지합니다.
    /// </summary>
    public static class MrPointer
    {
        public static void Show()
        {
            var laser = Object.FindFirstObjectByType<LaserPointer>(FindObjectsInactive.Include);
            if (laser)
            {
                laser.gameObject.SetActive(true);
            }

            foreach (var raycaster in Object.FindObjectsByType<OVRRaycaster>(FindObjectsSortMode.None))
            {
                if (laser && (raycaster.pointer == null || !raycaster.pointer))
                {
                    raycaster.pointer = laser.gameObject;
                }
            }
        }

        public static void Hide()
        {
            var laser = Object.FindFirstObjectByType<LaserPointer>(FindObjectsInactive.Include);
            if (laser)
            {
                laser.gameObject.SetActive(false);
            }
        }
    }
}
