// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp.EditorTools
{
    /// <summary>
    /// 이전에 별도 MainApp.unity 씬을 만들던 도구였으나, MR 앱은 StartScene에서
    /// <see cref="MrAppRuntimeSetup"/>으로 런타임에 자동 구성됩니다.
    /// </summary>
    public static class MainAppSceneBuilder
    {
        [MenuItem("MainApp/About MR App Setup")]
        public static void ShowInfo()
        {
            EditorUtility.DisplayDialog(
                "MR App — StartScene에서 실행",
                "별도 MainApp 씬이 필요 없습니다.\n\n" +
                "1. StartScene을 Play 하거나 Quest에 빌드/실행\n" +
                "2. 왼쪽 컨트롤러 ☰ 버튼으로 메뉴 열기\n" +
                "3. 텍스트 / 사진 / 종이 스캔 / 그리기 / 도움말 / 갖고놀기 사용\n\n" +
                "패스스루·카메라 API는 Quest 3/3S 실기기(또는 Horizon Link)에서 테스트하세요.",
                "OK");
        }
    }
}
