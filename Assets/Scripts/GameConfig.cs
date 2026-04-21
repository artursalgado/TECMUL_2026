using UnityEngine;

/// <summary>
/// Global game configuration — all settings persist through scenes and are saved to PlayerPrefs.
/// </summary>
public static class GameConfig
{
    // ── Difficulty ──────────────────────────────────────────────────────
    public static int DifficultyLevel
    {
        get => PlayerPrefs.GetInt("DifficultyLevel", 1);
        set { PlayerPrefs.SetInt("DifficultyLevel", value); PlayerPrefs.Save(); }
    }

    // ── Atmosphere ──────────────────────────────────────────────────────
    public static bool NightMode
    {
        get => PlayerPrefs.GetInt("NightMode", 0) == 1;
        set { PlayerPrefs.SetInt("NightMode", value ? 1 : 0); PlayerPrefs.Save(); }
    }

    // ── Ammo ────────────────────────────────────────────────────────────
    public static int StartingAmmo = 56;

    // ── Audio ───────────────────────────────────────────────────────────
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

    // ── Controls ────────────────────────────────────────────────────────
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

    // ── Derived / read-only ──────────────────────────────────────────────
    public static float ZombieHealthMultiplier =>
        DifficultyLevel == 0 ? 0.60f :
        DifficultyLevel == 1 ? 1.00f : 1.65f;

    public static float ZombieDamageMultiplier =>
        DifficultyLevel == 0 ? 0.50f :
        DifficultyLevel == 1 ? 1.00f : 2.00f;

    public static float LightIntensity =>
        NightMode ? 0.08f : 1.0f;

    public static Color AmbientColor =>
        NightMode
            ? new Color(0.06f, 0.08f, 0.18f)
            : new Color(0.78f, 0.83f, 0.88f);

    public static float PlayerHealthMultiplier =>
        DifficultyLevel == 0 ? 1.5f :
        DifficultyLevel == 1 ? 1.0f : 0.75f;

    // ── Helpers ──────────────────────────────────────────────────────────
    /// <summary>Apply audio config to AudioListener and cached AudioManager.</summary>
    public static void ApplyAudio()
    {
        AudioListener.volume = MasterVolume;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(MusicVolume);
            AudioManager.Instance.SetSFXVolume(SFXVolume);
        }
    }
    
    /// <summary>Reset all settings to default.</summary>
    public static void ResetToDefaults()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        
        // Restore runtime defaults
        DifficultyLevel = 1;
        NightMode = false;
        MasterVolume = 0.85f;
        MusicVolume = 0.6f;
        SFXVolume = 1.0f;
        MouseSensitivity = 2.0f;
        FieldOfView = 75;
        
        ApplyAudio();
    }
}
