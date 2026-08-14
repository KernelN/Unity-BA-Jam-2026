/////////////////////////////////////////////////////////////////////////////////
//
//	VCToolbar.cs
//
//	Description:	controls the toolbar in the editor.
//					
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Linq;
using UnityEditor.Build;

namespace VSController
{
    public class VCToolbar : EditorWindow
    {
        private bool useMobileControl = false;
        private bool addHealth = true;
        private bool addGrabbing = true;
        private bool spawnPlayer = true;
        private bool spawnUI = true;
        private bool spawnMechanismManager = true;

        [MenuItem("VS Controller/Create Player Controller", false, 1)]
        public static void ShowWindow()
        {
            VCToolbar window = GetWindow<VCToolbar>("Spawn VS Controller");
            window.minSize = new Vector2(220, 280);
        }

        [MenuItem("VS Controller/Setup & Tutorials/Refresh UI & References", false, 20)]
        public static void RefreshAllReferences()
        {

#if UNITY_2022_1_OR_NEWER
            UIManager uiManager = FindAnyObjectByType<UIManager>();
            FPSController fps = FindAnyObjectByType<FPSController>();
            LookController look = FindAnyObjectByType<LookController>();
            Grabbing grabbing = FindAnyObjectByType<Grabbing>();
            Health health = FindAnyObjectByType<Health>();
            HUDManager hudManager = FindAnyObjectByType<HUDManager>();
            GameObject player = GameObject.Find("PlayerController");
            GameObject ui = GameObject.Find("UI");
            AudioSource audioSource = FindAnyObjectByType<AudioSource>();
            var eventSystem = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            var respawnPoint = FindAnyObjectByType<RespawnPoint>();
#else
            UIManager uiManager = FindObjectOfType<UIManager>();
            FPSController fps = FindObjectOfType<FPSController>();
            LookController look = FindObjectOfType<LookController>();
            Grabbing grabbing = FindObjectOfType<Grabbing>();
            Health health = FindObjectOfType<Health>();
            HUDManager hudManager = FindObjectOfType<HUDManager>();
            GameObject player = GameObject.Find("PlayerController");
            GameObject ui = GameObject.Find("UI");
            AudioSource audioSource = FindObjectOfType<AudioSource>();
            var eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            var respawnPoint = FindObjectOfType<RespawnPoint>();
#endif

            GameObject hpBarObj = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(obj => obj.name == "HP_bar");
            GameObject blood = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(obj => obj.name == "Blood");
            GameObject healthOverlay = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(obj => obj.name == "Health");

            bool hasHealthScript = health != null;
            bool hasHPBar = hpBarObj != null && hpBarObj.activeSelf;
            bool hasBlood = blood != null;
            bool hasHealthImage = healthOverlay != null;

            bool addHealth = hasHealthScript || hasHPBar || hasBlood || hasHealthImage;

            Debug.Log(
                $"[Health Check]\n" +
                $"- Health script: {(hasHealthScript ? "yes" : "no")}\n" +
                $"- HP_bar active: {(hasHPBar ? "yes" : "no")}\n" +
                $"- Blood object: {(hasBlood ? "yes" : "no")}\n" +
                $"- Health object: {(hasHealthImage ? "yes" : "no")}\n" +
                $"→ System is {(addHealth ? "PARTIALLY PRESENT — will add missing parts" : "MISSING — nothing to add")}"
            );

            string missing = "";
            if (eventSystem == null) missing += "- EventSystem\n";
            if (audioSource == null) missing += "- Audio Source (GameObject)\n";
            if (hudManager == null) missing += "- HUDManager\n";
            if (respawnPoint == null) missing += "- Respawn Point\n";
            if (player == null) missing += "- PlayerController (prefab)\n";
            if (ui == null) missing += "- UI (prefab)\n";
            if (uiManager == null) missing += "- UIManager\n";
            if (fps == null) missing += "- FPSController\n";
            if (look == null) missing += "- LookController\n";
            if (grabbing == null) missing += "- Grabbing\n";

            if (!string.IsNullOrEmpty(missing))
            {
                bool create = EditorUtility.DisplayDialog(
                    "Missing Components Detected",
                    "Some required components or objects were not found:\n\n" + missing +
                    "\nDo you want to automatically create them from prefabs or add components?",
                    "Yes, create them", "Cancel");

                if (!create) return;

                if (eventSystem == null)
                {
                    GameObject es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
                    Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
                    EditorUtility.SetDirty(es);
                }

                if (audioSource == null)
                {
                    GameObject audioObj = new GameObject("Audio Source");
                    audioSource = audioObj.AddComponent<AudioSource>();
                    audioObj.transform.position = Vector3.zero;
                    Undo.RegisterCreatedObjectUndo(audioObj, "Create Audio Source");
                    EditorUtility.SetDirty(audioObj);
                }

                if (hudManager == null)
                {
                    GameObject hudObj = new GameObject("HUDManager");
                    hudManager = hudObj.AddComponent<HUDManager>();
                    Undo.RegisterCreatedObjectUndo(hudObj, "Create HUDManager");
                    EditorUtility.SetDirty(hudObj);
                }

                if (respawnPoint == null)
                {
                    GameObject respawnObj = new GameObject("Respawn Point", typeof(RespawnPoint));
                    respawnObj.transform.position = Vector3.back * 2f;
                    Undo.RegisterCreatedObjectUndo(respawnObj, "Create Respawn Point");
                    EditorUtility.SetDirty(respawnObj);
                    Debug.Log("Created Respawn Point");
                }

                if (player == null)
                {
                    GameObject prefab = Resources.Load<GameObject>("Prefabs/Camera&Controller");
                    if (prefab != null)
                    {
                        player = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                        player.name = "PlayerController";
                        Undo.RegisterCreatedObjectUndo(player, "Spawn PlayerController");
                        EditorUtility.SetDirty(player);
                    }
                    else
                    {
                        Debug.LogError("'Prefabs/Camera&Controller' not found.");
                        return;
                    }
                }

                if (ui == null)
                {
                    GameObject uiPrefab = Resources.Load<GameObject>("Prefabs/UI");
                    if (uiPrefab != null)
                    {
                        ui = PrefabUtility.InstantiatePrefab(uiPrefab) as GameObject;
                        ui.name = "UI";
                        Undo.RegisterCreatedObjectUndo(ui, "Spawn UI");
                        EditorUtility.SetDirty(ui);
                        Debug.Log("Spawned UI from prefab.");
                    }
                    else
                    {
                        Debug.LogWarning("'Prefabs/UI' not found in Resources.");
                    }
                }

                if (uiManager == null)
                {
                    uiManager = player.GetComponent<UIManager>() ?? player.AddComponent<UIManager>();
                    Undo.RegisterCreatedObjectUndo(player, "Add UIManager");
                    EditorUtility.SetDirty(player);
                }

                if (fps == null)
                {
                    fps = player.GetComponent<FPSController>() ?? player.AddComponent<FPSController>();
                    Undo.RegisterCreatedObjectUndo(player, "Add FPSController");
                    EditorUtility.SetDirty(player);
                }

                if (grabbing == null)
                {
                    grabbing = player.GetComponent<Grabbing>() ?? player.AddComponent<Grabbing>();
                    Undo.RegisterCreatedObjectUndo(player, "Add Grabbing");
                    EditorUtility.SetDirty(player);
                }
            }

            if (addHealth && health == null && player != null)
            {
                health = player.GetComponent<Health>() ?? player.AddComponent<Health>();
                Undo.RegisterCreatedObjectUndo(player, "Add Health");
                EditorUtility.SetDirty(player);
                Debug.Log("Health script added to PlayerController.");
            }

            if (addHealth && ui != null)
            {
                Transform hud = ui.transform.Find("HUD");
                if (hud != null)
                {
                    var allChildren = hud.GetComponentsInChildren<Transform>(true);
                    Transform hpBar = allChildren.FirstOrDefault(t => t.name == "HP_bar");
                    Transform bloodTransform = allChildren.FirstOrDefault(t => t.name == "Blood");
                    Transform healthTransform = allChildren.FirstOrDefault(t => t.name == "Health");

                    if (hpBar != null)
                    {
                        hpBar.gameObject.SetActive(true);
                    }

                    if (bloodTransform == null)
                    {
                        GameObject bloodObj = new GameObject("Blood", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                        bloodObj.transform.SetParent(hud, false);
                        bloodObj.transform.SetSiblingIndex(0);
                        bloodObj.SetActive(false);

                        var image = bloodObj.GetComponent<UnityEngine.UI.Image>();
                        var sprite = Resources.Load<Sprite>("Text2D/UI/Blood");
                        if (sprite != null)
                        {
                            image.sprite = sprite;
                            image.color = new Color(1f, 1f, 1f, 0f);
                            image.raycastTarget = false;

                            var rect = bloodObj.GetComponent<RectTransform>();
                            rect.anchorMin = Vector2.zero;
                            rect.anchorMax = Vector2.one;
                            rect.offsetMin = Vector2.zero;
                            rect.offsetMax = Vector2.zero;
                            rect.pivot = new Vector2(0.5f, 0.5f);
                            Debug.Log("Blood overlay created.");
                        }
                        else
                        {
                            Debug.LogWarning("Sprite 'Blood' not found in Resources/Text2D/UI.");
                        }
                    }

                    if (healthTransform == null)
                    {
                        Debug.Log("Creating Health overlay...");
                        GameObject healObj = new GameObject("Health", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                        healObj.transform.SetParent(hud, false);
                        healObj.transform.SetSiblingIndex(1);
                        healObj.SetActive(false);

                        var image = healObj.GetComponent<UnityEngine.UI.Image>();
                        var sprite = Resources.Load<Sprite>("Text2D/UI/Health");
                        if (sprite != null)
                        {
                            image.sprite = sprite;
                            image.color = new Color(1f, 1f, 1f, 0f);
                            image.raycastTarget = false;

                            var rect = healObj.GetComponent<RectTransform>();
                            rect.anchorMin = Vector2.zero;
                            rect.anchorMax = Vector2.one;
                            rect.offsetMin = Vector2.zero;
                            rect.offsetMax = Vector2.zero;
                            rect.pivot = new Vector2(0.5f, 0.5f);
                        }
                        else
                        {
                            Debug.LogWarning("Sprite 'Health' not found in Resources/Text2D/UI.");
                        }
                    }
                    else
                    {
                        Debug.Log("Health overlay already exists - skip creation.");
                    }

                }
                else
                {
                    Debug.LogWarning("HUD not found in UI hierarchy.");
                }
            }

            TryAssign(() => uiManager?.AssignUI(), "UIManager.AssignUI()");
            TryAssign(() => uiManager?.ApplyControlMode(uiManager?.GetJoystick()?.gameObject.activeSelf ?? false), "UIManager.ApplyControlMode()");
            TryAssign(() => fps?.AssignUI(), "FPSController.AssignUI()");
            TryAssign(() => look?.AssignReferences(), "LookController.AssignReferences()");
            TryAssign(() => grabbing?.AssignReferences(), "Grabbing.AssignReferences()");
            TryAssign(() => health?.AssignReferences(), "Health.AssignReferences()");
            TryAssign(() => hudManager?.AssignReferences(), "HUDManager.AssignReferences()");

            EditorUtility.DisplayDialog("VS Controller", "✅ References refreshed, missing components created if needed.", "OK");
        }

        private static void TryAssign(System.Action action, string label)
        {
            try
            {
                action?.Invoke();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in {label}: {ex.Message}");
            }
        }

        private const string Define = "VS_CONTROLLER_EDITORS_DISABLED";

        [MenuItem("VS Controller/Setup & Tutorials/Editor Scripts", false, 40)]
        public static void ToggleEditors()
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;

#if UNITY_2021_2_OR_NEWER
            string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group));
#else
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
#endif

            var list = defines
                .Split(';')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            bool enabled = !list.Contains(Define);

            if (enabled)
                list.Add(Define);
            else
                list.Remove(Define);

            string result = string.Join(";", list.Distinct());

#if UNITY_2021_2_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group), result);
#else
        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, result);
#endif

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("VS Controller/Setup & Tutorials/Editor Scripts", true)]
        private static bool ValidateToggleEditors()
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;

#if UNITY_2021_2_OR_NEWER
            string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group));
#else
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
#endif

            bool enabled = !defines.Split(';').Contains(Define);

            Menu.SetChecked("VS Controller/Setup & Tutorials/Editor Scripts", enabled);

            return true;
        }

        [MenuItem("VS Controller/Setup & Tutorials/Documentation", false, 50)]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://sites.google.com/view/vips-studio/vs-controller");
        }

        [MenuItem("VS Controller/Setup & Tutorials/About", false, 60)]
        public static void OpenAbout()
        {
            AboutWindow.ShowWindow();
        }

        public class AboutWindow : EditorWindow
        {
            private static Texture2D logoMain;
            private static GUIStyle linkStyle;
            private static int logoClickCount = 0;
            private static double lastClickTime = 0;
            private static bool showSecret = false;

            public static void ShowWindow()
            {
                AboutWindow window = GetWindow<AboutWindow>("About VS Controller");
                window.minSize = new Vector2(320, 460);
                logoMain = Resources.Load<Texture2D>("Text2D/UI/vs_controller");
            }

            // About menu
            private void OnGUI()
            {
                GUILayout.Space(10);

                DrawCenteredLogo(logoMain);

                GUILayout.Label("VS Controller", EditorStyles.boldLabel);
                GUILayout.Label("Version: 4.4.1", EditorStyles.label);

                GUILayout.Space(10);
                GUILayout.Label("Resources & Links", EditorStyles.boldLabel);

                DrawLink("Official Website", "https://sites.google.com/view/vips-studio");
                DrawLink("YouTube", "https://www.youtube.com/@vips_studio_official");
                DrawLink("Asset Store", "https://assetstore.unity.com/publishers/120739?preview=1");

                if (showSecret)
                {
                    GUILayout.Space(20);
                    EditorGUILayout.HelpBox("Good luck with your project :)", MessageType.Info);
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label("© 2026 Vip's Studio", EditorStyles.centeredGreyMiniLabel);
            }

            private void DrawCenteredLogo(Texture2D logo, float size = 128)
            {
                if (logo == null) return;

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                Rect logoRect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
                GUI.DrawTexture(logoRect, logo, ScaleMode.ScaleToFit);

                Event e = Event.current;
                if (e.type == EventType.MouseDown && logoRect.Contains(e.mousePosition))
                {
                    double currentTime = EditorApplication.timeSinceStartup;
                    if (currentTime - lastClickTime < 1.5f)
                        logoClickCount++;
                    else
                        logoClickCount = 1;

                    lastClickTime = currentTime;

                    if (logoClickCount >= 1)
                    {
                        showSecret = true;
                        Repaint();
                    }
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            private void DrawLink(string label, string url)
            {
                if (linkStyle == null)
                {
                    linkStyle = new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = new Color(0.3f, 0.6f, 1f) },
                        hover = { textColor = Color.cyan }
                    };
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label("•", GUILayout.Width(10));
                if (GUILayout.Button(label, linkStyle))
                {
                    Application.OpenURL(url);
                }
                GUILayout.EndHorizontal();
            }
        }

        // Spawning menu
        private void OnGUI()
        {
            GUILayout.Space(5);
            GUILayout.Label("VS Controller Setup", EditorStyles.boldLabel);

            EditorGUILayout.Space(10);
            GUILayout.Label("General Options", EditorStyles.boldLabel);
            useMobileControl = DrawLabeledToggle("Use Mobile Controls", useMobileControl);
            spawnPlayer = DrawLabeledToggle("Spawn Controller", spawnPlayer);
            spawnUI = DrawLabeledToggle("Spawn UI", spawnUI);
            spawnMechanismManager = DrawLabeledToggle("Spawn Mechanism Manager", spawnMechanismManager);

            EditorGUILayout.Space(10);
            GUILayout.Label("Optional Systems", EditorStyles.boldLabel);
            addHealth = DrawLabeledToggle("Include Health System", addHealth);
            addGrabbing = DrawLabeledToggle("Include Grabbing System", addGrabbing);

            GUILayout.Space(20);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fixedHeight = 30;
            buttonStyle.fontStyle = FontStyle.Bold;

            if (GUILayout.Button("Create Player Controller", buttonStyle))
            {
                CreatePlayerController();
                Close();
            }
        }

        private bool DrawLabeledToggle(string label, bool value, float spacing = 10f)
        {
            GUILayout.BeginHorizontal();
            value = GUILayout.Toggle(value, "", GUILayout.Width(20));
            GUILayout.Space(spacing);
            GUILayout.Label(label, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            return value;
        }

        private void CreatePlayerController()
        {
            if (!spawnPlayer && !spawnUI)
            {
                EditorUtility.DisplayDialog("Nothing to spawn", "Enable at least 'Spawn Player' or 'Spawn UI'", "OK");
                return;
            }

            GameObject player = null;
            Health health = null;
            Grabbing grabbing = null;
            FPSController fps = null;

            GameObject controllerScriptsObj = null;

            // 1) EventSystem
#if UNITY_2022_1_OR_NEWER
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
#else
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
#endif
            {
                GameObject es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
                Debug.Log("EventSystem created.");
            }

            // 2) Audio Source
#if UNITY_2022_1_OR_NEWER
            if (FindAnyObjectByType<AudioSource>() == null)
#else
            if (FindObjectOfType<AudioSource>() == null)
#endif
            {
                GameObject audioObj = new GameObject("Audio Source");
                audioObj.AddComponent<AudioSource>();
            }

            // 3) Controller Scripts (HUDManager / MechanismManager)
            if (spawnMechanismManager || spawnPlayer)
            {
#if UNITY_2022_1_OR_NEWER
                if (FindAnyObjectByType<HUDManager>() == null || FindAnyObjectByType<MechanismManager>() == null)
#else
                if (FindObjectOfType<HUDManager>() == null || FindObjectOfType<MechanismManager>() == null)
#endif
                {
                    controllerScriptsObj = GameObject.Find("Controller Scripts");
                    if (controllerScriptsObj == null)
                    {
                        controllerScriptsObj = new GameObject("Controller Scripts");
                    }

#if UNITY_2022_1_OR_NEWER
                    if (FindAnyObjectByType<HUDManager>() == null)
#else
                    if (FindObjectOfType<HUDManager>() == null)
#endif
                    {
                        controllerScriptsObj.AddComponent<HUDManager>();
                        Debug.Log("HUDManager created.");
                    }

#if UNITY_2022_1_OR_NEWER
                    if (spawnMechanismManager && FindAnyObjectByType<MechanismManager>() == null)
#else
                    if (spawnMechanismManager && FindObjectOfType<MechanismManager>() == null)
#endif
                    {
                        controllerScriptsObj.AddComponent<MechanismManager>();
                        Debug.Log("MechanismManager created.");
                    }
                    else if (spawnMechanismManager)
                    {
                        EditorUtility.DisplayDialog("Attention", "MechanismManager already exists in the scene", "OK");
                    }
                }
            }

            // 4) Respawn Point (if needed)
#if UNITY_2022_1_OR_NEWER
            if (spawnPlayer && FindAnyObjectByType<RespawnPoint>() == null)
#else
            if (spawnPlayer && FindObjectOfType<RespawnPoint>() == null)
#endif
            {
                GameObject respawn = new GameObject("Respawn Point");
                respawn.transform.position = new Vector3(0f, 3.25f, 0f);
                respawn.AddComponent<RespawnPoint>();
            }

            // 5) PlayerController
            if (spawnPlayer)
            {
                GameObject prefab = Resources.Load<GameObject>("Prefabs/Camera&Controller");
                if (prefab == null)
                {
                    Debug.LogError("PlayerController.prefab not found in Resources/Prefabs.");
                    return;
                }

                player = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                player.name = "PlayerController";

                grabbing = player.GetComponent<Grabbing>();
                if (!addGrabbing && grabbing != null)
                {
                    DestroyImmediate(grabbing, true);
                    grabbing = null;
                }

                health = player.GetComponent<Health>();
                if (!addHealth && health != null)
                {
                    DestroyImmediate(health, true);
                    health = null;
                }

                if (addHealth && health == null)
                {
                    health = player.AddComponent<Health>();
                }

                fps = player.GetComponent<FPSController>();
                if (fps != null)
                {
                    fps.useMobileControls = useMobileControl;
                }
            }

            // 6) UI
            GameObject ui = null;
            if (spawnUI)
            {
                GameObject uiPrefab = Resources.Load<GameObject>("Prefabs/UI");
                if (uiPrefab != null)
                {
                    ui = PrefabUtility.InstantiatePrefab(uiPrefab) as GameObject;
                    ui.name = "UI";

                    Transform hud = ui.transform.Find("HUD");
                    if (hud == null)
                    {
                        Debug.LogWarning("HUD not found in UI.");
                    }

                    if (addHealth && hud != null)
                    {
                        Transform hpBar = hud.Find("HP_bar");
                        if (hpBar != null)
                        {
                            hpBar.gameObject.SetActive(true);
                        }
                        else
                        {
                            Debug.LogWarning("HP_bar not found in UI/HUD.");
                        }

                        GameObject bloodObj = new GameObject("Blood", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                        bloodObj.transform.SetParent(hud, false);
                        bloodObj.transform.SetSiblingIndex(0);
                        bloodObj.SetActive(false);

                        Image bloodImage = bloodObj.GetComponent<Image>();
                        Sprite bloodSprite = Resources.Load<Sprite>("Text2D/UI/Blood");
                        if (bloodSprite != null)
                        {
                            bloodImage.sprite = bloodSprite;
                            bloodImage.color = new Color(1f, 1f, 1f, 0f);
                            bloodImage.raycastTarget = false;

                            RectTransform rect = bloodObj.GetComponent<RectTransform>();
                            rect.anchorMin = Vector2.zero;
                            rect.anchorMax = Vector2.one;
                            rect.offsetMin = Vector2.zero;
                            rect.offsetMax = Vector2.zero;
                            rect.pivot = new Vector2(0.5f, 0.5f);
                        }
                        else
                        {
                            Debug.LogWarning("Sprite 'Blood' not found in Resources/Text2D/UI.");
                        }

                        GameObject healObj = new GameObject("Health", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                        healObj.transform.SetParent(hud, false);
                        healObj.transform.SetSiblingIndex(1);
                        healObj.SetActive(false);

                        Image healImage = healObj.GetComponent<Image>();
                        Sprite healSprite = Resources.Load<Sprite>("Text2D/UI/Health");
                        if (healSprite != null)
                        {
                            healImage.sprite = healSprite;
                            healImage.color = new Color(1f, 1f, 1f, 0f);
                            healImage.raycastTarget = false;

                            RectTransform rect = healObj.GetComponent<RectTransform>();
                            rect.anchorMin = Vector2.zero;
                            rect.anchorMax = Vector2.one;
                            rect.offsetMin = Vector2.zero;
                            rect.offsetMax = Vector2.zero;
                            rect.pivot = new Vector2(0.5f, 0.5f);
                        }
                        else
                        {
                            Debug.LogWarning("Sprite 'Health' not found in Resources/Text2D/UI.");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("UI prefab not found in Resources/Prefabs.");
                }
            }

            // Assign references
            if (player != null)
            {
                UIManager uiManager = player.GetComponent<UIManager>();
                if (uiManager != null)
                {
                    uiManager.AssignUI();
                }

                if (fps != null)
                {
                    fps.AssignUI();
                }

                var look = player.GetComponentInChildren<LookController>();
                if (look != null)
                {
                    look.AssignReferences();
                }

                if (grabbing != null)
                {
                    grabbing.AssignReferences();
                }

                if (health != null)
                {
                    health.AssignReferences();
                }

                Selection.activeGameObject = player;
            }

#if UNITY_2022_1_OR_NEWER
            HUDManager hudMgr = FindAnyObjectByType<HUDManager>();
#else
            HUDManager hudMgr = FindObjectOfType<HUDManager>();
#endif
            if (hudMgr != null)
            {
                hudMgr.AssignReferences();
            }
        }

        // Spawning objects near camera
        private static Vector3 GetSpawnPosition()
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null && sceneView.camera != null)
            {
                return sceneView.camera.transform.position + sceneView.camera.transform.forward * 5f;
            }
            return Vector3.zero;
        }

        /// <summary>
        /// VS Controller/Create/...
        /// </summary>

        [MenuItem("VS Controller/Create/Teleport")]
        private static void CreateTeleportMarker()
        {
            GameObject root = new GameObject("Teleport Marker");
            root.transform.position = GetSpawnPosition();

            TeleportMarker marker = root.AddComponent<TeleportMarker>();

            GameObject from = new GameObject("From");
            from.transform.SetParent(root.transform);
            from.transform.localPosition = Vector3.zero;

            BoxCollider fromCollider = from.AddComponent<BoxCollider>();
            fromCollider.isTrigger = true;

            GameObject to = new GameObject("To");
            to.transform.SetParent(root.transform);
            to.transform.localPosition = Vector3.forward * 3f;

            marker.fromPoints = new Transform[] { from.transform };
            marker.toPoint = to.transform;

            Selection.activeGameObject = root;
        }

        [MenuItem("VS Controller/Create/Water")]
        private static void CreateWaterTrigger()
        {
            GameObject root = new GameObject("Water");
            root.transform.position = GetSpawnPosition();
            root.AddComponent<Water>();
            Selection.activeGameObject = root;
        }

        [MenuItem("VS Controller/Surface/Mud")]
        private static void CreateMudSurface()
        {
            GameObject root = new GameObject("Mud");
            root.transform.position = GetSpawnPosition();
            root.AddComponent<Mud>();
            Selection.activeGameObject = root;
        }

        [MenuItem("VS Controller/Surface/Ice")]
        private static void CreateIceSurface()
        {
            GameObject root = new GameObject("Ice");
            root.transform.position = GetSpawnPosition();
            root.AddComponent<Ice>();
            Selection.activeGameObject = root;
        }

        [MenuItem("VS Controller/Create/BunnyHop")]
        private static void CreateBunnyHopTrigger()
        {
            GameObject root = new GameObject("BunnyHop");
            root.transform.position = GetSpawnPosition();
            root.AddComponent<BunnyHop>();
            Selection.activeGameObject = root;
        }

        [MenuItem("VS Controller/Create/EventsTrigger")]
        private static void CreateEventsTrigger()
        {
            GameObject root = new GameObject("EventsTrigger");
            root.transform.position = GetSpawnPosition();
            root.AddComponent<EventsTrigger>();
            Selection.activeGameObject = root;
        }

        [MenuItem("VS Controller/Create/Ladder")]
        private static void CreateLadderTrigger()
        {
            GameObject ladderObject = new GameObject("Ladder");
            ladderObject.transform.position = GetSpawnPosition();
            ladderObject.AddComponent<Ladder>();
            Selection.activeGameObject = ladderObject;
        }

        [MenuItem("VS Controller/Create/Respawn Point")]
        private static void CreateRespawnPoint()
        {
            GameObject respawnObject = new GameObject("Respawn Point");
            respawnObject.transform.position = GetSpawnPosition();
            respawnObject.AddComponent<RespawnPoint>();
            Selection.activeGameObject = respawnObject;
        }

        /// <summary>
        /// VS Controller/Add/...
        /// </summary>

        [MenuItem("VS Controller/Add/Collectable Item")]
        private static void AddCollectableItemObject()
        {
            GameObject selectedObject = Selection.activeGameObject;

            if (selectedObject == null)
            {
                EditorUtility.DisplayDialog("Warning", "Please select an object in the scene.", "OK");
                return;
            }

            CollectableItem item = selectedObject.AddComponent<CollectableItem>();
            item.InitList(selectedObject.transform.localScale);

            EditorGUIUtility.PingObject(selectedObject);
            Debug.Log("Collectable Item script added to object: " + selectedObject.name);

            Collider[] colliders = selectedObject.GetComponents<Collider>();

            bool hasAnyCollider = colliders.Length > 0;
            bool hasNonTriggerCollider = false;

            foreach (var col in colliders)
            {
                if (col != null && !col.isTrigger)
                {
                    hasNonTriggerCollider = true;
                    break;
                }
            }

            if (!hasAnyCollider || !hasNonTriggerCollider)
            {
                selectedObject.AddComponent<BoxCollider>();
            }
        }


        [MenuItem("VS Controller/Add/Movable Object")]
        private static void AddMovabletoSelectedObject()
        {
            GameObject selectedObject = Selection.activeGameObject;
            if (selectedObject != null)
            {
                MovableObject door = selectedObject.AddComponent<MovableObject>();

                EditorGUIUtility.PingObject(selectedObject);
                Debug.Log("Movable Object script added to object: " + selectedObject.name);
            }
            else
            {
                EditorUtility.DisplayDialog("Warning", "Please select an object in the scene.", "ОК");
            }
        }

        [MenuItem("VS Controller/Add/Floor Button")]
        private static void AddButtonFloorToSelectedObject()
        {
            GameObject selectedObject = Selection.activeGameObject;

            if (selectedObject == null)
            {
                EditorUtility.DisplayDialog("Warning", "Please select an object in the scene.", "OK");
                return;
            }

            FloorButton floorButton = selectedObject.GetComponent<FloorButton>();
            Debug.Log("Floor Button script added to object: " + selectedObject.name);

            if (floorButton == null)
            {
                floorButton = selectedObject.AddComponent<FloorButton>();
            }

            bool hasTriggerCollider = false;
            Collider[] colliders = selectedObject.GetComponents<Collider>();

            foreach (var col in colliders)
            {
                if (col != null && col.isTrigger)
                {
                    hasTriggerCollider = true;
                    break;
                }
            }

            if (!hasTriggerCollider)
            {
                BoxCollider box = selectedObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
            }

            EditorGUIUtility.PingObject(selectedObject);
        }

        [MenuItem("VS Controller/Add/Manual Button")]
        private static void AddButtonManualToSelectedObject()
        {
            GameObject selectedObject = Selection.activeGameObject;

            if (selectedObject == null)
            {
                EditorUtility.DisplayDialog("Warning", "Please select an object in the scene.", "OK");
                return;
            }

            ManualButton manualButton = selectedObject.GetComponent<ManualButton>();
            Debug.Log("Manual Button script added to object: " + selectedObject.name);

            if (manualButton == null)
            {
                manualButton = selectedObject.AddComponent<ManualButton>();
            }

            bool hasTriggerCollider = false;
            Collider[] colliders = selectedObject.GetComponents<Collider>();

            foreach (var col in colliders)
            {
                if (col != null && col.isTrigger)
                {
                    hasTriggerCollider = true;
                    break;
                }
            }

            if (!hasTriggerCollider)
            {
                BoxCollider box = selectedObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
            }

            EditorGUIUtility.PingObject(selectedObject);
        }
    }
}

