using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace HanoiGame {
    public static class CreatePanelSettings {
        [MenuItem("Tools/Create Genshin PanelSettings")]
        public static void Create() {
            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.name = "GenshinPanelSettings";
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0.5f;
            ps.referenceDpi = 96;
            ps.fallbackDpi = 96;
            AssetDatabase.CreateAsset(ps, "Assets/Resources/GenshinPanelSettings.asset");
            AssetDatabase.SaveAssets();
            Debug.Log("[GenshinUI] PanelSettings created at Resources/GenshinPanelSettings.asset");
        }
    }
}
