// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Reflection;
using Meta.XR;
using Meta.XR.Samples;
using PassthroughCameraSamples.MainApp.Admin;
using PassthroughCameraSamples.MainApp.Draw;
using PassthroughCameraSamples.MainApp.Help;
using PassthroughCameraSamples.MainApp.Interaction;
using PassthroughCameraSamples.MainApp.Paper;
using PassthroughCameraSamples.MainApp.Photo;
using PassthroughCameraSamples.MainApp.Play;
using PassthroughCameraSamples.MainApp.TextTools;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PassthroughCameraSamples.MainApp
{
    /// <summary>
    /// StartScene에 MR 앱 도구(텍스트/사진/스캔/그리기/도움말/갖고놀기)를 런타임에
    /// 한 번만 생성·연결합니다. 별도 MainApp 씬이나 Editor 메뉴 클릭 없이 동작합니다.
    /// </summary>
    public static class MrAppRuntimeSetup
    {
        private static MrAppServices s_services;
        private static bool s_created;

        public static MrAppServices EnsureCreated()
        {
            if (s_created && s_services != null)
            {
                return s_services;
            }

            try
            {
                s_services = Create();
                s_created = s_services != null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MrAppRuntimeSetup] Failed: {ex}");
                s_services = null;
                s_created = false;
            }

            return s_services;
        }

        private static MrAppServices Create()
        {
            var cameraRig = Object.FindFirstObjectByType<OVRCameraRig>();
            if (!cameraRig)
            {
                Debug.LogError("[MrAppRuntimeSetup] OVRCameraRig not found.");
                return null;
            }

            var root = new GameObject("MrApp");
            Object.DontDestroyOnLoad(root);

            var cameraAccess = Object.FindFirstObjectByType<PassthroughCameraAccess>();
            if (!cameraAccess)
            {
                var cameraAccessGo = new GameObject("PassthroughCameraAccess");
                cameraAccessGo.transform.SetParent(root.transform);
                cameraAccess = cameraAccessGo.AddComponent<PassthroughCameraAccess>();
            }

            if (!OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.PassthroughCameraAccess))
            {
                OVRPermissionsRequester.Request(new[] { OVRPermissionsRequester.Permission.PassthroughCameraAccess });
            }

            var envGo = new GameObject("EnvironmentRaycastManager");
            envGo.transform.SetParent(root.transform);
            var envRaycast = envGo.AddComponent<EnvironmentRaycastManager>();

            root.AddComponent<SpawnableInteractionDriver>();
            root.AddComponent<GuardianBoundarySuppressor>();
            root.AddComponent<RoomPhysicsBootstrap>();

            var miniMenuTemplate = BuildMiniMenuTemplate();

            // Text
            var textToolGo = new GameObject("TextTool");
            textToolGo.transform.SetParent(root.transform);
            var textTool = textToolGo.AddComponent<TextToolSpawner>();
            var textConfigGo = new GameObject("TextConfigPanel");
            textConfigGo.transform.SetParent(root.transform);
            var textConfig = textConfigGo.AddComponent<TextConfigPanel>();
            BuildTextConfigPanel(textConfigGo, textConfig, cameraRig);
            SetField(textTool, "cameraRig", cameraRig);
            SetField(textTool, "configPanel", textConfig);
            SetField(textTool, "miniMenuPrefab", miniMenuTemplate);

            // Photo
            var photoGo = new GameObject("PhotoTool");
            photoGo.transform.SetParent(root.transform);
            var photoTool = photoGo.AddComponent<PhotoViewfinderController>();
            var viewfinder = BuildViewfinder(photoGo.transform, out var viewfinderImage);
            SetField(photoTool, "cameraAccess", cameraAccess);
            SetField(photoTool, "cameraRig", cameraRig);
            SetField(photoTool, "viewfinderRoot", viewfinder);
            SetField(photoTool, "viewfinderImage", viewfinderImage);
            SetField(photoTool, "miniMenuPrefab", miniMenuTemplate);

            // Paper scan
            var paperGo = new GameObject("PaperScanTool");
            paperGo.transform.SetParent(root.transform);
            var paperTool = paperGo.AddComponent<PaperScanController>();
            var scanRoot = new GameObject("ScanRoot");
            scanRoot.transform.SetParent(paperGo.transform);
            var markers = new Transform[4];
            for (var i = 0; i < 4; i++)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "CornerMarker" + i;
                marker.transform.SetParent(scanRoot.transform);
                marker.transform.localScale = Vector3.one * 0.02f;
                Object.Destroy(marker.GetComponent<Collider>());
                marker.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color")) { color = Color.green };
                markers[i] = marker.transform;
            }

            var frame = GameObject.CreatePrimitive(PrimitiveType.Quad);
            frame.name = "ScanFrame";
            frame.transform.SetParent(scanRoot.transform, false);
            frame.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            frame.transform.localScale = new Vector3(0.68f, 0.48f, 1f);
            Object.Destroy(frame.GetComponent<Collider>());
            var frameShader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            frame.GetComponent<MeshRenderer>().material = new Material(frameShader) { color = new Color(0.15f, 0.95f, 0.35f, 0.28f) };

            var instructionGo = RuntimeUguiBuilder.CreateWorldPanel("PaperScanHelp", new Vector2(520, 170));
            instructionGo.transform.SetParent(paperGo.transform, false);
            instructionGo.SetActive(false);
            var helpRt = instructionGo.GetComponent<RectTransform>();
            var helpLabel = RuntimeUguiBuilder.CreateLabel(helpRt,
                "종이를 프레임에. 초록 점이면 보정 캡처. A/캡처, B/취소",
                new Vector2(0f, 48f), new Vector2(500, 70), TextAnchor.MiddleCenter);
            helpLabel.color = Color.white;
            var captureBtn = RuntimeUguiBuilder.CreateButton(helpRt, "캡처", new Vector2(-110f, -48f), new Vector2(180, 56));
            var cancelBtn = RuntimeUguiBuilder.CreateButton(helpRt, "취소", new Vector2(110f, -48f), new Vector2(180, 56));

            var edgeShader = MrShaders.EdgeSobel();
            SetField(paperTool, "cameraAccess", cameraAccess);
            SetField(paperTool, "cameraRig", cameraRig);
            SetField(paperTool, "edgeShader", edgeShader);
            SetField(paperTool, "scanRoot", scanRoot);
            SetArrayField(paperTool, "cornerMarkers", markers);
            SetField(paperTool, "miniMenuPrefab", miniMenuTemplate);
            SetField(paperTool, "instructionRoot", instructionGo);
            SetField(paperTool, "captureButton", captureBtn);
            SetField(paperTool, "cancelButton", cancelBtn);

            // Draw
            var drawGo = new GameObject("DrawTool");
            drawGo.transform.SetParent(root.transform);
            var drawTool = drawGo.AddComponent<DrawToolSpawner>();
            SetField(drawTool, "cameraRig", cameraRig);
            SetField(drawTool, "miniMenuPrefab", miniMenuTemplate);

            // Help
            var helpGo = new GameObject("HelpPanel");
            helpGo.transform.SetParent(root.transform);
            var helpPanel = helpGo.AddComponent<HelpPanelController>();
            BuildHelpPanel(helpGo, helpPanel, cameraRig);

            // Play
            var placementGo = new GameObject("InstantPlacement");
            placementGo.transform.SetParent(root.transform);
            var placement = placementGo.AddComponent<InstantPlacementSpawner>();
            var placementItem = BuildPlacementItemTemplate();
            SetField(placement, "placementPrefab", placementItem);
            SetField(placement, "raycastManager", envRaycast);
            SetField(placement, "miniMenuPrefab", miniMenuTemplate);

            var ballSpawner = BuildBallSpawnerTemplate();

            var playGo = new GameObject("PlaySubmenu");
            playGo.transform.SetParent(root.transform);
            var playSubmenu = playGo.AddComponent<PlaySubmenuController>();
            var handPlay = playGo.AddComponent<HandInteractionPlay>();
            SetField(handPlay, "cameraRig", cameraRig);
            SetField(handPlay, "raycastManager", envRaycast);
            BuildPlaySubmenu(playGo, playSubmenu, cameraRig, cameraAccess, ballSpawner, placement);
            SetField(playSubmenu, "miniMenuPrefab", miniMenuTemplate);
            SetField(playSubmenu, "handInteractionPlay", handPlay);

            var adminGo = new GameObject("AdminMenu");
            adminGo.transform.SetParent(root.transform);
            var adminMenu = adminGo.AddComponent<AdminMenuController>();
            var imageSpawner = adminGo.AddComponent<AdminImageSpawner>();
            SetField(imageSpawner, "miniMenuPrefab", miniMenuTemplate);
            SetField(imageSpawner, "cameraRig", cameraRig);

            return new MrAppServices
            {
                TextTool = textTool,
                PhotoTool = photoTool,
                PaperScanTool = paperTool,
                DrawTool = drawTool,
                HelpPanel = helpPanel,
                PlaySubmenu = playSubmenu,
                AdminMenu = adminMenu,
                ImageSpawner = imageSpawner
            };
        }

        private static ItemMiniMenu BuildMiniMenuTemplate()
        {
            var go = RuntimeUguiBuilder.CreateWorldPanel("ItemMiniMenuTemplate", new Vector2(170, 280));
            go.SetActive(false);
            Object.DontDestroyOnLoad(go);

            var rt = go.GetComponent<RectTransform>();
            var sizeBtn = RuntimeUguiBuilder.CreateButton(rt, "크기", new Vector2(0, 84), new Vector2(150, 48));
            var colorBtn = RuntimeUguiBuilder.CreateButton(rt, "색", new Vector2(0, 28), new Vector2(150, 48));
            var editBtn = RuntimeUguiBuilder.CreateButton(rt, "수정", new Vector2(0, -28), new Vector2(150, 48));
            var warpBtn = RuntimeUguiBuilder.CreateButton(rt, "왜곡", new Vector2(0, -84), new Vector2(150, 48));
            var trashBtn = RuntimeUguiBuilder.CreateButton(rt, "삭제", new Vector2(0, -140), new Vector2(150, 48));

            var miniMenu = go.AddComponent<ItemMiniMenu>();
            SetField(miniMenu, "sizeButton", sizeBtn);
            SetField(miniMenu, "colorButton", colorBtn);
            SetField(miniMenu, "editButton", editBtn);
            SetField(miniMenu, "warpButton", warpBtn);
            SetField(miniMenu, "trashButton", trashBtn);
            return miniMenu;
        }

        private static GameObject BuildPlacementItemTemplate()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "PlacementItemTemplate";
            go.transform.localScale = Vector3.one * 0.1f;
            go.SetActive(false);
            Object.DontDestroyOnLoad(go);
            return go;
        }

        private static BallSpawnerLocal BuildBallSpawnerTemplate()
        {
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "BallTemplate";
            ball.transform.localScale = Vector3.one * 0.08f;
            ball.SetActive(false);
            var rb = ball.AddComponent<Rigidbody>();
            rb.mass = 0.2f;
            Object.DontDestroyOnLoad(ball);

            var spawnerGo = new GameObject("BallSpawnerTemplate");
            spawnerGo.SetActive(false);
            Object.DontDestroyOnLoad(spawnerGo);
            var spawner = spawnerGo.AddComponent<BallSpawnerLocal>();
            SetField(spawner, "projectilePrefab", rb);
            return spawner;
        }

        private static void BuildTextConfigPanel(GameObject host, TextConfigPanel panel, OVRCameraRig cameraRig)
        {
            _ = host;
            _ = panel;
            _ = cameraRig;
        }

        private static GameObject BuildViewfinder(Transform parent, out RawImage image)
        {
            var go = new GameObject("Viewfinder", typeof(RectTransform), typeof(Canvas), typeof(CanvasRenderer));
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(640, 480);
            rt.localScale = Vector3.one * 0.00125f;
            rt.SetParent(parent, false);

            var border = new GameObject("GreenBorder", typeof(RectTransform), typeof(Image));
            var borderRt = border.GetComponent<RectTransform>();
            borderRt.SetParent(rt, false);
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = Vector2.zero;
            border.GetComponent<Image>().color = Color.green;

            var rawGo = new GameObject("CameraImage", typeof(RectTransform), typeof(RawImage));
            var rawRt = rawGo.GetComponent<RectTransform>();
            rawRt.SetParent(rt, false);
            rawRt.anchorMin = Vector2.zero;
            rawRt.anchorMax = Vector2.one;
            rawRt.sizeDelta = new Vector2(-16, -16);
            image = rawGo.GetComponent<RawImage>();

            go.SetActive(false);
            return go;
        }

        private static void BuildHelpPanel(GameObject host, HelpPanelController panel, OVRCameraRig cameraRig)
        {
            var panelGo = RuntimeUguiBuilder.CreateWorldPanel("Panel", new Vector2(920, 980), host.transform);
            var rt = panelGo.GetComponent<RectTransform>();

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.SetParent(rt, false);
            viewportRt.anchoredPosition = new Vector2(-50f, 30f);
            viewportRt.sizeDelta = new Vector2(740f, 780f);
            var viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.04f);
            viewportImage.raycastTarget = false;
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.SetParent(viewportRt, false);
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 780f);

            var bodyText = RuntimeUguiBuilder.CreateLabel(contentRt, "", Vector2.zero, new Vector2(720, 780), TextAnchor.UpperLeft);
            var bodyRt = bodyText.transform.parent as RectTransform;
            if (bodyRt)
            {
                bodyRt.anchorMin = Vector2.zero;
                bodyRt.anchorMax = Vector2.one;
                bodyRt.offsetMin = new Vector2(8f, 8f);
                bodyRt.offsetMax = new Vector2(-8f, -8f);
                bodyRt.anchoredPosition = Vector2.zero;
            }

            bodyText.color = Color.white;
            bodyText.fontSize = 36;
            bodyText.resizeTextForBestFit = false;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;

            var scroll = panelGo.AddComponent<ScrollRect>();
            scroll.viewport = viewportRt;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            scroll.inertia = true;

            var plus = RuntimeUguiBuilder.CreateButton(rt, "+", new Vector2(390, 120), new Vector2(88, 64));
            var minus = RuntimeUguiBuilder.CreateButton(rt, "-", new Vector2(390, 48), new Vector2(88, 64));
            var scrollUp = RuntimeUguiBuilder.CreateButton(rt, "▲", new Vector2(390, -24), new Vector2(88, 64));
            var scrollDown = RuntimeUguiBuilder.CreateButton(rt, "▼", new Vector2(390, -96), new Vector2(88, 64));
            var close = RuntimeUguiBuilder.CreateButton(rt, "닫기", new Vector2(0, -430), new Vector2(240, 64));
            panelGo.SetActive(false);

            SetField(panel, "panelRoot", panelGo);
            SetField(panel, "bodyText", bodyText);
            SetField(panel, "scrollRect", scroll);
            SetField(panel, "content", contentRt);
            SetField(panel, "closeButton", close);
            SetField(panel, "plusButton", plus);
            SetField(panel, "minusButton", minus);
            SetField(panel, "scrollUpButton", scrollUp);
            SetField(panel, "scrollDownButton", scrollDown);
            SetField(panel, "cameraRig", cameraRig);
        }

        private static void BuildPlaySubmenu(GameObject host, PlaySubmenuController controller, OVRCameraRig cameraRig,
            PassthroughCameraAccess cameraAccess, BallSpawnerLocal ballSpawner, InstantPlacementSpawner placement)
        {
            var panelGo = RuntimeUguiBuilder.CreateWorldPanel("Panel", new Vector2(420, 400), host.transform);
            var rt = panelGo.GetComponent<RectTransform>();
            var y = 130f;
            const float step = 80f;

            var shaderBtn = RuntimeUguiBuilder.CreateButton(rt, "Shader", new Vector2(0, y), new Vector2(360, 64));
            y -= step;
            var cubeBtn = RuntimeUguiBuilder.CreateButton(rt, "3D Cube", new Vector2(0, y), new Vector2(360, 64));
            y -= step;
            var handBtn = RuntimeUguiBuilder.CreateButton(rt, "HandInteraction", new Vector2(0, y), new Vector2(360, 64));
            y -= step;
            var closeBtn = RuntimeUguiBuilder.CreateButton(rt, "Close", new Vector2(0, y), new Vector2(360, 64));

            panelGo.SetActive(false);
            SetField(controller, "panelRoot", panelGo);
            SetField(controller, "shaderButton", shaderBtn);
            SetField(controller, "spawnCubeButton", cubeBtn);
            SetField(controller, "handInteractionButton", handBtn);
            SetField(controller, "closeButton", closeBtn);
            SetField(controller, "cameraRig", cameraRig);
            SetField(controller, "cameraAccess", cameraAccess);
            _ = ballSpawner;
            _ = placement;
        }

        private static void SetField(Object target, string fieldName, Object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                Debug.LogError($"[MrAppRuntimeSetup] Field '{fieldName}' not found on {target.GetType().Name}");
                return;
            }

            field.SetValue(target, value);
        }

        private static void SetArrayField(Object target, string fieldName, Transform[] values)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(target, values);
        }
    }

    public class MrAppServices
    {
        public TextToolSpawner TextTool;
        public PhotoViewfinderController PhotoTool;
        public PaperScanController PaperScanTool;
        public DrawToolSpawner DrawTool;
        public HelpPanelController HelpPanel;
        public PlaySubmenuController PlaySubmenu;
        public AdminMenuController AdminMenu;
        public AdminImageSpawner ImageSpawner;
    }
}
