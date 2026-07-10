#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PlayModeBootstrapper
{
    private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
    private const string MenuPath = "Tools/Always Start From Bootstrap";

    static PlayModeBootstrapper()
    {
        EditorApplication.delayCall += () => {
            ApplyStartScene(IsFeatureEnabled());
        };
    }

    [MenuItem(MenuPath)]
    public static void ToggleBootstrap()
    {
        bool newState = !IsFeatureEnabled();
        Menu.SetChecked(MenuPath, newState);
        EditorPrefs.SetBool("UseBootstrapScene", newState);
        
        ApplyStartScene(newState);
        
        Debug.Log(newState ? "🚀 Bootstrap mode: ENABLED" : "🎮 Bootstrap mode: DISABLED");
    }
    
    [MenuItem("Tools/Run Bootstrap &b")]
    public static void RunWithBootstrap()
    {
        ApplyStartScene(true);
        EditorApplication.isPlaying = true;
    }

    private static void ApplyStartScene(bool enabled)
    {
        if (enabled)
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
            EditorSceneManager.playModeStartScene = scene;
        }
        else
        {
            EditorSceneManager.playModeStartScene = null;
        }
    }

    private static bool IsFeatureEnabled()
    {
        return EditorPrefs.GetBool("UseBootstrapScene", false);
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateToggleBootstrap()
    {
        Menu.SetChecked(MenuPath, IsFeatureEnabled());
        return true;
    }
}
#endif