using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_6_OR_NEWER
using UnityEngine.AddressableAssets;
#endif

namespace HanoiGame.GenshinUI
{
    /// <summary>Resource loading utilities for sprites, fonts, and stylesheets.</summary>
    public static class GenshinResources
    {
        private static readonly Dictionary<string, Object> _cache = new();

        /// <summary>Load a Sprite from Resources synchronously (cached).</summary>
        public static Sprite LoadSprite(string path)
        {
            if (_cache.TryGetValue(path, out var obj) && obj is Sprite s) return s;
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) { Debug.LogWarning($"[GenshinUI] Sprite not found: {path}"); return null; }
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
            _cache[path] = sprite;
            return sprite;
        }

        /// <summary>Async load sprite from Resources.</summary>
        public static async void LoadSpriteAsync(string path, System.Action<Sprite> onLoaded)
        {
            var req = Resources.LoadAsync<Texture2D>(path);
            await System.Threading.Tasks.Task.Run(() => { while (!req.isDone) { } });
            var tex = req.asset as Texture2D;
            if (tex == null) { Debug.LogWarning($"[GenshinUI] Async sprite not found: {path}"); return; }
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
            _cache[path] = sprite;
            onLoaded?.Invoke(sprite);
        }

#if UNITY_6_OR_NEWER
        public static async void LoadSpriteAddressable(string key, System.Action<Sprite> onLoaded)
        {
            var handle = Addressables.LoadAssetAsync<Texture2D>(key);
            await handle.Task;
            if (handle.Result == null) { Debug.LogWarning($"[GenshinUI] Addressable not found: {key}"); return; }
            var sprite = Sprite.Create(handle.Result, new Rect(0, 0, handle.Result.width, handle.Result.height), Vector2.one * 0.5f);
            _cache[key] = sprite;
            onLoaded?.Invoke(sprite);
        }
#endif

        /// <summary>Get or load a StyleSheet from Resources.</summary>
        public static StyleSheet LoadUSS(string path)
        {
            if (_cache.TryGetValue(path, out var obj) && obj is StyleSheet ss) return ss;
            var loaded = Resources.Load<StyleSheet>(path);
            if (loaded != null) _cache[path] = loaded;
            return loaded;
        }

        public static void ClearCache() => _cache.Clear();
    }
}
