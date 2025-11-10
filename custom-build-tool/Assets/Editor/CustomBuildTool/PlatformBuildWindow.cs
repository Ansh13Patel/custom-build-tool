using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class PlatformBuildWindow : EditorWindow
{
    [SerializeField] private string platformName;
    [SerializeField] private string mode = "debug"; // "debug" or "release"

    private Dictionary<string, bool> symbolToggles = new Dictionary<string, bool>();
    private GlobalBuildSettingsData globalSettings;

    private const string CONFIG_PATH = "Assets/Editor/CustomBuildTool/Config/";
    private bool initialized = false;

    public static void Open(string platform)
    {
        var window = GetWindow<PlatformBuildWindow>($"{platform} Build Settings");
        window.platformName = platform;
        window.mode = "debug";
        window.initialized = false;
        window.Show();
    }

    private void OnEnable()
    {
        // Unity reloads domain after PlayerSettings changes, so re-init here
        if (!string.IsNullOrEmpty(platformName))
        {
            LoadGlobalSettings();
            LoadSymbols();
            initialized = true;
        }
    }

    private void LoadGlobalSettings()
    {
        string path = Path.Combine(CONFIG_PATH, "GlobalSettings.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            globalSettings = JsonUtility.FromJson<GlobalBuildSettingsData>(json);
        }
        else
        {
            globalSettings = new GlobalBuildSettingsData { scriptingSymbols = new List<string>() };
        }

        if (globalSettings.scriptingSymbols == null)
            globalSettings.scriptingSymbols = new List<string>();
    }

    private string GetJsonFilePath()
    {
        return Path.Combine(CONFIG_PATH, $"{platformName.ToLower()}-{mode}.json");
    }

    private void LoadSymbols()
    {
        symbolToggles.Clear();
        foreach (var s in globalSettings.scriptingSymbols)
            symbolToggles[s] = false;

        string path = GetJsonFilePath();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            var dict = ParseFlatJson(json);
            foreach (var kvp in dict)
                if (symbolToggles.ContainsKey(kvp.Key))
                    symbolToggles[kvp.Key] = kvp.Value;
        }
        else
        {
            SaveSymbols(); // create file if missing
        }

        SyncWithCurrentDefines();
    }

    private void SaveSymbols()
    {
        string path = GetJsonFilePath();
        var data = symbolToggles.ToDictionary(k => k.Key, v => v.Value);

        string json = "{\n" + string.Join(",\n", data.Select(kvp => $"    \"{kvp.Key}\": {kvp.Value.ToString().ToLower()}")) + "\n}";
        File.WriteAllText(path, json);

        UpdateScriptingDefineSymbols();
    }

    private void UpdateScriptingDefineSymbols()
    {
        var activeGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
        var enabledSymbols = symbolToggles
            .Where(s => s.Value)
            .Select(s => s.Key)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        string defines = string.Join(";", enabledSymbols);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(activeGroup, defines);
    }

    private void SyncWithCurrentDefines()
    {
        var activeGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(activeGroup);
        var currentDefines = defines.Split(';').ToList();

        foreach (var key in symbolToggles.Keys.ToList())
            symbolToggles[key] = currentDefines.Contains(key);
    }

    private Dictionary<string, bool> ParseFlatJson(string json)
    {
        var dict = new Dictionary<string, bool>();
        foreach (string line in json.Split('\n'))
        {
            if (line.Contains(":"))
            {
                string[] parts = line.Split(':');
                string key = parts[0].Trim().Trim('"');
                string val = parts[1].Trim().Trim(',', ' ', '\r', '}');
                if (!string.IsNullOrEmpty(key))
                    dict[key] = val.Equals("true", System.StringComparison.OrdinalIgnoreCase);
            }
        }
        return dict;
    }

    private void OnGUI()
    {
        if (!initialized && !string.IsNullOrEmpty(platformName))
        {
            LoadGlobalSettings();
            LoadSymbols();
            initialized = true;
        }

        GUILayout.Label($"{platformName} Build Settings", EditorStyles.boldLabel);
        GUILayout.Space(5);

        // Dropdown for build mode
        EditorGUI.BeginChangeCheck();
        mode = EditorGUILayout.Popup("Build Mode",
            mode == "debug" ? 0 : 1,
            new[] { "Debug", "Release" }) == 0 ? "debug" : "release";

        if (EditorGUI.EndChangeCheck())
            LoadSymbols();

        GUILayout.Space(10);
        GUILayout.Label("Scripting Symbols:", EditorStyles.boldLabel);

        if (globalSettings.scriptingSymbols.Count == 0)
        {
            EditorGUILayout.HelpBox("No scripting symbols defined in global settings.", MessageType.Info);
            return;
        }

        foreach (var symbol in globalSettings.scriptingSymbols.ToList())
        {
            bool current = symbolToggles.ContainsKey(symbol) && symbolToggles[symbol];
            bool updated = EditorGUILayout.ToggleLeft(symbol, current);
            symbolToggles[symbol] = updated;
        }

        GUILayout.Space(15);
        if (GUILayout.Button("💾 Save & Apply", GUILayout.Height(30)))
        {
            SaveSymbols();
            SyncWithCurrentDefines();
            // After apply, Unity recompiles, but OnEnable() will reload JSON
        }

        GUILayout.Space(10);
        if (GUILayout.Button("🚀 Build", GUILayout.Height(30)))
        {
            BuildCurrentPlatform();
        }
    }

    private void BuildCurrentPlatform()
    {
        string path = "Builds/" + platformName;
        Directory.CreateDirectory(path);

        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        string buildPath = Path.Combine(path, $"{platformName}_{mode}.exe");

        BuildTarget target = GetBuildTarget(platformName);
        BuildPipeline.BuildPlayer(scenes, buildPath, target, BuildOptions.None);

        EditorUtility.DisplayDialog("Build Complete", $"{platformName} ({mode}) build finished!", "OK");
    }

    private BuildTarget GetBuildTarget(string name)
    {
        switch (name.ToLower())
        {
            case "android": return BuildTarget.Android;
            case "ios": return BuildTarget.iOS;
            case "tvos": return BuildTarget.tvOS;
            case "webgl": return BuildTarget.WebGL;
            case "standalone": return BuildTarget.StandaloneWindows64;
            case "ps4": return BuildTarget.PS4;
            case "ps5": return BuildTarget.PS5;
            case "xboxone": return BuildTarget.XboxOne;
            default: return BuildTarget.NoTarget;
        }
    }
}