using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PlayFromRailroad
{
    private const string ScenePath = "Assets/Knightworld/Scenes/Railroad.unity";

    static PlayFromRailroad()
    {
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        if (scene != null)
            EditorSceneManager.playModeStartScene = scene;
    }
}
