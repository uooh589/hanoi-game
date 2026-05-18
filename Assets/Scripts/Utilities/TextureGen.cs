using UnityEngine;

namespace HanoiGame
{
    /// <summary>
    /// Generates simple runtime textures so no external sprites are required.
    /// </summary>
    public static class TextureGen
    {
        /// <summary>Circle sprite with border — good for map nodes.</summary>
        public static Sprite Circle(int size, Color fill, Color border, float borderWidth = 0.15f)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float half = size * 0.5f;
            float rOuter = half;
            float rInner = half * (1f - borderWidth);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - half + 0.5f, dy = y - half + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > rOuter) pixels[y * size + x] = Color.clear;
                    else if (dist < rInner) pixels[y * size + x] = fill;
                    else { float t = Mathf.InverseLerp(rOuter, rInner, dist); pixels[y * size + x] = Color.Lerp(border, fill, t); }
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>Rounded rectangle.</summary>
        public static Sprite RoundedRect(int w, int h, Color fill, Color border, float radius = 8)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - w / 2f) - w / 2f + radius);
                    float dy = Mathf.Max(0, Mathf.Abs(y - h / 2f) - h / 2f + radius);
                    if (dx * dx + dy * dy > radius * radius) { pixels[y * w + x] = Color.clear; continue; }
                    // Simple border check
                    bool nearEdge = x < 2 || y < 2 || x >= w - 2 || y >= h - 2;
                    pixels[y * w + x] = nearEdge ? border : fill;
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }
    }
}
