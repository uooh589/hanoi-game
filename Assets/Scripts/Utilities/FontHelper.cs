using UnityEngine;

namespace HanoiGame
{
    public static class FontHelper
    {
        private static Font _cached;

        public static Font GetFont()
        {
            if (_cached != null) return _cached;

            _cached = Resources.Load<Font>("NotoSansSC");
            if (_cached != null) return _cached;

            _cached = Font.CreateDynamicFontFromOSFont("Noto Sans CJK SC", 14);
            if (_cached != null) return _cached;

            _cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _cached;
        }

        public static Font GetFont(int size)
        {
            var f = GetFont();
            return f;
        }
    }
}
