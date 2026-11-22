using System;
using UnityEngine;
using TeksMaster.Core;

namespace TeksMaster.Audio
{
    public class TeksAudioManager : MonoBehaviour
    {
        public static TeksAudioManager Instance { get; private set; }

        private const int SfxSampleRate = 44100;
        private const int BgmSampleRate = 22050;
        private const float BgmDurationSeconds = 14.0f;
        private const float BgmMixGain = 0.52f;
        private const float UiClickThrottleSeconds = 0.03f;

        public float MasterVolume => _masterVolume;
        public float BgmVolume => _bgmVolume;
        public float SfxVolume => _sfxVolume;
        public bool IsMuted => _isMuted;
        public bool IsBgmPlaying => _bgmSource != null && _bgmSource.isPlaying;

        private AudioSource _sfxSource;
        private AudioSource _bgmSource;

        private AudioClip _cardFlickClip;
        private AudioClip _cardLandCommonClip;
        private AudioClip _cardLandRareClip;
        private AudioClip _cardLandEpicClip;
        private AudioClip _cardLandLegendaryClip;
        private AudioClip _winFanfareClip;
        private AudioClip _loseToneClip;
        private AudioClip _tablaRetryClip;
        private AudioClip _coinClinkClip;
        private AudioClip _packOpenClip;
        private AudioClip _hiddenCardFoundClip;
        private AudioClip _uiClickClip;
        private AudioClip _bgmClip;

        private PlayerInventory _boundInventory;
        private float _masterVolume = 0.85f;
        private float _bgmVolume = 0.45f;
        private float _sfxVolume = 0.90f;
        private bool _isMuted;
        private bool _isFullscreen = true;

        private bool _initialized;
        private bool _syncingSettings;
        private float _lastUiClickTime = -10f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (!Application.isPlaying) return;

            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            EnsureInitialized();
        }

        private void Start()
        {
            if (!Application.isPlaying) return;

            EnsureInitialized();
            if (_boundInventory == null && PlayerInventory.Instance != null)
            {
                BindInventory(PlayerInventory.Instance);
            }
            StartBgm();
        }

        private void OnDestroy()
        {
            if (_boundInventory != null)
            {
                _boundInventory.OnAudioSettingsChanged -= HandleInventoryAudioSettingsChanged;
                _boundInventory = null;
            }

            if (_bgmSource != null) _bgmSource.Stop();
            if (_sfxSource != null) _sfxSource.Stop();

            DestroyClip(ref _cardFlickClip);
            DestroyClip(ref _cardLandCommonClip);
            DestroyClip(ref _cardLandRareClip);
            DestroyClip(ref _cardLandEpicClip);
            DestroyClip(ref _cardLandLegendaryClip);
            DestroyClip(ref _winFanfareClip);
            DestroyClip(ref _loseToneClip);
            DestroyClip(ref _tablaRetryClip);
            DestroyClip(ref _coinClinkClip);
            DestroyClip(ref _packOpenClip);
            DestroyClip(ref _hiddenCardFoundClip);
            DestroyClip(ref _uiClickClip);
            DestroyClip(ref _bgmClip);

            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void EnsureInitialized()
        {
            if (!Application.isPlaying) return;
            if (_initialized) return;
            _initialized = true;

            _sfxSource = Create2DSource(loop: false, priority: 128);
            _bgmSource = Create2DSource(loop: true, priority: 96);

            AudioSettingsData initial = PlayerInventory.Instance != null && PlayerInventory.Instance.AudioSettings != null
                ? PlayerInventory.Instance.AudioSettings
                : TeksSaveSystem.LoadAudioSettingsFromPrefs();
            ApplySettingsInternal(initial);

            BakeAllProceduralClips();

            if (PlayerInventory.Instance != null)
            {
                BindInventory(PlayerInventory.Instance);
            }
        }

        public void BindInventory(PlayerInventory inventory)
        {
            if (!Application.isPlaying) return;
            if (inventory == null) return;

            if (_boundInventory == inventory)
            {
                if (inventory.AudioSettings != null)
                {
                    ApplySettingsInternal(inventory.AudioSettings);
                }
                return;
            }

            if (_boundInventory != null)
            {
                _boundInventory.OnAudioSettingsChanged -= HandleInventoryAudioSettingsChanged;
            }

            _boundInventory = inventory;
            _boundInventory.OnAudioSettingsChanged += HandleInventoryAudioSettingsChanged;

            if (_boundInventory.AudioSettings != null)
            {
                ApplySettingsInternal(_boundInventory.AudioSettings);
            }
        }

        public void ApplyAudioSettings(AudioSettingsData settings)
        {
            if (!Application.isPlaying || settings == null) return;
            EnsureInitialized();
            ApplySettingsInternal(settings);
        }

        public AudioSettingsData GetCurrentAudioSettings()
        {
            return new AudioSettingsData(_masterVolume, _bgmVolume, _sfxVolume, _isMuted, _isFullscreen);
        }

        public void SetMasterVolume(float volume)
        {
            if (!Application.isPlaying) return;
            EnsureInitialized();
            _masterVolume = Mathf.Clamp01(volume);
            ApplySourceVolumes();
            SyncSettingsToPersistence();
        }

        public void SetSfxVolume(float volume)
        {
            if (!Application.isPlaying) return;
            EnsureInitialized();
            _sfxVolume = Mathf.Clamp01(volume);
            ApplySourceVolumes();
            SyncSettingsToPersistence();
        }

        public void SetBgmVolume(float volume)
        {
            if (!Application.isPlaying) return;
            EnsureInitialized();
            _bgmVolume = Mathf.Clamp01(volume);
            ApplySourceVolumes();
            SyncSettingsToPersistence();
        }

        public void SetMuted(bool muted)
        {
            if (!Application.isPlaying) return;
            EnsureInitialized();
            _isMuted = muted;
            ApplySourceVolumes();
            SyncSettingsToPersistence();
        }

        public void ToggleMute()
        {
            SetMuted(!_isMuted);
        }

        public void StartBgm()
        {
            if (!Application.isPlaying) return;
            EnsureInitialized();
            if (_bgmSource == null || _bgmClip == null) return;

            if (_bgmSource.clip != _bgmClip)
            {
                _bgmSource.clip = _bgmClip;
            }

            _bgmSource.loop = true;
            ApplySourceVolumes();

            if (!_bgmSource.isPlaying)
            {
                _bgmSource.Play();
            }
        }

        public void StopBgm()
        {
            if (!Application.isPlaying) return;
            if (_bgmSource != null && _bgmSource.isPlaying)
            {
                _bgmSource.Stop();
            }
        }

        // --- 9 Procedural SFX Playback Methods ---

        public void PlayCardFlick(float powerNormalized = 0.7f)
        {
            if (!Application.isPlaying) return;
            EnsureInitialized();
            float scale = Mathf.Lerp(0.75f, 1.0f, Mathf.Clamp01(powerNormalized));
            PlaySfxInternal(_cardFlickClip, scale);
        }

        public void PlayCardLand(CardRarity rarity = CardRarity.Common)
        {
            if (!Application.isPlaying) return;
            EnsureInitialized();

            AudioClip clip;
            float scale;
            switch (rarity)
            {
                case CardRarity.Rare:
                    clip = _cardLandRareClip;
                    scale = 0.92f;
                    break;
                case CardRarity.Epic:
                    clip = _cardLandEpicClip;
                    scale = 0.96f;
                    break;
                case CardRarity.Legendary:
                    clip = _cardLandLegendaryClip;
                    scale = 1.0f;
                    break;
                default:
                    clip = _cardLandCommonClip;
                    scale = 0.85f;
                    break;
            }

            PlaySfxInternal(clip, scale);
        }

        public void PlayWinFanfare()
        {
            if (!Application.isPlaying) return;
            EnsureInitialized();
            PlaySfxInternal(_winFanfareClip, 0.95f);
        }

        public void PlayLoseTone()
        {
            if (!Application.isPlaying) return;
            EnsureInitialized();
            PlaySfxInternal(_loseToneClip, 0.88f);
        }

        public void PlayTablaRetry()
        {
            if (!Application.isPlaying) return;
            EnsureInitialized();
            PlaySfxInternal(_tablaRetryClip, 0.92f);
        }

        public void PlayCoinClink()
        {
            if (!Application.isPlaying) return;
            EnsureInitialized();
            PlaySfxInternal(_coinClinkClip, 0.90f);
        }

        public void PlayPackOpen()
        {
            if (!Application.isPlaying) return;
            EnsureInitialized();
            PlaySfxInternal(_packOpenClip, 0.95f);
        }

        public void PlayHiddenCardFound()
        {
            if (!Application.isPlaying) return;
            EnsureInitialized();
            PlaySfxInternal(_hiddenCardFoundClip, 0.98f);
        }

        public void PlayUiClick()
        {
            if (!Application.isPlaying) return;
            EnsureInitialized();

            float now = Time.unscaledTime;
            if (now - _lastUiClickTime < UiClickThrottleSeconds)
            {
                return;
            }

            _lastUiClickTime = now;
            PlaySfxInternal(_uiClickClip, 0.72f);
        }

        // --- Internal Audio Source & Settings Sync ---

        private AudioSource Create2DSource(bool loop, int priority)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            src.dopplerLevel = 0f;
            src.priority = priority;
            return src;
        }

        private void HandleInventoryAudioSettingsChanged(AudioSettingsData settings)
        {
            if (_syncingSettings || settings == null) return;
            _syncingSettings = true;
            try
            {
                ApplySettingsInternal(settings);
            }
            finally
            {
                _syncingSettings = false;
            }
        }

        private void ApplySettingsInternal(AudioSettingsData settings)
        {
            if (settings == null) return;
            _masterVolume = Mathf.Clamp01(settings.MasterVolume);
            _bgmVolume = Mathf.Clamp01(settings.BgmVolume);
            _sfxVolume = Mathf.Clamp01(settings.SfxVolume);
            _isMuted = settings.IsMuted;
            _isFullscreen = settings.IsFullscreen;
            ApplySourceVolumes();
        }

        private void ApplySourceVolumes()
        {
            float effectiveSfx = _isMuted ? 0f : Mathf.Clamp01(_masterVolume * _sfxVolume);
            float effectiveBgm = _isMuted ? 0f : Mathf.Clamp01(_masterVolume * _bgmVolume);

            if (_sfxSource != null)
            {
                _sfxSource.mute = _isMuted;
                _sfxSource.volume = effectiveSfx;
            }

            if (_bgmSource != null)
            {
                _bgmSource.mute = _isMuted;
                _bgmSource.volume = effectiveBgm * BgmMixGain;
            }
        }

        private void SyncSettingsToPersistence()
        {
            if (!Application.isPlaying || _syncingSettings) return;

            var snapshot = GetCurrentAudioSettings();
            var targetInventory = _boundInventory != null ? _boundInventory : PlayerInventory.Instance;

            _syncingSettings = true;
            try
            {
                if (targetInventory != null)
                {
                    if (targetInventory.AudioSettings != null)
                    {
                        snapshot.IsFullscreen = targetInventory.AudioSettings.IsFullscreen;
                        _isFullscreen = snapshot.IsFullscreen;
                    }
                    targetInventory.UpdateAudioSettings(snapshot);
                }
                else
                {
                    TeksSaveSystem.SaveAudioSettingsToPrefs(snapshot);
                }
            }
            finally
            {
                _syncingSettings = false;
            }
        }

        private void PlaySfxInternal(AudioClip clip, float volumeScale)
        {
            if (_sfxSource == null || clip == null || _isMuted) return;
            float effectiveSfx = Mathf.Clamp01(_masterVolume * _sfxVolume);
            if (effectiveSfx <= 0.0005f) return;

            _sfxSource.volume = effectiveSfx;
            _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        private static void DestroyClip(ref AudioClip clip)
        {
            if (clip == null) return;
            if (Application.isPlaying)
            {
                Destroy(clip);
            }
            else
            {
                DestroyImmediate(clip);
            }
            clip = null;
        }

        // --- Procedural PCM Synthesis (Deterministic System.Random only) ---

        private void BakeAllProceduralClips()
        {
            var rng = new System.Random(20260521);

            _cardFlickClip = BakeCardFlickClip(rng);
            _cardLandCommonClip = BakeCardLandCommonClip(rng);
            _cardLandRareClip = BakeCardLandRareClip(rng);
            _cardLandEpicClip = BakeCardLandEpicClip(rng);
            _cardLandLegendaryClip = BakeCardLandLegendaryClip(rng);
            _winFanfareClip = BakeWinFanfareClip();
            _loseToneClip = BakeLoseToneClip();
            _tablaRetryClip = BakeTablaRetryClip(rng);
            _coinClinkClip = BakeCoinClinkClip();
            _packOpenClip = BakePackOpenClip(rng);
            _hiddenCardFoundClip = BakeHiddenCardFoundClip();
            _uiClickClip = BakeUiClickClip(rng);
            _bgmClip = BakeBarangayRondallaBgmClip(rng);
        }

        private static AudioClip CreateClipFromSamples(string clipName, float[] samples, int sampleRate)
        {
            NormalizeAndSoftLimit(samples, 0.90f);
            var clip = AudioClip.Create(clipName, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip BakeCardFlickClip(System.Random rng)
        {
            int total = Mathf.RoundToInt(SfxSampleRate * 0.16f);
            var samples = new float[total];
            float lpState = 0f;
            float prevNoise = 0f;
            float phase = 0f;

            for (int i = 0; i < total; i++)
            {
                float t = (float)i / SfxSampleRate;
                float white = NextSignedFloat(rng);

                // Band-shaped cardboard snap transient
                float hp = white - prevNoise * 0.72f;
                prevNoise = white;
                lpState += (hp - lpState) * 0.32f;

                float snapEnv = Mathf.Exp(-t * 48f) * Mathf.Clamp01(t / 0.002f);
                float whooshEnv = Mathf.Sin(Mathf.Clamp01(t / 0.14f) * Mathf.PI) * Mathf.Exp(-t * 14f);

                float sweepFreq = Mathf.Lerp(520f, 1180f, Mathf.Clamp01(t / 0.045f)) * Mathf.Exp(-t * 6f);
                phase += (2f * Mathf.PI * sweepFreq) / SfxSampleRate;

                float body = Mathf.Sin(phase) * 0.35f * Mathf.Exp(-t * 32f);
                float air = lpState * (snapEnv * 1.15f + whooshEnv * 0.55f);

                samples[i] = (body + air) * FadeEdge(i, total, 36, 240);
            }

            return CreateClipFromSamples("TeksSfx_CardFlick", samples, SfxSampleRate);
        }

        private static AudioClip BakeCardLandCommonClip(System.Random rng)
        {
            int total = Mathf.RoundToInt(SfxSampleRate * 0.14f);
            var samples = new float[total];
            AddCardboardSlap(samples, 0, rng, 0.95f);
            return CreateClipFromSamples("TeksSfx_CardLand_Common", samples, SfxSampleRate);
        }

        private static AudioClip BakeCardLandRareClip(System.Random rng)
        {
            int total = Mathf.RoundToInt(SfxSampleRate * 0.44f);
            var samples = new float[total];
            AddCardboardSlap(samples, 0, rng, 0.78f);
            AddBellTone(samples, 0.015f, 659.25f, 0.36f, 0.32f, SfxSampleRate);  // E5
            AddBellTone(samples, 0.050f, 987.77f, 0.36f, 0.28f, SfxSampleRate);  // B5
            AddBellTone(samples, 0.090f, 1318.51f, 0.34f, 0.24f, SfxSampleRate); // E6
            return CreateClipFromSamples("TeksSfx_CardLand_Rare", samples, SfxSampleRate);
        }

        private static AudioClip BakeCardLandEpicClip(System.Random rng)
        {
            int total = Mathf.RoundToInt(SfxSampleRate * 0.60f);
            var samples = new float[total];
            AddCardboardSlap(samples, 0, rng, 0.80f);
            AddBellTone(samples, 0.012f, 440.00f, 0.48f, 0.28f, SfxSampleRate);  // A4
            AddBellTone(samples, 0.055f, 554.37f, 0.48f, 0.28f, SfxSampleRate);  // C#5
            AddBellTone(samples, 0.105f, 659.25f, 0.46f, 0.30f, SfxSampleRate);  // E5
            AddBellTone(samples, 0.160f, 880.00f, 0.42f, 0.32f, SfxSampleRate);  // A5
            AddBellTone(samples, 0.215f, 1318.51f, 0.36f, 0.20f, SfxSampleRate); // E6 shimmer
            return CreateClipFromSamples("TeksSfx_CardLand_Epic", samples, SfxSampleRate);
        }

        private static AudioClip BakeCardLandLegendaryClip(System.Random rng)
        {
            int total = Mathf.RoundToInt(SfxSampleRate * 0.92f);
            var samples = new float[total];
            AddCardboardSlap(samples, 0, rng, 0.85f);

            // Warm golden sub-impact bloom
            AddWarmChordTone(samples, 0.008f, 196.00f, 0.65f, 0.30f, SfxSampleRate); // G3
            AddWarmChordTone(samples, 0.015f, 392.00f, 0.78f, 0.28f, SfxSampleRate); // G4
            AddBellTone(samples, 0.045f, 493.88f, 0.76f, 0.26f, SfxSampleRate);      // B4
            AddBellTone(samples, 0.095f, 587.33f, 0.74f, 0.28f, SfxSampleRate);      // D5
            AddBellTone(samples, 0.145f, 783.99f, 0.72f, 0.34f, SfxSampleRate);      // G5
            AddBellTone(samples, 0.205f, 987.77f, 0.66f, 0.28f, SfxSampleRate);      // B5
            AddBellTone(samples, 0.270f, 1174.66f, 0.60f, 0.26f, SfxSampleRate);     // D6
            AddBellTone(samples, 0.335f, 1567.98f, 0.54f, 0.20f, SfxSampleRate);     // G6
            return CreateClipFromSamples("TeksSfx_CardLand_Legendary", samples, SfxSampleRate);
        }

        private static AudioClip BakeWinFanfareClip()
        {
            int total = Mathf.RoundToInt(SfxSampleRate * 0.96f);
            var samples = new float[total];

            // Fiesta major fanfare: G4 -> B4 -> D5 -> G5 (with bright major chord hold)
            AddWarmChordTone(samples, 0.00f, 392.00f, 0.20f, 0.36f, SfxSampleRate); // G4
            AddWarmChordTone(samples, 0.12f, 493.88f, 0.20f, 0.38f, SfxSampleRate); // B4
            AddWarmChordTone(samples, 0.24f, 587.33f, 0.22f, 0.40f, SfxSampleRate); // D5
            AddWarmChordTone(samples, 0.38f, 783.99f, 0.54f, 0.44f, SfxSampleRate); // G5
            AddWarmChordTone(samples, 0.38f, 493.88f, 0.52f, 0.24f, SfxSampleRate); // B4 harmony
            AddWarmChordTone(samples, 0.38f, 587.33f, 0.52f, 0.24f, SfxSampleRate); // D5 harmony
            AddBellTone(samples, 0.40f, 1567.98f, 0.46f, 0.16f, SfxSampleRate);     // G6 sparkle

            return CreateClipFromSamples("TeksSfx_WinFanfare", samples, SfxSampleRate);
        }

        private static AudioClip BakeLoseToneClip()
        {
            int total = Mathf.RoundToInt(SfxSampleRate * 0.72f);
            var samples = new float[total];

            // Sympathetic warm acoustic descent: E4 -> D4 -> C4 -> A3
            AddPluckedTone(samples, 0.00f, 329.63f, 0.22f, 0.36f, SfxSampleRate, circularWrap: false);
            AddPluckedTone(samples, 0.16f, 293.66f, 0.22f, 0.34f, SfxSampleRate, circularWrap: false);
            AddPluckedTone(samples, 0.32f, 261.63f, 0.22f, 0.32f, SfxSampleRate, circularWrap: false);
            AddPluckedTone(samples, 0.46f, 220.00f, 0.25f, 0.38f, SfxSampleRate, circularWrap: false);

            return CreateClipFromSamples("TeksSfx_LoseTone", samples, SfxSampleRate);
        }

        private static AudioClip BakeTablaRetryClip(System.Random rng)
        {
            int total = Mathf.RoundToInt(SfxSampleRate * 0.42f);
            var samples = new float[total];

            // Two crisp woodblock/card taps + upbeat rising "try again" interval
            AddCardboardSlap(samples, 0, rng, 0.55f);
            AddCardboardSlap(samples, Mathf.RoundToInt(SfxSampleRate * 0.11f), rng, 0.55f);
            AddBellTone(samples, 0.02f, 587.33f, 0.18f, 0.30f, SfxSampleRate); // D5
            AddBellTone(samples, 0.13f, 783.99f, 0.26f, 0.34f, SfxSampleRate); // G5

            return CreateClipFromSamples("TeksSfx_TablaRetry", samples, SfxSampleRate);
        }

        private static AudioClip BakeCoinClinkClip()
        {
            int total = Mathf.RoundToInt(SfxSampleRate * 0.32f);
            var samples = new float[total];

            AddCoinStrike(samples, 0.000f, 1.00f, 1.00f);
            AddCoinStrike(samples, 0.072f, 1.06f, 0.72f);

            return CreateClipFromSamples("TeksSfx_CoinClink", samples, SfxSampleRate);
        }

        private static AudioClip BakePackOpenClip(System.Random rng)
        {
            int total = Mathf.RoundToInt(SfxSampleRate * 0.74f);
            var samples = new float[total];

            // Crisp foil tear (0.00s - 0.18s)
            int tearSamples = Mathf.RoundToInt(SfxSampleRate * 0.18f);
            float prev = 0f;
            for (int i = 0; i < tearSamples && i < total; i++)
            {
                float t = (float)i / SfxSampleRate;
                float n = NextSignedFloat(rng);
                float hp = n - prev * 0.84f;
                prev = n;
                float env = Mathf.Sin(Mathf.Clamp01(t / 0.18f) * Mathf.PI);
                float crinkle = 0.72f + 0.28f * Mathf.Sin(t * 210f);
                samples[i] += hp * env * crinkle * 0.45f;
            }

            // Sparkling reveal arpeggio: C5 -> E5 -> G5 -> B5 -> C6 -> E6
            float[] notes = new float[] { 523.25f, 659.25f, 783.99f, 987.77f, 1046.50f, 1318.51f };
            for (int n = 0; n < notes.Length; n++)
            {
                float start = 0.11f + n * 0.055f;
                AddBellTone(samples, start, notes[n], 0.42f, 0.26f, SfxSampleRate);
            }

            return CreateClipFromSamples("TeksSfx_PackOpen", samples, SfxSampleRate);
        }

        private static AudioClip BakeHiddenCardFoundClip()
        {
            int total = Mathf.RoundToInt(SfxSampleRate * 0.88f);
            var samples = new float[total];

            // Radiant secret discovery harp/rondalla glissando in D Major
            float[] notes = new float[] { 587.33f, 739.99f, 880.00f, 1174.66f, 1479.98f };
            for (int i = 0; i < notes.Length; i++)
            {
                float start = i * 0.068f;
                AddBellTone(samples, start, notes[i], 0.56f, 0.30f, SfxSampleRate);
            }
            AddWarmChordTone(samples, 0.24f, 587.33f, 0.58f, 0.22f, SfxSampleRate);
            AddWarmChordTone(samples, 0.24f, 880.00f, 0.58f, 0.20f, SfxSampleRate);

            return CreateClipFromSamples("TeksSfx_HiddenCardFound", samples, SfxSampleRate);
        }

        private static AudioClip BakeUiClickClip(System.Random rng)
        {
            int total = Mathf.RoundToInt(SfxSampleRate * 0.055f);
            var samples = new float[total];
            float phase = 0f;

            for (int i = 0; i < total; i++)
            {
                float t = (float)i / SfxSampleRate;
                float freq = Mathf.Lerp(960f, 540f, Mathf.Clamp01(t / 0.035f));
                phase += (2f * Mathf.PI * freq) / SfxSampleRate;
                float pop = Mathf.Sin(phase) * Mathf.Exp(-t * 78f);
                float tick = NextSignedFloat(rng) * Mathf.Exp(-t * 240f) * 0.28f;
                samples[i] = (pop * 0.75f + tick) * FadeEdge(i, total, 18, 120);
            }

            return CreateClipFromSamples("TeksSfx_UiClick", samples, SfxSampleRate);
        }

        private static AudioClip BakeBarangayRondallaBgmClip(System.Random rng)
        {
            // 14.0s exact 8-bar 3/4 Filipino rondalla / acoustic waltz in G Major
            // All note tails wrap circularly via (startIdx + k) % totalSamples for a 100% clickless loop.
            int totalSamples = Mathf.RoundToInt(BgmSampleRate * BgmDurationSeconds);
            var samples = new float[totalSamples];

            const int totalBars = 8;
            float barDur = BgmDurationSeconds / totalBars; // 1.75s per bar
            float beatDur = barDur / 3f;                   // 0.583333s per beat

            // Bass roots & guitar chord voicings (Hz) for: G, Em, C, D7, G, Bm, C, D7
            float[] bassRoots = new float[]
            {
                98.00f,  // G2
                82.41f,  // E2
                130.81f, // C3
                146.83f, // D3
                98.00f,  // G2
                123.47f, // B2
                130.81f, // C3
                146.83f  // D3
            };

            float[][] chordNotes = new float[][]
            {
                new float[] { 196.00f, 246.94f, 293.66f, 392.00f }, // G
                new float[] { 164.81f, 246.94f, 329.63f, 392.00f }, // Em
                new float[] { 196.00f, 261.63f, 329.63f, 392.00f }, // C
                new float[] { 220.00f, 261.63f, 293.66f, 369.99f }, // D7
                new float[] { 196.00f, 246.94f, 293.66f, 392.00f }, // G
                new float[] { 185.00f, 246.94f, 293.66f, 369.99f }, // Bm
                new float[] { 196.00f, 261.63f, 329.63f, 392.00f }, // C
                new float[] { 220.00f, 261.63f, 293.66f, 369.99f }  // D7
            };

            // Warm 3/4 acoustic guitar accompaniment
            for (int bar = 0; bar < totalBars; bar++)
            {
                float barStart = bar * barDur;

                // Beat 1: warm guitar bass thumb pluck
                AddPluckedTone(samples, barStart, bassRoots[bar], 1.15f, 0.25f, BgmSampleRate, circularWrap: true);

                // Beats 2 & 3: gentle arpeggiated guitar strums
                float[] chord = chordNotes[bar];
                for (int beat = 1; beat <= 2; beat++)
                {
                    float beatStart = barStart + beat * beatDur;
                    float amp = beat == 1 ? 0.115f : 0.095f;
                    for (int s = 0; s < chord.Length; s++)
                    {
                        float strumOffset = s * 0.015f;
                        AddPluckedTone(samples, beatStart + strumOffset, chord[s], 0.72f, amp, BgmSampleRate, circularWrap: true);
                    }
                }
            }

            // Nostalgic Barangay Bandurria / Rondalla melody (beatOffset in beats from 0..24, freq, durationBeats)
            float[] melBeat = new float[]
            {
                0.0f, 1.0f, 1.5f, 2.0f,
                3.0f, 4.0f, 5.0f,
                6.0f, 7.0f, 7.5f, 8.0f,
                9.0f, 10.5f, 11.0f,
                12.0f, 13.0f, 13.5f, 14.0f,
                15.0f, 16.0f, 17.0f,
                18.0f, 19.0f, 19.5f, 20.0f,
                21.0f, 22.0f, 23.0f
            };

            float[] melFreq = new float[]
            {
                392.00f, 493.88f, 587.33f, 659.25f, // Bar 0 (G): G4 B4 D5 E5
                587.33f, 493.88f, 392.00f,          // Bar 1 (Em): D5 B4 G4
                523.25f, 659.25f, 587.33f, 523.25f, // Bar 2 (C): C5 E5 D5 C5
                587.33f, 493.88f, 440.00f,          // Bar 3 (D7): D5 B4 A4
                392.00f, 493.88f, 587.33f, 783.99f, // Bar 4 (G): G4 B4 D5 G5
                739.99f, 587.33f, 493.88f,          // Bar 5 (Bm): F#5 D5 B4
                523.25f, 659.25f, 783.99f, 659.25f, // Bar 6 (C): C5 E5 G5 E5
                587.33f, 523.25f, 440.00f           // Bar 7 (D7): D5 C5 A4 -> resolves to G4!
            };

            float[] melDurBeats = new float[]
            {
                1.0f, 0.5f, 0.5f, 1.0f,
                1.0f, 1.0f, 1.0f,
                1.0f, 0.5f, 0.5f, 1.0f,
                1.5f, 0.5f, 1.0f,
                1.0f, 0.5f, 0.5f, 1.0f,
                1.0f, 1.0f, 1.0f,
                1.0f, 0.5f, 0.5f, 1.0f,
                1.0f, 1.0f, 1.0f
            };

            for (int m = 0; m < melBeat.Length; m++)
            {
                float startSec = melBeat[m] * beatDur;
                float durSec = Mathf.Max(0.28f, melDurBeats[m] * beatDur * 1.15f);
                float humanJitter = NextSignedFloat(rng) * 0.004f;
                AddBandurriaNote(samples, startSec + humanJitter, melFreq[m], durSec, 0.165f, BgmSampleRate);
            }

            // Subtle warm bamboo shaker / soft wooden pulse on beats 2 & 3
            for (int bar = 0; bar < totalBars; bar++)
            {
                float barStart = bar * barDur;
                AddSoftShaker(samples, barStart + beatDur, 0.045f, 0.025f, BgmSampleRate, rng);
                AddSoftShaker(samples, barStart + beatDur * 2f, 0.040f, 0.020f, BgmSampleRate, rng);
            }

            NormalizeAndSoftLimit(samples, 0.68f);
            var clip = AudioClip.Create("TeksBgm_BarangayRondalla", totalSamples, 1, BgmSampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        // --- Synthesis Primitives ---

        private static void AddCardboardSlap(float[] buffer, int startSample, System.Random rng, float gain)
        {
            int len = Mathf.RoundToInt(SfxSampleRate * 0.095f);
            float prev = 0f;
            float lp = 0f;
            float phase = 0f;

            for (int k = 0; k < len; k++)
            {
                int idx = startSample + k;
                if (idx < 0 || idx >= buffer.Length) continue;

                float t = (float)k / SfxSampleRate;
                float freq = Mathf.Lerp(195f, 72f, Mathf.Clamp01(t / 0.045f));
                phase += (2f * Mathf.PI * freq) / SfxSampleRate;

                float thump = Mathf.Sin(phase) * Mathf.Exp(-t * 44f) * 0.65f;
                float n = NextSignedFloat(rng);
                float hp = n - prev * 0.55f;
                prev = n;
                lp += (hp - lp) * 0.45f;
                float slap = lp * Mathf.Exp(-t * 85f) * 0.72f;

                buffer[idx] += (thump + slap) * gain * FadeEdge(k, len, 12, 160);
            }
        }

        private static void AddCoinStrike(float[] buffer, float startSec, float pitchMul, float gain)
        {
            int startIdx = Mathf.RoundToInt(startSec * SfxSampleRate);
            int len = Mathf.RoundToInt(SfxSampleRate * 0.24f);
            float[] partials = new float[] { 2480f, 3910f, 5260f, 6810f };
            float[] amps = new float[] { 0.42f, 0.30f, 0.18f, 0.10f };
            float[] decays = new float[] { 15f, 20f, 26f, 34f };

            for (int k = 0; k < len; k++)
            {
                int idx = startIdx + k;
                if (idx < 0 || idx >= buffer.Length) continue;

                float t = (float)k / SfxSampleRate;
                float attack = Mathf.Clamp01(t / 0.0018f);
                float sum = 0f;
                for (int p = 0; p < partials.Length; p++)
                {
                    float w = 2f * Mathf.PI * partials[p] * pitchMul * t;
                    sum += Mathf.Sin(w) * amps[p] * Mathf.Exp(-t * decays[p]);
                }

                buffer[idx] += sum * attack * gain * FadeEdge(k, len, 10, 220);
            }
        }

        private static void AddBellTone(float[] buffer, float startSec, float freq, float durSec, float amp, int sampleRate)
        {
            int startIdx = Mathf.RoundToInt(startSec * sampleRate);
            int len = Mathf.RoundToInt(durSec * sampleRate);
            float decay = 5.5f / Mathf.Max(0.12f, durSec);

            for (int k = 0; k < len; k++)
            {
                int idx = startIdx + k;
                if (idx < 0 || idx >= buffer.Length) continue;

                float t = (float)k / sampleRate;
                float attack = Mathf.Clamp01(t / 0.004f);
                float env = attack * Mathf.Exp(-t * decay);
                float shimmer = 1f + 0.08f * Mathf.Sin(2f * Mathf.PI * 11f * t);

                float v =
                    Mathf.Sin(2f * Mathf.PI * freq * t) * 0.62f +
                    Mathf.Sin(2f * Mathf.PI * freq * 2.0f * t) * 0.24f * Mathf.Exp(-t * decay * 0.9f) +
                    Mathf.Sin(2f * Mathf.PI * freq * 3.01f * t) * 0.14f * Mathf.Exp(-t * decay * 1.6f);

                buffer[idx] += v * env * shimmer * amp * FadeEdge(k, len, 16, 180);
            }
        }

        private static void AddWarmChordTone(float[] buffer, float startSec, float freq, float durSec, float amp, int sampleRate)
        {
            int startIdx = Mathf.RoundToInt(startSec * sampleRate);
            int len = Mathf.RoundToInt(durSec * sampleRate);

            for (int k = 0; k < len; k++)
            {
                int idx = startIdx + k;
                if (idx < 0 || idx >= buffer.Length) continue;

                float t = (float)k / sampleRate;
                float u = (float)k / Mathf.Max(1, len - 1);
                float attack = Mathf.Clamp01(t / 0.012f);
                float release = Mathf.Clamp01((1f - u) / 0.25f);
                float env = attack * release * Mathf.Exp(-t * 2.2f);

                float v =
                    Mathf.Sin(2f * Mathf.PI * freq * t) * 0.65f +
                    Mathf.Sin(2f * Mathf.PI * freq * 2.0f * t) * 0.22f +
                    Mathf.Sin(2f * Mathf.PI * freq * 3.0f * t) * 0.09f;

                buffer[idx] += v * env * amp;
            }
        }

        private static void AddPluckedTone(float[] buffer, float startSec, float freq, float durSec, float amp, int sampleRate, bool circularWrap)
        {
            int total = buffer.Length;
            int startIdx = Mathf.RoundToInt(startSec * sampleRate);
            int len = Mathf.RoundToInt(durSec * sampleRate);
            float decay = 4.6f / Mathf.Max(0.18f, durSec);

            for (int k = 0; k < len; k++)
            {
                int idx = startIdx + k;
                if (circularWrap)
                {
                    idx = ((idx % total) + total) % total;
                }
                else if (idx < 0 || idx >= total)
                {
                    continue;
                }

                float t = (float)k / sampleRate;
                float attack = Mathf.Clamp01(t / 0.005f);
                float env = attack * Mathf.Exp(-t * decay);

                float h2Decay = Mathf.Exp(-t * decay * 1.4f);
                float h3Decay = Mathf.Exp(-t * decay * 2.2f);
                float v =
                    Mathf.Sin(2f * Mathf.PI * freq * t) * 0.66f +
                    Mathf.Sin(2f * Mathf.PI * freq * 2.0f * t) * 0.24f * h2Decay +
                    Mathf.Sin(2f * Mathf.PI * freq * 3.0f * t) * 0.10f * h3Decay;

                buffer[idx] += v * env * amp * FadeEdge(k, len, 12, 160);
            }
        }

        private static void AddBandurriaNote(float[] buffer, float startSec, float freq, float durSec, float amp, int sampleRate)
        {
            int total = buffer.Length;
            int startIdx = Mathf.RoundToInt(startSec * sampleRate);
            int len = Mathf.RoundToInt(durSec * sampleRate);
            float decay = 3.8f / Mathf.Max(0.20f, durSec);

            for (int k = 0; k < len; k++)
            {
                int idx = ((startIdx + k) % total + total) % total;
                float t = (float)k / sampleRate;
                float attack = Mathf.Clamp01(t / 0.004f);
                // Gentle rondalla mandolin/bandurria tremolo shimmer on sustained tones
                float tremolo = 0.88f + 0.12f * Mathf.Sin(2f * Mathf.PI * 10.5f * t);
                float env = attack * Mathf.Exp(-t * decay) * tremolo;

                float v =
                    Mathf.Sin(2f * Mathf.PI * freq * t) * 0.58f +
                    Mathf.Sin(2f * Mathf.PI * freq * 2.0f * t) * 0.28f * Mathf.Exp(-t * 3.2f) +
                    Mathf.Sin(2f * Mathf.PI * freq * 3.0f * t) * 0.11f * Mathf.Exp(-t * 6.0f) +
                    Mathf.Sin(2f * Mathf.PI * freq * 4.0f * t) * 0.04f * Mathf.Exp(-t * 9.0f);

                buffer[idx] += v * env * amp * FadeEdge(k, len, 14, 180);
            }
        }

        private static void AddSoftShaker(float[] buffer, float startSec, float durSec, float amp, int sampleRate, System.Random rng)
        {
            int total = buffer.Length;
            int startIdx = Mathf.RoundToInt(startSec * sampleRate);
            int len = Mathf.RoundToInt(durSec * sampleRate);
            float prev = 0f;

            for (int k = 0; k < len; k++)
            {
                int idx = ((startIdx + k) % total + total) % total;
                float u = (float)k / Mathf.Max(1, len - 1);
                float env = Mathf.Sin(u * Mathf.PI);
                float n = NextSignedFloat(rng);
                float hp = n - prev * 0.80f;
                prev = n;
                buffer[idx] += hp * env * amp;
            }
        }

        private static float FadeEdge(int index, int length, int fadeInSamples, int fadeOutSamples)
        {
            float gain = 1f;
            if (fadeInSamples > 0 && index < fadeInSamples)
            {
                gain *= (float)index / fadeInSamples;
            }
            int remaining = length - 1 - index;
            if (fadeOutSamples > 0 && remaining < fadeOutSamples)
            {
                gain *= Mathf.Max(0f, (float)remaining / fadeOutSamples);
            }
            return gain;
        }

        private static float NextSignedFloat(System.Random rng)
        {
            return (float)(rng.NextDouble() * 2.0 - 1.0);
        }

        private static void NormalizeAndSoftLimit(float[] samples, float targetPeak)
        {
            float maxAbs = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float a = Mathf.Abs(samples[i]);
                if (a > maxAbs) maxAbs = a;
            }

            if (maxAbs <= 0.0001f) return;

            float scale = maxAbs > targetPeak ? targetPeak / maxAbs : 1f;
            for (int i = 0; i < samples.Length; i++)
            {
                float v = samples[i] * scale;
                // Gentle soft limiter guarantees zero digital clipping
                samples[i] = (float)(Math.Atan(v * 1.25f) / Math.Atan(1.25f)) * targetPeak;
            }
        }
    }
}
