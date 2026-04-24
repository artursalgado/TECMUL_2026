using UnityEngine;

/// <summary>
/// Global game configuration. Settings persist through scenes and are saved to PlayerPrefs.
/// </summary>
public static class GameConfig
{
    public const string DefaultGameplaySceneName = "Mapa_EXT01";

    public static string GameplaySceneName
    {
        get => PlayerPrefs.GetString("GameplaySceneName", DefaultGameplaySceneName);
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            PlayerPrefs.SetString("GameplaySceneName", value.Trim());
            PlayerPrefs.Save();
        }
    }

    public static bool EnableSceneBootstrap
    {
        get => PlayerPrefs.GetInt("EnableSceneBootstrap", 1) == 1;
        set
        {
            PlayerPrefs.SetInt("EnableSceneBootstrap", value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool IsMenuScene(string sceneName)
    {
        return sceneName == "MainMenu" || sceneName == "Menu" || sceneName == "Start";
    }

    public static int DifficultyLevel
    {
        get => PlayerPrefs.GetInt("DifficultyLevel", 1);
        set { PlayerPrefs.SetInt("DifficultyLevel", value); PlayerPrefs.Save(); }
    }

    public static bool NightMode
    {
        get => PlayerPrefs.GetInt("NightMode", 0) == 1;
        set { PlayerPrefs.SetInt("NightMode", value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static int StartingAmmo = 56;

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat("MasterVolume", 0.85f);
        set { PlayerPrefs.SetFloat("MasterVolume", value); PlayerPrefs.Save(); ApplyAudio(); }
    }

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat("MusicVolume", 0.6f);
        set { PlayerPrefs.SetFloat("MusicVolume", value); PlayerPrefs.Save(); ApplyAudio(); }
    }

    public static float SFXVolume
    {
        get => PlayerPrefs.GetFloat("SFXVolume", 1.0f);
        set { PlayerPrefs.SetFloat("SFXVolume", value); PlayerPrefs.Save(); ApplyAudio(); }
    }

    public static float MouseSensitivity
    {
        get => PlayerPrefs.GetFloat("MouseSensitivity", 2.0f);
        set { PlayerPrefs.SetFloat("MouseSensitivity", value); PlayerPrefs.Save(); }
    }

    public static int FieldOfView
    {
        get => PlayerPrefs.GetInt("FieldOfView", 75);
        set { PlayerPrefs.SetInt("FieldOfView", value); PlayerPrefs.Save(); }
    }

    public static bool SkipConfigMenu = false;

    public static float ZombieHealthMultiplier =>
        DifficultyLevel == 0 ? 0.60f :
        DifficultyLevel == 1 ? 1.00f : 1.65f;

    public static float ZombieDamageMultiplier =>
        DifficultyLevel == 0 ? 0.50f :
        DifficultyLevel == 1 ? 1.00f : 2.00f;

    public static float LightIntensity => NightMode ? 0.08f : 1.0f;

    public static Color AmbientColor =>
        NightMode
            ? new Color(0.06f, 0.08f, 0.18f)
            : new Color(0.78f, 0.83f, 0.88f);

    public static float PlayerHealthMultiplier =>
        DifficultyLevel == 0 ? 1.5f :
        DifficultyLevel == 1 ? 1.0f : 0.75f;

    public static void ApplyAudio()
    {
        AudioListener.volume = MasterVolume;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(MusicVolume);
            AudioManager.Instance.SetSFXVolume(SFXVolume);
        }
    }

    public static void ResetToDefaults()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        DifficultyLevel = 1;
        NightMode = false;
        MasterVolume = 0.85f;
        MusicVolume = 0.6f;
        SFXVolume = 1.0f;
        MouseSensitivity = 2.0f;
        FieldOfView = 75;
        GameplaySceneName = DefaultGameplaySceneName;
        EnableSceneBootstrap = true;

        ApplyAudio();
    }
}
