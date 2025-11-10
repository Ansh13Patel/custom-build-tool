using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Modules;

[System.Serializable]
public class GlobalBuildSettingsData {
    public List<string> scriptingSymbols = new List<string>();
    public List<string> targetPlatforms = new List<string>();
}

public class GlobalBuildSettings : EditorWindow {
    private GlobalBuildSettingsData settings = new GlobalBuildSettingsData();
    private const string SETTINGS_PATH = "Assets/Editor/CustomBuildTool/Config/GlobalSettings.json";
    private Vector2 scroll;
    private int selectedPlatformIndex;
    private string[] availablePlatforms;

    [MenuItem("Custom Build Tool/Settings")]
    public static void ShowWindow() => GetWindow<GlobalBuildSettings>("Build Settings");

    private void OnEnable() {
        Directory.CreateDirectory(Path.GetDirectoryName(SETTINGS_PATH));

        if (File.Exists(SETTINGS_PATH))
            settings = JsonUtility.FromJson<GlobalBuildSettingsData>(File.ReadAllText(SETTINGS_PATH));

        // Get available (installed) platforms
        availablePlatforms = GetInstalledPlatforms();

        // First-time setup: add only current active platform
        if (settings.targetPlatforms.Count == 0) {
            var active = EditorUserBuildSettings.activeBuildTarget.ToString();
            if (availablePlatforms.Contains(active))
                settings.targetPlatforms.Add(active);
        }
    }

    private void OnGUI() {
        GUILayout.Label("Global Build Settings", EditorStyles.boldLabel);

        GUILayout.Label("Scripting Symbols:");
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(150));
        for (int i = 0; i < settings.scriptingSymbols.Count; i++) {
            EditorGUILayout.BeginHorizontal();
            settings.scriptingSymbols[i] = EditorGUILayout.TextField(settings.scriptingSymbols[i]);
            if (GUILayout.Button("X", GUILayout.Width(25))) {
                settings.scriptingSymbols.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("+ Add Symbol")) settings.scriptingSymbols.Add("");

        GUILayout.Space(10);
        GUILayout.Label("Target Platforms:");

        foreach (var platform in settings.targetPlatforms.ToList()) {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(platform, GUILayout.Width(200));
            if (GUILayout.Button("Remove", GUILayout.Width(80)))
                settings.targetPlatforms.Remove(platform);
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(5);
        selectedPlatformIndex = EditorGUILayout.Popup("Add Platform", selectedPlatformIndex, availablePlatforms);
        if (GUILayout.Button("Add Selected Platform")) {
            string selected = availablePlatforms[selectedPlatformIndex];
            if (!settings.targetPlatforms.Contains(selected))
                settings.targetPlatforms.Add(selected);
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Save Settings")) Save();
    }

    private void Save() {
        File.WriteAllText(SETTINGS_PATH, JsonUtility.ToJson(settings, true));
        AssetDatabase.Refresh();
        Debug.Log("Global Build Settings saved!");
    }

    private string[] GetInstalledPlatforms()
    {
        // Define your supported build targets manually
        var platforms = new List<string>
        {
            "Android",
            "iOS",
            "tvOS",       // ✅ added tvOS here
            "Standalone",
            "WebGL",
            "PS4",
            "PS5",
            "XboxOne"
        };

        // Remove duplicates or nulls just in case
        return platforms
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .ToArray();
    }
}