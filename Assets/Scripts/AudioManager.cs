using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Music")]
    public AudioClip musicAmbient;
    public AudioClip musicCombat;
    public AudioClip musicGameOver;
    public AudioClip musicVictory;
    public float musicFadeDuration = 2f;

    [Header("SFX")]
    public AudioClip sfxFootstepA;
    public AudioClip sfxFootstepB;
    public AudioClip sfxHeartbeat;
    public AudioClip sfxPickup;
    public AudioClip sfxAlert;

    private AudioSource _musicSource;
    private AudioSource _musicSource2;
    private AudioSource _sfxSource;
    private AudioSource _heartbeatSource;
    private bool _activeMusicIsA = true;

    private float _footstepTimer;
    private float _footstepInterval = 0.42f;
    private bool  _playerMoving;
    private bool  _playerSprinting;

    private float    _musicVolume = 0.6f;
    private float    _sfxVolume   = 1.0f;
    private AudioClip _currentMusic;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureAudioManager()
    {
        if (FindFirstObjectByType<AudioManager>() != null) return;
        GameObject go = new GameObject("AudioManager");
        DontDestroyOnLoad(go);
        go.AddComponent<AudioManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildSources();
        TryLoadClipsFromResources();
    }

    void BuildSources()
    {
        _musicSource       = gameObject.GetComponent<AudioSource>();
        _musicSource.loop  = true;
        _musicSource.playOnAwake = false;

        _musicSource2      = gameObject.AddComponent<AudioSource>();
        _musicSource2.loop = true;
        _musicSource2.playOnAwake = false;

        _sfxSource         = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;

        _heartbeatSource   = gameObject.AddComponent<AudioSource>();
        _heartbeatSource.loop = true;
        _heartbeatSource.playOnAwake = false;
    }

    void TryLoadClipsFromResources()
    {
        if (musicAmbient  == null) musicAmbient  = Resources.Load<AudioClip>("Audio/Music/Ambient");
        if (musicCombat   == null) musicCombat   = Resources.Load<AudioClip>("Audio/Music/Combat");
        if (musicGameOver == null) musicGameOver = Resources.Load<AudioClip>("Audio/Music/GameOver");
        if (musicVictory  == null) musicVictory  = Resources.Load<AudioClip>("Audio/Music/Victory");
        if (sfxFootstepA  == null) sfxFootstepA  = Resources.Load<AudioClip>("Audio/SFX/FootstepA");
        if (sfxFootstepB  == null) sfxFootstepB  = Resources.Load<AudioClip>("Audio/SFX/FootstepB");
        if (sfxHeartbeat  == null) sfxHeartbeat  = Resources.Load<AudioClip>("Audio/SFX/Heartbeat");
        if (sfxPickup     == null) sfxPickup     = Resources.Load<AudioClip>("Audio/SFX/Pickup");
        if (sfxAlert      == null) sfxAlert      = Resources.Load<AudioClip>("Audio/SFX/Alert");

        if (musicAmbient != null) PlayMusic(musicAmbient, false);
    }

    void Update()
    {
        TickFootsteps();
    }

    // ── Music ─────────────────────────────────────────────────────────────
    public void PlayMusic(AudioClip clip, bool crossfade = true)
    {
        if (clip == null || clip == _currentMusic) return;
        _currentMusic = clip;
        if (crossfade)
            StartCoroutine(CrossfadeMusic(clip));
        else
        {
            _musicSource.clip   = clip;
            _musicSource.volume = _musicVolume;
            _musicSource.Play();
        }
    }

    IEnumerator CrossfadeMusic(AudioClip newClip)
    {
        AudioSource fadeOut = _activeMusicIsA ? _musicSource  : _musicSource2;
        AudioSource fadeIn  = _activeMusicIsA ? _musicSource2 : _musicSource;
        _activeMusicIsA = !_activeMusicIsA;

        fadeIn.clip   = newClip;
        fadeIn.volume = 0f;
        fadeIn.Play();

        float elapsed  = 0f;
        float startVol = fadeOut.volume;
        while (elapsed < musicFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = elapsed / musicFadeDuration;
            fadeOut.volume = Mathf.Lerp(startVol, 0f,           t);
            fadeIn.volume  = Mathf.Lerp(0f,       _musicVolume, t);
            yield return null;
        }
        fadeOut.Stop();
        fadeIn.volume = _musicVolume;
    }

    public void SwitchToCombat()  => PlayMusic(musicCombat);
    public void SwitchToAmbient() => PlayMusic(musicAmbient);
    public void PlayGameOver()    => PlayMusic(musicGameOver, false);
    public void PlayVictory()     => PlayMusic(musicVictory,  false);

    // ── SFX ───────────────────────────────────────────────────────────────
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || _sfxSource == null) return;
        _sfxSource.PlayOneShot(clip, _sfxVolume * volumeScale);
    }

    public void PlayPickup() => PlaySFX(sfxPickup, 0.9f);
    public void PlayAlert()  => PlaySFX(sfxAlert,  0.8f);

    // ── Footsteps ─────────────────────────────────────────────────────────
    public void SetPlayerMovement(bool moving, bool sprinting)
    {
        _playerMoving    = moving;
        _playerSprinting = sprinting;
        _footstepInterval = sprinting ? 0.28f : 0.44f;
    }

    void TickFootsteps()
    {
        if (!_playerMoving) { _footstepTimer = 0f; return; }
        _footstepTimer -= Time.deltaTime;
        if (_footstepTimer > 0f) return;

        _footstepTimer = _footstepInterval;
        AudioClip step = (Random.value > 0.5f) ? sfxFootstepA : sfxFootstepB;
        if (step != null)
            _sfxSource.PlayOneShot(step, _sfxVolume * Random.Range(0.7f, 1f));
    }

    // ── Heartbeat on low health ────────────────────────────────────────────
    public void SetPlayerHealth(int current, int max)
    {
        if (_heartbeatSource == null) return;
        float ratio = max > 0 ? (float)current / max : 1f;

        if (ratio < 0.25f && sfxHeartbeat != null)
        {
            if (!_heartbeatSource.isPlaying)
            {
                _heartbeatSource.clip   = sfxHeartbeat;
                _heartbeatSource.volume = _sfxVolume * 0.6f;
                _heartbeatSource.Play();
            }
        }
        else
        {
            if (_heartbeatSource.isPlaying) _heartbeatSource.Stop();
        }
    }

    // ── Volume ────────────────────────────────────────────────────────────
    public void SetMusicVolume(float v)
    {
        _musicVolume          = Mathf.Clamp01(v);
        _musicSource.volume   = _musicVolume;
        _musicSource2.volume  = _musicVolume;
    }

    public void SetSFXVolume(float v) => _sfxVolume = Mathf.Clamp01(v);

    void OnDestroy() { if (Instance == this) Instance = null; }
}
