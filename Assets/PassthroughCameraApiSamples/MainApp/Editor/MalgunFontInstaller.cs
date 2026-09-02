// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.IO;
using UnityEditor;
using UnityEngine;

namespace PassthroughCameraSamples.MainApp.EditorTools
{
    /// <summary>
    /// Windows의 맑은 고딕(malgun.ttf)을 Resources로 복사해 Quest 빌드에 포함합니다.
    /// </summary>
    [InitializeOnLoad]
    public static class MalgunFontInstaller
    {
        private const string DestPath = "Assets/PassthroughCameraApiSamples/MainApp/Resources/Fonts/malgun.ttf";

        static MalgunFontInstaller()
        {
            TryInstall();
        }

        [MenuItem("MainApp/Install Malgun Gothic Font")]
        public static void TryInstall()
        {
            EditorApplication.delayCall += InstallDeferred;
        }

        private static void InstallDeferred()
        {
            if (File.Exists(DestPath))
            {
                return;
            }

            var source = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Fonts), "malgun.ttf");
            if (!File.Exists(source))
            {
                Debug.LogWarning("[MalgunFontInstaller] C:\\Windows\\Fonts\\malgun.ttf 를 찾지 못했습니다.");
                return;
            }

            var destDir = Path.GetDirectoryName(DestPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            File.Copy(source, DestPath, false);
            AssetDatabase.Refresh();
            Debug.Log("[MalgunFontInstaller] Copied Malgun Gothic to " + DestPath);
        }
    }
}
