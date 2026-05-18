using System.Collections.Generic;
using UnityEngine;

namespace HanoiGame
{
    [RequireComponent(typeof(AudioSource))]
    public class BGMPlayer : MonoBehaviour
    {
        public static BGMPlayer Instance { get; private set; }

        public enum Theme { MainMenu, Mondstadt, Liyue, Inazuma, Sumeru, Fontaine, Natlan, Battle, Boss }

        private AudioSource _src;
        private Theme _current;
        private static readonly Dictionary<Theme, string> ClipMap = new()
        {
            { Theme.MainMenu,  "Audio/bgm_mainmenu" },
            { Theme.Mondstadt, "Audio/bgm_mondstadt" },
            { Theme.Liyue,     "Audio/bgm_liyue" },
            { Theme.Inazuma,   "Audio/bgm_inazuma" },
            { Theme.Sumeru,    "Audio/bgm_sumeru" },
            { Theme.Fontaine,  "Audio/bgm_fontaine" },
            { Theme.Natlan,    "Audio/bgm_natlan" },
            { Theme.Battle,    "Audio/bgm_mondstadt" }, // fallback
            { Theme.Boss,      "Audio/bgm_boss_mondstadt" },
        };

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _src = GetComponent<AudioSource>();
            _src.loop = true;
            _src.spatialBlend = 0f;
        }

        public void Play(Theme theme)
        {
            if (_current == theme && _src != null && _src.isPlaying) return;
            _current = theme;

            if (_src == null) _src = GetComponent<AudioSource>();

            string path = ClipMap.GetValueOrDefault(theme, "Audio/bgm_mondstadt");
            var clip = Resources.Load<AudioClip>(path);
            if (clip != null)
            {
                _src.clip = clip;
                _src.volume = 0.3f;
                _src.Play();
            }
        }

        public void PlayRegion(string region)
        {
            var t = region switch
            {
                "蒙德" => Theme.Mondstadt,
                "璃月" => Theme.Liyue,
                "稻妻" => Theme.Inazuma,
                "须弥" => Theme.Sumeru,
                "枫丹" => Theme.Fontaine,
                "纳塔" => Theme.Natlan,
                _ => Theme.Mondstadt,
            };
            Play(t);
        }

        /// <summary>Play boss theme for the given region.</summary>
        public void PlayBoss(string region)
        {
            string path = region switch
            {
                "蒙德" => "Audio/bgm_boss_mondstadt",
                "璃月" => "Audio/bgm_boss_liyue",
                "稻妻" => "Audio/bgm_boss_inazuma",
                "须弥" => "Audio/bgm_boss_sumeru",
                "枫丹" => "Audio/bgm_boss_fontaine",
                _ => "Audio/bgm_boss_mondstadt",
            };
            var clip = Resources.Load<AudioClip>(path);
            if (clip != null && _src != null) { _src.clip = clip; _src.volume = 0.3f; _src.Play(); }
        }

        void PlayClip(string path)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip != null)
            {
                _src.clip = clip;
                _src.volume = 0.3f;
                _src.Play();
            }
        }

        public void Stop() { _src?.Stop(); }
    }
}
