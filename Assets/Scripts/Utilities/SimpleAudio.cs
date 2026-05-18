using UnityEngine;

namespace HanoiGame
{
    /// <summary>
    /// Generates simple synthesized sound effects without external audio files.
    /// Attach to a GameObject with an AudioSource.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SimpleAudio : MonoBehaviour
    {
        public static SimpleAudio Instance { get; private set; }

        private AudioSource _source;

        // Frequencies for different events (Hz)
        private const float MOVE_FREQ = 440f;
        private const float ERROR_FREQ = 200f;
        private const float COMPLETE_FREQ = 660f;
        private const float DAMAGE_FREQ = 330f;
        private const float HEAL_FREQ = 550f;
        private const float CLICK_FREQ = 520f;

        [Range(0f, 1f)] public float volume = 0.08f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _source = GetComponent<AudioSource>();
        }

        public void PlayMove()     => PlayChime(new[] { 440f, 520f }, 0.06f);
        public void PlayError()    => PlayBuzz(180f, 0.18f);
        public void PlayComplete() => PlayChime(new[] { 523f, 659f, 784f }, 0.12f);
        public void PlayDamage()   => PlayNoise(0.08f, 0.5f);
        public void PlayHeal()     => PlayChime(new[] { 440f, 554f, 659f }, 0.1f);
        public void PlayClick()    => PlayTone(880f, 0.04f, 0.3f);
        public void PlayShield()   => PlayChime(new[] { 587f, 740f, 880f }, 0.1f);
        public void PlayDraw()     => PlayChime(new[] { 440f, 554f }, 0.05f);
        public void PlayBuff()     => PlayRising(330f, 660f, 0.15f);

        void PlayTone(float freq, float duration, float volMul = 1f)
        {
            int sr = 44100;
            int samples = Mathf.CeilToInt(sr * duration);
            var clip = AudioClip.Create("sfx", samples, 1, sr, false);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sr;
                float env = Mathf.Exp(-t * 10f);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * volume * volMul;
            }
            clip.SetData(data, 0);
            _source.PlayOneShot(clip);
        }

        void PlayChime(float[] freqs, float eachLen)
        {
            int sr = 44100;
            int eachSamples = Mathf.CeilToInt(sr * eachLen);
            int total = eachSamples * freqs.Length;
            var clip = AudioClip.Create("chime", total, 1, sr, false);
            float[] data = new float[total];
            for (int j = 0; j < freqs.Length; j++)
            {
                int off = j * eachSamples;
                for (int i = 0; i < eachSamples; i++)
                {
                    float t = (float)i / sr;
                    float env = 1f - (float)(i + off) / total;
                    data[off + i] = Mathf.Sin(2f * Mathf.PI * freqs[j] * t) * env * volume * 0.6f;
                }
            }
            clip.SetData(data, 0);
            _source.PlayOneShot(clip);
        }

        void PlayBuzz(float freq, float dur)
        {
            int sr = 44100;
            int samples = Mathf.CeilToInt(sr * dur);
            var clip = AudioClip.Create("buzz", samples, 1, sr, false);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sr;
                float env = 1f - (float)i / samples;
                // Square-like wave for harsh sound
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) > 0 ? 1f : -0.3f;
                wave += Mathf.Sin(2f * Mathf.PI * freq * 0.5f * t) * 0.3f;
                data[i] = wave * env * volume * 0.4f;
            }
            clip.SetData(data, 0);
            _source.PlayOneShot(clip);
        }

        void PlayNoise(float dur, float intensity)
        {
            int sr = 44100;
            int samples = Mathf.CeilToInt(sr * dur);
            var clip = AudioClip.Create("noise", samples, 1, sr, false);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float env = 1f - (float)i / samples;
                float noise = (Random.value * 2f - 1f) * intensity;
                noise += Mathf.Sin(2f * Mathf.PI * 110f * (float)i / sr) * 0.3f;
                data[i] = noise * env * volume;
            }
            clip.SetData(data, 0);
            _source.PlayOneShot(clip);
        }

        void PlayRising(float from, float to, float dur)
        {
            int sr = 44100;
            int samples = Mathf.CeilToInt(sr * dur);
            var clip = AudioClip.Create("rise", samples, 1, sr, false);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sr;
                float freq = Mathf.Lerp(from, to, (float)i / samples);
                float env = Mathf.Sin(Mathf.PI * (float)i / samples); // smooth fade
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * volume * 0.5f;
            }
            clip.SetData(data, 0);
            _source.PlayOneShot(clip);
        }
    }
}
