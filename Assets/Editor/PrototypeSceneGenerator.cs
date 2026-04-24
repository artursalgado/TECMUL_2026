#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PrototypeSceneGenerator
{
    static PrototypeSceneGenerator()
    {
        EditorApplication.delayCall += TryGenerateScene;
    }

    [MenuItem("Tools/Generate Prototype Scene")]
    public static void GenerateFromMenu()
    {
        BuildScene(force: true);
    }

    static void TryGenerateScene()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (!SceneBootstrapper.ShouldBuildActiveScene())
        {
            return;
        }

        BuildScene(force: false);
    }

    static void BuildScene(bool force)
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "Mapa_EXT01")
        {
            return;
        }

        bool needsVersionUpgrade = SceneNeedsUpgrade(scene);

        if (!force && !needsVersionUpgrade && scene.rootCount > 3)
        {
            return;
        }

        if (!SceneBootstrapper.ShouldBuildActiveScene())
        {
            return;
        }

        SceneBootstrapper.BuildPrototypeScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Prototype scene generated and saved.");
    }

    static bool SceneNeedsUpgrade(UnityEngine.SceneManagement.Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            PrototypeSceneMarker marker = root.GetComponent<PrototypeSceneMarker>();
            if (marker != null)
            {
                return marker.version != SceneBootstrapper.SceneVersion;
            }
        }

        return true;
    }
}
#endif
