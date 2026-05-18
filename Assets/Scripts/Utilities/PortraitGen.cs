using UnityEngine;
using UnityEngine.UI;

namespace HanoiGame
{
    public static class PortraitGen
    {
        static Color RegionColor(string region) => region switch
        {
            "蒙德" => new Color(0.25f, 0.45f, 0.3f),
            "璃月" => new Color(0.7f, 0.55f, 0.2f),
            "稻妻" => new Color(0.5f, 0.2f, 0.6f),
            "须弥" => new Color(0.2f, 0.5f, 0.3f),
            "枫丹" => new Color(0.2f, 0.4f, 0.7f),
            "纳塔" => new Color(0.8f, 0.3f, 0.15f),
            "层岩" => new Color(0.55f, 0.45f, 0.25f),
            _ => new Color(0.3f, 0.3f, 0.4f),
        };

        static Sprite LoadSpriteFromResources(string path, int w, int h)
        {
            var tex = Resources.Load<Texture2D>(path);
            if (tex != null)
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            return null;
        }

        public static Sprite Generate(string enemyName, string region, int w, int h)
        {
            string safeName = enemyName.Replace("·", "_").Replace(" ", "_");
            var sprite = LoadSpriteFromResources($"portraits/{safeName}", w, h);
            if (sprite != null) return sprite;
            return GenerateProcedural(enemyName, region, w, h);
        }

        static Sprite GenerateProcedural(string enemyName, string region, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color bg = RegionColor(region);
            var px = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float t = (float)y / h;
                Color row = Color.Lerp(bg * 0.6f, bg * 1.2f, t);
                for (int x = 0; x < w; x++)
                    px[y * w + x] = row;
            }

            int border = 2;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (x < border || x >= w - border || y < border || y >= h - border)
                        px[y * w + x] = new Color(1f, 0.84f, 0f, 0.7f);

            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        public static Sprite BattleBg(string region, int w, int h)
        {
            var sprite = LoadSpriteFromResources($"bg_{region}", w, h);
            if (sprite != null) return sprite;
            return BattleBgProcedural(w, h);
        }

        public static Sprite BattleBg(int w, int h) => BattleBgProcedural(w, h);

        static Sprite BattleBgProcedural(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color top = new Color(0.05f, 0.08f, 0.2f);
            Color bot = new Color(0.02f, 0.03f, 0.08f);

            for (int y = 0; y < h; y++)
            {
                float t = (float)y / h;
                Color row = Color.Lerp(top, bot, t);
                for (int x = 0; x < w; x++)
                    px[y * w + x] = row;
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }
    }
}
