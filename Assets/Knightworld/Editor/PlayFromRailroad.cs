using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PlayFromOpenScene
{
    static PlayFromOpenScene()
    {
        // Play whatever scene is open in the editor (do not force Railroad).
        EditorSceneManager.playModeStartScene = null;
    }
}
