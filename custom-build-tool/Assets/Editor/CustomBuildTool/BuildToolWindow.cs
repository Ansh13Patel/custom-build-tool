using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class BuildToolWindow : EditorWindow
{
    private GlobalBuildSettingsData globalSettings;
    private const string SETTINGS_PATH = "Assets/Editor/CustomBuildTool/Config/GlobalSettings.json";

    [MenuItem("Custom Build Tool/Launcher")]
    public static void ShowWindow()
    {
        GetWindow<BuildToolWindow>("Build Launcher");
    }

    private void OnEnable()
    {
        LoadGlobalSettings();
    }

    private void LoadGlobalSettings()
    {
        if (File.Exists(SETTINGS_PATH))
        {
            string json = File.ReadAllText(SETTINGS_PATH);
            globalSettings = JsonUtility.FromJson<GlobalBuildSettingsData>(json);
        }
        else
        {
            globalSettings = new GlobalBuildSettingsData
            {
                targetPlatforms = new List<string> { GetCurrentPlatformName() } // first time, only current
            };
            SaveGlobalSettings();
        }
    }

    private void SaveGlobalSettings()
    {
        string json = JsonUtility.ToJson(globalSettings, true);
        File.WriteAllText(SETTINGS_PATH, json);
    }

    private string GetCurrentPlatformName()
    {
        BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;
        switch (activeTarget)
        {
            case BuildTarget.Android: return "Android";
            case BuildTarget.iOS: return "iOS";
            case BuildTarget.tvOS: return "tvOS";
            case BuildTarget.WebGL: return "WebGL";
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
            case BuildTarget.StandaloneOSX:
            case BuildTarget.StandaloneLinux64:
                return "Standalone";
            case BuildTarget.PS4: return "PS4";
            case BuildTarget.PS5: return "PS5";
            case BuildTarget.XboxOne: return "XboxOne";
            default: return activeTarget.ToString();
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Build Tool", EditorStyles.boldLabel);

        if (GUILayout.Button("⚙️ Settings"))
            GlobalBuildSettings.ShowWindow();

        GUILayout.Space(10);

        GUILayout.Label("Platforms:", EditorStyles.boldLabel);

        if (globalSettings == null || globalSettings.targetPlatforms == null || globalSettings.targetPlatforms.Count == 0)
        {
            GUILayout.Label("No platforms selected. Please open Settings to configure.");
            return;
        }

        foreach (string platform in globalSettings.targetPlatforms)
        {
            if (GUILayout.Button(platform))
            {
                HandlePlatformClick(platform);
            }
        }
    }

    private void HandlePlatformClick(string platform)
    {
        BuildTarget target = BuildTarget.NoTarget;

        switch (platform)
        {
            case "Android": target = BuildTarget.Android; break;
            case "iOS": target = BuildTarget.iOS; break;
            case "tvOS": target = BuildTarget.tvOS; break;
            case "WebGL": target = BuildTarget.WebGL; break;
            case "Standalone": target = BuildTarget.StandaloneWindows64; break;
            case "PS4": target = BuildTarget.PS4; break;
            case "PS5": target = BuildTarget.PS5; break;
            case "XboxOne": target = BuildTarget.XboxOne; break;
            default:
                Debug.LogWarning($"Unsupported platform: {platform}");
                return;
        }

        // Check if current platform is different
        if (EditorUserBuildSettings.activeBuildTarget != target)
        {
            bool switchPlatform = EditorUtility.DisplayDialog(
                "Switch Platform?",
                $"Current build target is {EditorUserBuildSettings.activeBuildTarget}.\n\nDo you want to switch to {platform}?",
                "Switch", "Cancel"
            );

            if (switchPlatform)
            {
                BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
                EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
            }
            else
            {
                return; // Don’t open window if user cancels
            }
        }

        // Once platform is correct, open build window
        PlatformBuildWindow.Open(platform);
    }

}
