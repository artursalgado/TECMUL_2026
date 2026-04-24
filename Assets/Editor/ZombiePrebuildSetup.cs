// Assets/Editor/ZombiePrebuildSetup.cs
// Copia automaticamente o prefab zombie para Assets/Resources/Prefabs/ antes de cada build.
// Isto garante que Resources.Load<GameObject>("Prefabs/ZombieMale_AAB_URP") funciona em runtime.

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public class ZombiePrebuildSetup : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    const string SourcePrefab = "Assets/ZombieMale_AAB/Prefabs/URP/ZombieMale_AAB_URP.prefab";
    const string DestFolder   = "Assets/Resources/Prefabs";
    const string DestPrefab   = "Assets/Resources/Prefabs/ZombieMale_AAB_URP.prefab";

    public void OnPreprocessBuild(BuildReport report)
    {
        // Garante que a pasta existe
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(DestFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");

        // Verifica se o prefab fonte existe
        if (!File.Exists(SourcePrefab))
        {
            Debug.LogError($"[ZombiePrebuildSetup] Prefab fonte não encontrado: {SourcePrefab}");
            return;
        }

        // Copia se ainda não existe ou está desatualizado
        if (!File.Exists(DestPrefab))
        {
            bool ok = AssetDatabase.CopyAsset(SourcePrefab, DestPrefab);
            if (ok)
                Debug.Log($"[ZombiePrebuildSetup] Prefab copiado para {DestPrefab}");
            else
                Debug.LogError($"[ZombiePrebuildSetup] Falhou ao copiar prefab para {DestPrefab}");
        }
        else
        {
            Debug.Log($"[ZombiePrebuildSetup] Prefab já existe em {DestPrefab} — sem cópia necessária.");
        }

        AssetDatabase.Refresh();
    }
}
