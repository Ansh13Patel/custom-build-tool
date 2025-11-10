using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class PlatformBuildData {
    public List<string> symbols = new List<string>();
    public List<bool> enabled = new List<bool>();
}

public class PlatformBuildWindow : EditorWindow {
    private string platform;
    private string mode;
    private PlatformBuildData data;
    private GlobalBuildSettingsData global;
    private string filePath;

    public static void Open(string platform, string mode) {
        var window = GetWindow<PlatformBuildWindow>($"{platform} - {mode}");
        window.platform = platform.ToLower();
        window.mode = mode.ToLower();
        window.Init();
    }

    private void Init() {
        string globalPath = "Assets/Editor/CustomBuildTool/Config/GlobalSettings.json";
        global = File.Exists(globalPath)
            ? JsonUtility.FromJson<GlobalBuildSettingsData>(File.ReadAllText(globalPath))
            : new GlobalBuildSettingsData();

        filePath = $"Assets/Editor/CustomBuildTool/PlatformSettings/{platform}-{mode}.txt";
        if (File.Exists(filePath)) {
            data = JsonUtility.FromJson<PlatformBuildData>(File.ReadAllText(filePath));
        } else {
            data = new PlatformBuildData();
        }

        // Ensure all global symbols exist in platform config
        SyncSymbolsWithGlobal();
    }

    private void OnGUI() {
        if (global == null || data == null) {
            GUILayout.Label("No configuration found.");
            return;
        }

        GUILayout.Label($"{platform.ToUpper()} - {mode.ToUpper()} Build Settings", EditorStyles.boldLabel);
        GUILayout.Space(10);

        for (int i = 0; i < data.symbols.Count; i++) {
            data.enabled[i] = EditorGUILayout.ToggleLeft(data.symbols[i], data.enabled[i]);
        }

        GUILayout.Space(10);
        if (GUILayout.Button("💾 Save")) Save();

        if (GUILayout.Button("🚀 Build")) {
            ApplySymbols();
            Debug.Log($"Building {platform} ({mode})...");
        }
    }

    private void Save() {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        File.WriteAllText(filePath, JsonUtility.ToJson(data, true));
        AssetDatabase.Refresh();
        Debug.Log($"{platform}-{mode} settings saved.");
    }

    private void ApplySymbols() {
        var activeSymbols = new List<string>();
        for (int i = 0; i < data.symbols.Count; i++)
            if (data.enabled[i]) activeSymbols.Add(data.symbols[i]);

        PlayerSettings.SetScriptingDefineSymbolsForGroup(
            BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget),
            string.Join(";", activeSymbols)
        );
    }

    private void SyncSymbolsWithGlobal() {
        // Add any missing global symbols
        foreach (var sym in global.scriptingSymbols) {
            if (!data.symbols.Contains(sym)) {
                data.symbols.Add(sym);
                data.enabled.Add(false);
            }
        }

        // Remove extra symbols not in global
        for (int i = data.symbols.Count - 1; i >= 0; i--) {
            if (!global.scriptingSymbols.Contains(data.symbols[i])) {
                data.symbols.RemoveAt(i);
                data.enabled.RemoveAt(i);
            }
        }

        Save();
    }
}
