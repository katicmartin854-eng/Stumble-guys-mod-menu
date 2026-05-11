using MelonLoader;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

[assembly: MelonInfo(typeof(StumbleGuysMenu.Core), "StumbleGuysMenu", "1.0.0", "SNOWY", null)]
[assembly: MelonGame("Kitka Games", "Stumble Guys")]

namespace StumbleGuysMenu
{
    public class Core : MelonMod
    {
        // Window
        private bool showMenu = false;
        private Rect windowRect = new Rect(20, 20, 270, 580);
        private Vector2 scroll = Vector2.zero;

        // Tab
        private int currentTab = 0;
        private string[] tabs = { "Movement", "Physics", "Misc", "Cosmetics" };

        // Movement
        private bool speedHack = false;
        private bool infiniteJump = false;
        private bool noClip = false;
        private float speedMultiplier = 2f;
        private float jumpForce = 15f;

        // Physics
        private bool godMode = false;
        private bool lowGravity = false;
        private float gravityScale = 0.3f;

        // Misc
        private bool antiAFK = false;

        // Cosmetics status
        private string cosmeticStatus = "Press a button to unlock.";

        // Player refs
        private GameObject player;
        private Rigidbody rb;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("[StumbleMenu] Loaded! Press INSERT to open.");
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Insert))
                showMenu = !showMenu;

            if (player == null)
            {
                player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    rb = player.GetComponent<Rigidbody>();
                    LoggerInstance.Msg("[StumbleMenu] Player found!");
                }
            }

            if (player == null) return;

            // Speed Hack
            if (speedHack && rb != null)
            {
                Vector3 vel = rb.velocity;
                vel.x *= speedMultiplier;
                vel.z *= speedMultiplier;
                rb.velocity = vel;
            }

            // Infinite Jump
            if (infiniteJump && Input.GetKeyDown(KeyCode.Space) && rb != null)
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            // God Mode
            if (godMode && rb != null && rb.velocity.y < -20f)
            {
                Vector3 v = rb.velocity;
                v.y = 0f;
                rb.velocity = v;
            }

            // Low Gravity
            Physics.gravity = lowGravity
                ? new Vector3(0f, -9.81f * gravityScale, 0f)
                : new Vector3(0f, -9.81f, 0f);

            // No Clip
            if (noClip && rb != null)
            {
                rb.useGravity = false;
                rb.velocity = Vector3.zero;
                Vector3 move = Vector3.zero;
                if (Input.GetKey(KeyCode.W)) move += player.transform.forward;
                if (Input.GetKey(KeyCode.S)) move -= player.transform.forward;
                if (Input.GetKey(KeyCode.A)) move -= player.transform.right;
                if (Input.GetKey(KeyCode.D)) move += player.transform.right;
                if (Input.GetKey(KeyCode.E)) move += Vector3.up;
                if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;
                player.transform.position += move * 10f * Time.deltaTime;
            }
            else if (rb != null)
                rb.useGravity = true;

            // Anti-AFK
            if (antiAFK && rb != null && Time.frameCount % 300 == 0)
                rb.AddForce(new Vector3(UnityEngine.Random.Range(-0.1f, 0.1f), 0f, UnityEngine.Random.Range(-0.1f, 0.1f)), ForceMode.Impulse);
        }

        public override void OnGUI()
        {
            if (!showMenu) return;
            windowRect = GUI.Window(0, windowRect, DrawMenu, "Stumble Guys Menu  |  INSERT");
        }

        private void DrawMenu(int id)
        {
            GUI.DragWindow(new Rect(0, 0, windowRect.width, 20));
            GUILayout.Space(5);

            // Tab bar
            currentTab = GUILayout.SelectionGrid(currentTab, tabs, 4);
            GUILayout.Space(8);

            scroll = GUILayout.BeginScrollView(scroll);

            switch (currentTab)
            {
                case 0: DrawMovement(); break;
                case 1: DrawPhysics(); break;
                case 2: DrawMisc(); break;
                case 3: DrawCosmetics(); break;
            }

            GUILayout.EndScrollView();

            GUILayout.Space(5);
            GUILayout.Label($"Player: {(player != null ? "Found ✓" : "Not Found")}");
        }

        // ── MOVEMENT ──────────────────────────────────────────────
        private void DrawMovement()
        {
            GUILayout.Label("─── Speed ───");
            speedHack = GUILayout.Toggle(speedHack, $"Speed Hack ({speedMultiplier:F1}x)");
            if (speedHack)
            {
                GUILayout.Label($"Multiplier: {speedMultiplier:F1}x");
                speedMultiplier = GUILayout.HorizontalSlider(speedMultiplier, 1f, 10f);
            }

            GUILayout.Space(5);
            GUILayout.Label("─── Jump ───");
            infiniteJump = GUILayout.Toggle(infiniteJump, "Infinite Jump");
            if (infiniteJump)
            {
                GUILayout.Label($"Force: {jumpForce:F0}");
                jumpForce = GUILayout.HorizontalSlider(jumpForce, 5f, 50f);
            }

            GUILayout.Space(5);
            GUILayout.Label("─── Fly ───");
            noClip = GUILayout.Toggle(noClip, "No Clip / Fly  (WASD + Q/E)");

            GUILayout.Space(5);
            if (GUILayout.Button("Teleport to Origin"))
                if (player != null) player.transform.position = new Vector3(0f, 5f, 0f);
        }

        // ── PHYSICS ───────────────────────────────────────────────
        private void DrawPhysics()
        {
            godMode = GUILayout.Toggle(godMode, "God Mode (Anti-Fall)");

            GUILayout.Space(5);
            lowGravity = GUILayout.Toggle(lowGravity, "Low Gravity");
            if (lowGravity)
            {
                GUILayout.Label($"Gravity Scale: {gravityScale:F2}");
                gravityScale = GUILayout.HorizontalSlider(gravityScale, 0.05f, 1f);
            }
        }

        // ── MISC ──────────────────────────────────────────────────
        private void DrawMisc()
        {
            antiAFK = GUILayout.Toggle(antiAFK, "Anti-AFK");

            GUILayout.Space(10);
            if (GUILayout.Button("Reset All Settings"))
            {
                speedHack = false; infiniteJump = false; noClip = false;
                godMode = false; antiAFK = false; lowGravity = false;
                speedMultiplier = 2f; jumpForce = 15f; gravityScale = 0.3f;
                Physics.gravity = new Vector3(0f, -9.81f, 0f);
                if (rb != null) rb.useGravity = true;
            }
        }

        // ── COSMETICS ─────────────────────────────────────────────
        private void DrawCosmetics()
        {
            GUILayout.Label("─── Unlock All ───");

            if (GUILayout.Button("Unlock All Skins"))
                cosmeticStatus = TryUnlock("Skin", "skin", "costume", "character");

            if (GUILayout.Button("Unlock All Emotes"))
                cosmeticStatus = TryUnlock("Emote", "emote", "dance", "gesture");

            if (GUILayout.Button("Unlock All Abilities"))
                cosmeticStatus = TryUnlock("Ability", "ability", "powerup", "skill");

            if (GUILayout.Button("Unlock All Footsteps"))
                cosmeticStatus = TryUnlock("Footstep", "footstep", "trail", "step");

            if (GUILayout.Button("Unlock All Animations"))
                cosmeticStatus = TryUnlock("Animation", "animation", "anim", "pose");

            GUILayout.Space(5);
            if (GUILayout.Button("UNLOCK EVERYTHING"))
                cosmeticStatus = UnlockEverything();

            GUILayout.Space(8);
            GUILayout.Label("─── Status ───");
            GUILayout.Label(cosmeticStatus);

            GUILayout.Space(8);
            GUILayout.Label("─── Force Grant via PlayerPrefs ───");
            if (GUILayout.Button("Force Save All Unlocked"))
                ForcePlayerPrefsUnlock();
        }

        // ── UNLOCK HELPERS ────────────────────────────────────────

        /// <summary>
        /// Searches all loaded assemblies for classes/methods matching the keywords
        /// and tries to invoke "unlock all" style methods via reflection.
        /// </summary>
        private string TryUnlock(string label, params string[] keywords)
        {
            int found = 0;
            try
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (Type type in asm.GetTypes())
                    {
                        string typeLower = type.Name.ToLower();
                        bool typeMatch = false;
                        foreach (string kw in keywords)
                            if (typeLower.Contains(kw)) { typeMatch = true; break; }

                        if (!typeMatch) continue;

                        // Try static methods that sound like "unlock all"
                        foreach (MethodInfo method in type.GetMethods(
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Static | BindingFlags.Instance))
                        {
                            string mLow = method.Name.ToLower();
                            if ((mLow.Contains("unlock") || mLow.Contains("grant") ||
                                 mLow.Contains("own") || mLow.Contains("have")) &&
                                method.GetParameters().Length == 0)
                            {
                                try
                                {
                                    object instance = null;
                                    if (!method.IsStatic)
                                    {
                                        // Try to find a singleton Instance field
                                        FieldInfo inst = type.GetField("Instance",
                                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                                        if (inst != null) instance = inst.GetValue(null);
                                        PropertyInfo instProp = type.GetProperty("Instance",
                                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                                        if (instance == null && instProp != null)
                                            instance = instProp.GetValue(null);
                                        if (instance == null) continue;
                                    }
                                    method.Invoke(instance, null);
                                    found++;
                                    LoggerInstance.Msg($"[StumbleMenu] Invoked {type.Name}.{method.Name}");
                                }
                                catch { }
                            }
                        }

                        // Try to set bool fields like "isOwned", "isUnlocked" to true
                        foreach (FieldInfo field in type.GetFields(
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Static | BindingFlags.Instance))
                        {
                            string fLow = field.Name.ToLower();
                            if (field.FieldType == typeof(bool) &&
                                (fLow.Contains("owned") || fLow.Contains("unlock") ||
                                 fLow.Contains("purchased") || fLow.Contains("have")))
                            {
                                try
                                {
                                    // Static fields
                                    if (field.IsStatic)
                                    {
                                        field.SetValue(null, true);
                                        found++;
                                    }
                                    else
                                    {
                                        // Find all instances via FindObjectsOfType
                                        var objs = UnityEngine.Object.FindObjectsOfType(type);
                                        foreach (var obj in objs)
                                        {
                                            field.SetValue(obj, true);
                                            found++;
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"[StumbleMenu] Error during {label} unlock: {ex.Message}");
                return $"{label}: Error - {ex.Message}";
            }

            return found > 0
                ? $"{label}: Patched {found} field(s)/method(s)!"
                : $"{label}: No direct targets found.\nTry 'Force Save All Unlocked'.";
        }

        private string UnlockEverything()
        {
            TryUnlock("Skin",      "skin", "costume", "character");
            TryUnlock("Emote",     "emote", "dance", "gesture");
            TryUnlock("Ability",   "ability", "powerup", "skill");
            TryUnlock("Footstep",  "footstep", "trail", "step");
            TryUnlock("Animation", "animation", "anim", "pose");
            return "All categories attempted! Check logs for details.";
        }

        /// <summary>
        /// Writes large integer values into every PlayerPrefs key that sounds
        /// like a currency or unlock counter — a common pattern in Unity games.
        /// </summary>
        private void ForcePlayerPrefsUnlock()
        {
            string[] currencyKeys = {
                "Gems", "Coins", "Tokens", "Currency", "Gold",
                "Stumble", "StumbleTokens", "Rolls", "Keys"
            };
            foreach (string key in currencyKeys)
                PlayerPrefs.SetInt(key, 999999);

            string[] unlockKeys = {
                "SkinOwned", "EmoteOwned", "AbilityOwned",
                "FootstepOwned", "AnimOwned", "AllUnlocked",
                "UnlockAll", "Owned", "Purchased"
            };
            foreach (string key in unlockKeys)
                PlayerPrefs.SetInt(key, 1);

            PlayerPrefs.Save();
            cosmeticStatus = "PlayerPrefs unlock keys written & saved!";
            LoggerInstance.Msg("[StumbleMenu] PlayerPrefs forced.");
        }
    }
}