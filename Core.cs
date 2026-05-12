using MelonLoader;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

[assembly: MelonInfo(typeof(StumbleMod.Core), "StumbleTooHardGuys", "1.4.0", "Martin", null)] 
[assembly: MelonGame("Kitka Games", "Stumble Guys")]

namespace StumbleMod
{
    public class Core : MelonMod
    {
        // ── Window state ─────────────────────────────────────────────────────
        private bool showMenu   = false;
        private bool mouseLocked = false;

        // Main nav tabs
        private int  mainTab    = 0;
        private string[] mainTabs = { "Server-side", "Party", "Map-Specific", "Alts Manager", "Misc" };

        // Sub-panel rects
        private Rect navRect     = new Rect(5,  20, 110, 160);
        private Rect panelRect   = new Rect(120, 20, 220, 480);
        private Rect miscRect    = new Rect(880, 270, 180, 180);

        // ── Server-side state ────────────────────────────────────────────────
        private string serverStatus   = "";
        private string currentServer  = "Normal";
        private bool   showAdvanced   = false;
        private string usernameInput  = "Player";
        private bool   editUsername   = false;
        private string tagColor       = "None";

        // ── Party state ──────────────────────────────────────────────────────
        private bool overrideMap  = false;
        private bool antiKick     = false;
        private bool fakeHost     = false;

        private string[] maps = {
            "Honey Drop","Lava 2","Tile Fall","Laser","Rocket",
            "Block Dash","Super Slide","Bombardment",
            "Lost Temple","Space Race","Pivot Push"
        };

        // ── Map-Specific state ───────────────────────────────────────────────
        private bool revealTiles  = false;
        private bool revealFinish = false;

        // ── Alts Manager state ───────────────────────────────────────────────
        private List<string> altAccounts = new List<string>();
        private string newAltInput = "";

        // ── Misc state ───────────────────────────────────────────────────────
        private bool  textLocalization = true;
        private float cameraFov        = 60f;

        // ── Unity refs ───────────────────────────────────────────────────────
        private GameObject player;
        private Rigidbody  rb;
        private Camera     cam;

        // ── Styles ───────────────────────────────────────────────────────────
        private bool       stylesReady = false;
        private GUIStyle   stylePanel, stylePanelDark, styleBtn, styleBtnColor;
        private GUIStyle   styleTitle, styleLabel, styleLabelTiny, styleToggle;
        private GUIStyle   styleTextField, styleBtnSmall;

        // ────────────────────────────────────────────────────────────────────
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("[StumbleMod v1.4] Press F1 to unlock/lock mouse. INSERT = menu.");
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Insert))
                showMenu = !showMenu;

            if (Input.GetKeyDown(KeyCode.F1))
            {
                mouseLocked = !mouseLocked;
                Cursor.lockState = mouseLocked ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible   = !mouseLocked;
            }

            if (player == null) player = GameObject.FindWithTag("Player");
            if (player != null && rb == null) rb = player.GetComponent<Rigidbody>();
            if (cam == null) cam = Camera.main;

            // Live camera FOV
            if (cam != null) cam.fieldOfView = cameraFov;

            // Anti-kick: periodically send a keep-alive movement nudge
            if (antiKick && rb != null && Time.frameCount % 180 == 0)
                rb.AddForce(new Vector3(UnityEngine.Random.Range(-0.05f, 0.05f), 0f,
                                        UnityEngine.Random.Range(-0.05f, 0.05f)), ForceMode.Impulse);

            // Map hacks
            if (revealTiles)  RevealTileFall();
            if (revealFinish) RevealSpaceRaceFinish();
        }

        // ────────────────────────────────────────────────────────────────────
        public override void OnGUI()
        {
            if (!showMenu) return;
            BuildStyles();

            // Header hint
            GUI.Label(new Rect(4, 2, 500, 18),
                "StumbleTooHardGuys (v1.4) [made by notpies and McMlstzYT] | Press F1 to Unlock and lock the mouse.",
                styleLabelTiny);

            // ── Navigation panel ──
            GUILayout.BeginArea(navRect);
            GUILayout.BeginVertical(stylePanel);
            for (int i = 0; i < mainTabs.Length; i++)
            {
                GUIStyle s = (mainTab == i) ? styleBtnSmall : styleBtn;
                if (GUILayout.Button(mainTabs[i], s))
                    mainTab = i;
                GUILayout.Space(2);
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();

            // ── Main content panel ──
            GUILayout.BeginArea(panelRect);
            GUILayout.BeginVertical(stylePanel);
            switch (mainTab)
            {
                case 0: DrawServerSide(); break;
                case 1: DrawParty();      break;
                case 2: DrawMapSpecific(); break;
                case 3: DrawAltsManager(); break;
                case 4: DrawMisc();       break;
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();

            // ── Misc floater (top-right like in screenshot) ──
            if (mainTab == 4)
            {
                GUILayout.BeginArea(miscRect);
                DrawMiscPanel();
                GUILayout.EndArea();
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  SERVER-SIDE
        // ════════════════════════════════════════════════════════════════════
        private void DrawServerSide()
        {
            GUILayout.Label("Server-side", styleTitle);
            DrawDiv();

            if (GUILayout.Button("Simulate Start Maintenance", styleBtn))
                serverStatus = "Maintenance START simulated.";

            if (GUILayout.Button("Simulate End Maintenance", styleBtn))
                serverStatus = "Maintenance END simulated.";

            GUILayout.Space(4);
            if (GUILayout.Button("Change Username (Tags included!)", styleBtn))
                editUsername = !editUsername;

            if (editUsername)
            {
                usernameInput = GUILayout.TextField(usernameInput, 24, styleTextField);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply", styleBtnSmall))
                {
                    ApplyUsername(usernameInput);
                    editUsername  = false;
                    serverStatus  = "Username changed to: " + usernameInput;
                }
                GUILayout.EndHorizontal();
            }

            // Tag color row
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Red",  styleBtnColor)) tagColor = "Red";
            if (GUILayout.Button("Green",styleBtnColor)) tagColor = "Green";
            if (GUILayout.Button("Blue", styleBtnColor)) tagColor = "Blue";
            if (GUILayout.Button("Mod",  styleBtnColor)) tagColor = "Mod";
            GUILayout.EndHorizontal();
            if (tagColor != "None")
                GUILayout.Label("Tag: " + tagColor, styleLabelTiny);

            DrawDiv();

            if (GUILayout.Button("100 Gems [doesnt work everytime]", styleBtn))
                GrantCurrency(0, 100);

            if (GUILayout.Button("Free Skins [doesnt work everytime]", styleBtn))
                TryUnlockCategory("skin", "costume");

            if (GUILayout.Button("Crowns [doesnt work everytime]", styleBtn))
                GrantCurrency(100, 0);

            if (GUILayout.Button("Trophies [doesnt work everytime]", styleBtn))
                GrantCurrency(200, 0);

            DrawDiv();

            if (GUILayout.Button("Free Username Change", styleBtn))
                editUsername = !editUsername;

            // Advanced
            GUILayout.Space(4);
            if (GUILayout.Button((showAdvanced ? "▲ " : "▼ ") + "Advanced", styleLabel))
                showAdvanced = !showAdvanced;

            if (showAdvanced)
            {
                if (GUILayout.Button("Switch to Beta Server", styleBtnSmall))
                    serverStatus = "Switched to Beta Server.";
            }

            GUILayout.FlexibleSpace();
            DrawDiv();

            if (GUILayout.Button("Switch to Normal Server", styleBtn))
            {
                currentServer = "Normal";
                serverStatus  = "Normal Server | T:50560 G:23720";
                GrantCurrency(50560, 23720);
            }

            GUILayout.Space(2);

            if (GUILayout.Button("Switch to Private Server", styleBtn))
            {
                currentServer = "Private";
                string leg = PickRandom(LegendarySkins);
                serverStatus  = "Private Server | Legendary: " + leg + " | T:50560 G:23720";
                GrantCurrency(50560, 23720);
            }

            if (serverStatus != "")
            {
                GUILayout.Space(2);
                GUILayout.Label(serverStatus, styleLabelTiny);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  PARTY
        // ════════════════════════════════════════════════════════════════════
        private void DrawParty()
        {
            GUILayout.Label("Party", styleTitle);
            DrawDiv();

            if (GUILayout.Button("Kick All", styleBtn))
                InvokeGameMethod("kick", "kickAll", "kickall");

            if (GUILayout.Button("Become Fake Host", styleBtn))
            {
                fakeHost = true;
                InvokeGameMethod("fakeHost", "becomeHost", "setHost");
            }

            if (GUILayout.Button("Steal/Become Host", styleBtn))
                InvokeGameMethod("stealHost", "becomeHost", "setHost");

            overrideMap = GUILayout.Toggle(overrideMap, "  Override Map (Host Only)", styleToggle);

            DrawDiv();
            GUILayout.Label("New Map (NO BOTS):", styleLabel);
            GUILayout.Space(2);

            foreach (string map in maps)
            {
                if (GUILayout.Button(map, styleBtn))
                    LoadMap(map);
            }

            DrawDiv();

            if (GUILayout.Button("Toggle Party Creator", styleBtn))
                InvokeGameMethod("partyCreator", "toggleParty", "createParty");

            if (GUILayout.Button("Start Game", styleBtn))
                InvokeGameMethod("startGame", "startMatch", "beginGame");

            if (GUILayout.Button("Remove Bots", styleBtn))
                InvokeGameMethod("removeBot", "deleteBots", "clearBots");

            antiKick = GUILayout.Toggle(antiKick, "  Anti-Kick (Buggy)", styleToggle);

            if (GUILayout.Button("Join Random Party", styleBtn))
                InvokeGameMethod("joinParty", "joinRandom", "randomParty");
        }

        // ════════════════════════════════════════════════════════════════════
        //  MAP-SPECIFIC
        // ════════════════════════════════════════════════════════════════════
        private void DrawMapSpecific()
        {
            GUILayout.Label("Map-Specific", styleTitle);
            DrawDiv();

            GUILayout.Label("Tile Fall", styleLabel);
            if (GUILayout.Button("Reveal all Tiles", styleBtn))
            {
                revealTiles = true;
                RevealTileFall();
            }

            DrawDiv();
            GUILayout.Label("Space Race", styleLabel);
            if (GUILayout.Button("Reveal Finish", styleBtn))
            {
                revealFinish = true;
                RevealSpaceRaceFinish();
            }

            DrawDiv();
            GUILayout.Label("General", styleLabel);

            if (GUILayout.Button("Show All Checkpoints", styleBtn))
                ToggleRenderers("checkpoint", "finish", "goal");

            if (GUILayout.Button("Highlight Finish Line", styleBtn))
                ToggleRenderers("finish", "end", "goal");

            if (GUILayout.Button("Disable Obstacles", styleBtn))
                SetObstacleState(false);

            if (GUILayout.Button("Enable Obstacles", styleBtn))
                SetObstacleState(true);
        }

        // ════════════════════════════════════════════════════════════════════
        //  ALTS MANAGER
        // ════════════════════════════════════════════════════════════════════
        private void DrawAltsManager()
        {
            GUILayout.Label("Alts Manager", styleTitle);
            DrawDiv();

            GUILayout.Label("Add Alt Account:", styleLabel);
            newAltInput = GUILayout.TextField(newAltInput, 24, styleTextField);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Alt", styleBtnSmall))
            {
                if (!string.IsNullOrWhiteSpace(newAltInput))
                {
                    altAccounts.Add(newAltInput.Trim());
                    newAltInput = "";
                }
            }
            if (GUILayout.Button("Clear All", styleBtnSmall))
                altAccounts.Clear();
            GUILayout.EndHorizontal();

            DrawDiv();
            GUILayout.Label($"Saved Alts: ({altAccounts.Count})", styleLabel);

            for (int i = 0; i < altAccounts.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(altAccounts[i], styleLabelTiny);
                if (GUILayout.Button("Use", styleBtnSmall, GUILayout.Width(40)))
                {
                    ApplyUsername(altAccounts[i]);
                    serverStatus = "Switched to alt: " + altAccounts[i];
                }
                if (GUILayout.Button("X", styleBtnSmall, GUILayout.Width(24)))
                    altAccounts.RemoveAt(i);
                GUILayout.EndHorizontal();
            }

            DrawDiv();
            if (GUILayout.Button("Grant All Alts 50560 T + 23720 G", styleBtn))
            {
                foreach (var _ in altAccounts)
                    GrantCurrency(50560, 23720);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  MISC (tab content)
        // ════════════════════════════════════════════════════════════════════
        private void DrawMisc()
        {
            GUILayout.Label("Miscellaneous", styleTitle);
            DrawDiv();
            GUILayout.Label("(Panel shown separately →)", styleLabelTiny);
            DrawDiv();

            GUILayout.Label("Localizations:", styleLabel);
            if (GUILayout.Button("Toggle Text Localization", styleBtn))
            {
                textLocalization = !textLocalization;
                ToggleLocalization(textLocalization);
            }
            if (GUILayout.Button("Open Debugger", styleBtn))
                OpenDebugger();

            if (GUILayout.Button("Leave Match", styleBtn))
                LeaveMatch();

            DrawDiv();
            GUILayout.Label($"Camera FOV: {cameraFov:F0}", styleLabel);
            cameraFov = GUILayout.HorizontalSlider(cameraFov, 30f, 120f);
        }

        // ── Misc floater panel (right side like screenshot) ───────────────
        private void DrawMiscPanel()
        {
            GUILayout.BeginVertical(stylePanelDark);
            GUILayout.Label("Miscellaneous", styleTitle);
            DrawDiv();
            GUILayout.Label("Localizations:", styleLabel);
            if (GUILayout.Button("Toggle Text Localization", styleBtn))
                ToggleLocalization(textLocalization = !textLocalization);
            if (GUILayout.Button("Open Debugger", styleBtn))
                OpenDebugger();
            if (GUILayout.Button("Leave Match", styleBtn))
                LeaveMatch();
            DrawDiv();
            GUILayout.Label($"Camera FOV: {cameraFov:F0}", styleLabel);
            cameraFov = GUILayout.HorizontalSlider(cameraFov, 30f, 120f);
            GUILayout.EndVertical();
        }

        // ════════════════════════════════════════════════════════════════════
        //  Game action implementations
        // ════════════════════════════════════════════════════════════════════

        private void LoadMap(string mapName)
        {
            LoggerInstance.Msg("[StumbleMod] Loading map: " + mapName);
            try { UnityEngine.SceneManagement.SceneManager.LoadScene(mapName); } catch { }
            InvokeGameMethod("loadMap", "changeMap", "switchMap");
        }

        private void RevealTileFall()
        {
            try
            {
                var tiles = GameObject.FindGameObjectsWithTag("Tile");
                foreach (var t in tiles)
                {
                    var r = t.GetComponent<Renderer>();
                    if (r != null)
                    {
                        Color c = r.material.color;
                        c.a = 1f;
                        r.material.color = c;
                    }
                }
            }
            catch { }
        }

        private void RevealSpaceRaceFinish()
        {
            ToggleRenderers("finish", "goal", "end", "checkpoint");
        }

        private void ToggleRenderers(params string[] keywords)
        {
            try
            {
                var all = UnityEngine.Object.FindObjectsOfType<GameObject>();
                foreach (var go in all)
                {
                    string n = go.name.ToLower();
                    foreach (var kw in keywords)
                    {
                        if (n.Contains(kw))
                        {
                            var r = go.GetComponent<Renderer>();
                            if (r != null) r.enabled = true;
                        }
                    }
                }
            }
            catch { }
        }

        private void SetObstacleState(bool active)
        {
            try
            {
                string[] obstacleNames = {
                    "obstacle","hammer","spinner","bumper","platform","trap","saw","fan"
                };
                var all = UnityEngine.Object.FindObjectsOfType<GameObject>();
                foreach (var go in all)
                {
                    string n = go.name.ToLower();
                    foreach (var kw in obstacleNames)
                        if (n.Contains(kw)) go.SetActive(active);
                }
            }
            catch { }
        }

        private void GrantCurrency(int tokens, int gems)
        {
            string[] tokenKeys = { "Tokens","StumbleTokens","Coins","Currency","Gold","Crowns","Trophies" };
            string[] gemKeys   = { "Gems","Diamonds","Premium" };

            foreach (var k in tokenKeys) if (tokens > 0) PlayerPrefs.SetInt(k, PlayerPrefs.GetInt(k, 0) + tokens);
            foreach (var k in gemKeys)   if (gems   > 0) PlayerPrefs.SetInt(k, PlayerPrefs.GetInt(k, 0) + gems);
            PlayerPrefs.Save();

            SetReflectionNumeric("token",  tokens);
            SetReflectionNumeric("gem",    gems);
            SetReflectionNumeric("coin",   tokens);
            SetReflectionNumeric("crown",  tokens);
            SetReflectionNumeric("trophy", tokens);
            LoggerInstance.Msg($"[StumbleMod] Grant: T+{tokens} G+{gems}");
        }

        private void ApplyUsername(string name)
        {
            string[] keys = { "Username","PlayerName","DisplayName","Nickname","Name" };
            foreach (var k in keys) PlayerPrefs.SetString(k, name);
            PlayerPrefs.Save();
            SetReflectionString("username", name);
            SetReflectionString("playerName", name);
            SetReflectionString("displayName", name);
            LoggerInstance.Msg("[StumbleMod] Username: " + name);
        }

        private void ToggleLocalization(bool state)
        {
            SetReflectionBool("localization", state);
            SetReflectionBool("textLocalization", state);
        }

        private void OpenDebugger()
        {
            InvokeGameMethod("openDebug","showDebug","debugger","console");
        }

        private void LeaveMatch()
        {
            InvokeGameMethod("leaveMatch","exitMatch","leaveGame","disconnect");
            try { UnityEngine.SceneManagement.SceneManager.LoadScene(0); } catch { }
        }

        // ════════════════════════════════════════════════════════════════════
        //  Reflection helpers
        // ════════════════════════════════════════════════════════════════════

        private void InvokeGameMethod(params string[] methodNames)
        {
            try
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (Type type in asm.GetTypes())
                    {
                        foreach (string target in methodNames)
                        {
                            foreach (MethodInfo m in type.GetMethods(
                                BindingFlags.Public | BindingFlags.NonPublic |
                                BindingFlags.Static | BindingFlags.Instance))
                            {
                                if (!m.Name.ToLower().Contains(target.ToLower())) continue;
                                if (m.GetParameters().Length != 0) continue;
                                try
                                {
                                    object inst = null;
                                    if (!m.IsStatic)
                                    {
                                        FieldInfo fi = type.GetField("Instance",
                                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                                        if (fi != null) inst = fi.GetValue(null);
                                        if (inst == null) continue;
                                    }
                                    m.Invoke(inst, null);
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private string TryUnlockCategory(params string[] keywords)
        {
            int count = 0;
            try
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (Type type in asm.GetTypes())
                    {
                        string tl = type.Name.ToLower();
                        bool match = false;
                        foreach (string kw in keywords) if (tl.Contains(kw)) { match = true; break; }
                        if (!match) continue;

                        foreach (FieldInfo f in type.GetFields(
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Static | BindingFlags.Instance))
                        {
                            if (f.FieldType != typeof(bool)) continue;
                            string fl = f.Name.ToLower();
                            if (!fl.Contains("own") && !fl.Contains("unlock") &&
                                !fl.Contains("purchas")) continue;
                            try
                            {
                                if (f.IsStatic) { f.SetValue(null, true); count++; }
                                else
                                {
                                    foreach (var obj in UnityEngine.Object.FindObjectsOfType(type))
                                    { f.SetValue(obj, true); count++; }
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
            return count > 0 ? $"Patched {count} fields." : "No targets found.";
        }

        private void SetReflectionNumeric(string keyword, int value)
        {
            if (value == 0) return;
            try
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                    foreach (Type type in asm.GetTypes())
                        foreach (FieldInfo f in type.GetFields(
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Static | BindingFlags.Instance))
                        {
                            if (!f.Name.ToLower().Contains(keyword)) continue;
                            if (f.FieldType != typeof(int) && f.FieldType != typeof(float) &&
                                f.FieldType != typeof(long)) continue;
                            try
                            {
                                if (f.IsStatic)
                                    f.SetValue(null, Convert.ChangeType(value, f.FieldType));
                                else
                                    foreach (var obj in UnityEngine.Object.FindObjectsOfType(type))
                                        f.SetValue(obj, Convert.ChangeType(value, f.FieldType));
                            }
                            catch { }
                        }
            }
            catch { }
        }

        private void SetReflectionString(string keyword, string value)
        {
            try
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                    foreach (Type type in asm.GetTypes())
                        foreach (FieldInfo f in type.GetFields(
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Static | BindingFlags.Instance))
                        {
                            if (!f.Name.ToLower().Contains(keyword)) continue;
                            if (f.FieldType != typeof(string)) continue;
                            try
                            {
                                if (f.IsStatic) f.SetValue(null, value);
                                else
                                    foreach (var obj in UnityEngine.Object.FindObjectsOfType(type))
                                        f.SetValue(obj, value);
                            }
                            catch { }
                        }
            }
            catch { }
        }

        private void SetReflectionBool(string keyword, bool value)
        {
            try
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                    foreach (Type type in asm.GetTypes())
                        foreach (FieldInfo f in type.GetFields(
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Static | BindingFlags.Instance))
                        {
                            if (!f.Name.ToLower().Contains(keyword)) continue;
                            if (f.FieldType != typeof(bool)) continue;
                            try
                            {
                                if (f.IsStatic) f.SetValue(null, value);
                                else
                                    foreach (var obj in UnityEngine.Object.FindObjectsOfType(type))
                                        f.SetValue(obj, value);
                            }
                            catch { }
                        }
            }
            catch { }
        }

        // ════════════════════════════════════════════════════════════════════
        //  Skin data
        // ════════════════════════════════════════════════════════════════════
        private static readonly string[] LegendarySkins = {
            "Golden Champion","Rainbow Racer","Cyber Punk","Phantom Shadow",
            "Thunder Strike","Prism","Storm Chaser","Neon Night","Starlight",
            "Volcano King","Shadow Demon","Crystal Dragon","Void Walker",
            "Celestial Guardian","Blazing Phoenix"
        };

        private string PickRandom(string[] arr)
            => arr[UnityEngine.Random.Range(0, arr.Length)];

        // ════════════════════════════════════════════════════════════════════
        //  Styles
        // ════════════════════════════════════════════════════════════════════
        private void BuildStyles()
        {
            if (stylesReady) return;
            stylesReady = true;

            stylePanel = new GUIStyle(GUI.skin.box);
            stylePanel.normal.background = MakeTex(2, 2, new Color(0.12f, 0.22f, 0.35f, 0.92f));
            stylePanel.padding = new RectOffset(6, 6, 6, 6);

            stylePanelDark = new GUIStyle(stylePanel);
            stylePanelDark.normal.background = MakeTex(2, 2, new Color(0.08f, 0.16f, 0.28f, 0.95f));

            styleBtn = new GUIStyle(GUI.skin.button);
            styleBtn.fontSize  = 10;
            styleBtn.alignment = TextAnchor.MiddleCenter;
            styleBtn.normal.background  = MakeTex(2, 2, new Color(0.18f, 0.28f, 0.42f, 0.90f));
            styleBtn.hover.background   = MakeTex(2, 2, new Color(0.25f, 0.40f, 0.60f, 0.95f));
            styleBtn.active.background  = MakeTex(2, 2, new Color(0.12f, 0.20f, 0.32f, 0.95f));
            styleBtn.normal.textColor   = new Color(0.85f, 0.95f, 1f);
            styleBtn.hover.textColor    = Color.white;
            styleBtn.fixedHeight = 22;
            styleBtn.margin = new RectOffset(0, 0, 1, 1);

            styleBtnSmall = new GUIStyle(styleBtn);
            styleBtnSmall.fontSize   = 9;
            styleBtnSmall.fixedHeight = 18;

            styleBtnColor = new GUIStyle(styleBtn);
            styleBtnColor.fontSize = 9;
            styleBtnColor.fixedHeight = 18;

            styleTitle = new GUIStyle(GUI.skin.label);
            styleTitle.fontSize  = 11;
            styleTitle.fontStyle = FontStyle.Bold;
            styleTitle.alignment = TextAnchor.MiddleCenter;
            styleTitle.normal.textColor = new Color(0.8f, 0.95f, 1f);

            styleLabel = new GUIStyle(GUI.skin.label);
            styleLabel.fontSize = 10;
            styleLabel.fontStyle = FontStyle.Bold;
            styleLabel.normal.textColor = new Color(0.75f, 0.90f, 1f);

            styleLabelTiny = new GUIStyle(GUI.skin.label);
            styleLabelTiny.fontSize = 9;
            styleLabelTiny.normal.textColor = new Color(0.70f, 0.85f, 0.95f, 0.85f);
            styleLabelTiny.wordWrap = true;

            styleToggle = new GUIStyle(GUI.skin.toggle);
            styleToggle.fontSize = 10;
            styleToggle.normal.textColor  = new Color(0.85f, 0.95f, 1f);
            styleToggle.active.textColor  = Color.white;

            styleTextField = new GUIStyle(GUI.skin.textField);
            styleTextField.fontSize = 10;
            styleTextField.normal.background   = MakeTex(2, 2, new Color(0.08f, 0.15f, 0.26f, 0.95f));
            styleTextField.focused.background  = MakeTex(2, 2, new Color(0.14f, 0.24f, 0.40f, 0.95f));
            styleTextField.normal.textColor    = Color.white;
            styleTextField.focused.textColor   = Color.white;
            styleTextField.fixedHeight = 20;
            styleTextField.padding = new RectOffset(4, 4, 4, 4);
        }

        private void DrawDiv()
        {
            GUILayout.Space(3);
            Rect r = GUILayoutUtility.GetRect(panelRect.width - 12, 1);
            GUI.DrawTexture(r, MakeTex(2, 2, new Color(0.4f, 0.7f, 1f, 0.3f)));
            GUILayout.Space(3);
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var t = new Texture2D(w, h);
            var p = new Color[w * h];
            for (int i = 0; i < p.Length; i++) p[i] = col;
            t.SetPixels(p); t.Apply();
            return t;
        }
    }
} 
