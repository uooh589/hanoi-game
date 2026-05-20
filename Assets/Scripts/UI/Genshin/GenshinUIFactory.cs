using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace HanoiGame.GenshinUI
{
    /// <summary>Genshin Impact-inspired UI factory. All elements created via pure C#, no UXML.</summary>
    public static class GenshinUIFactory
    {
        // ── Color palette ──
        public static readonly Color WarmWhite = new(0.976f, 0.965f, 0.937f);
        public static readonly Color LightTeal = new(0.91f, 0.94f, 0.95f);
        public static readonly Color Gold = new(0.898f, 0.761f, 0.557f);
        public static readonly Color DreamPurple = new(0.722f, 0.663f, 0.91f);
        public static readonly Color WaterBlue = new(0.557f, 0.784f, 0.816f);
        public static readonly Color DarkBg = new(0.08f, 0.06f, 0.10f);
        public static readonly Color GlassOverlay = new(1f, 1f, 1f, 0.06f);

        /// <summary>Create a Genshin-style button with hover glow, scale pulse, shimmer.</summary>
        public static Button CreateButton(string text, Action onClick, string sizeClass = "")
        {
            var btn = new Button { text = text };
            btn.AddToClassList("genshin-button");
            if (!string.IsNullOrEmpty(sizeClass)) btn.AddToClassList(sizeClass);
            btn.clicked += onClick;

            // Shimmer overlay
            var shimmer = new VisualElement();
            shimmer.AddToClassList("genshin-shimmer");
            shimmer.pickingMode = PickingMode.Ignore;
            btn.Add(shimmer);

            // Hover: lift + glow
            btn.RegisterCallback<PointerEnterEvent>(_ =>
            {
                btn.style.translate = new Translate(0, -2, 0);
                btn.style.backgroundColor = new StyleColor(Gold * 0.3f);
            });
            btn.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                btn.style.translate = new Translate(0, 0, 0);
                btn.style.backgroundColor = StyleKeyword.Null;
            });

            // Press: elastic scale
            btn.RegisterCallback<PointerDownEvent>(_ => btn.DoPunchScale(0.05f, 0.15f));

            return btn;
        }

        /// <summary>Glass-morphism panel with title, close button, open/close animation.</summary>
        public static VisualElement CreatePanel(string title, VisualElement content)
        {
            var panel = new VisualElement();
            panel.AddToClassList("genshin-panel");

            // Corner decorations
            var corners = new VisualElement();
            corners.AddToClassList("genshin-panel-corners");
            for (int i = 0; i < 4; i++)
            {
                var diamond = new VisualElement();
                diamond.AddToClassList($"genshin-corner-{i}");
                corners.Add(diamond);
            }
            panel.Add(corners);

            // Glass background
            var glass = new VisualElement();
            glass.AddToClassList("genshin-glass");
            glass.pickingMode = PickingMode.Ignore;
            panel.Add(glass);

            // Title bar
            var titleBar = new VisualElement();
            titleBar.AddToClassList("genshin-panel-titlebar");
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("genshin-glow-text");
            titleBar.Add(titleLabel);

            var closeBtn = new Button { text = "✕" };
            closeBtn.AddToClassList("genshin-close-btn");
            closeBtn.clicked += () => panel.DoFade(1f, 0f, 0.2f, () => panel.style.display = DisplayStyle.None);
            titleBar.Add(closeBtn);
            panel.Add(titleBar);

            // Content
            content.AddToClassList("genshin-panel-content");
            panel.Add(content);

            // Open animation
            panel.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                panel.style.opacity = 0;
                panel.DoFade(0, 1, 0.3f);
                panel.style.translate = new Translate(0, 30, 0);
                panel.DoMoveY(30, 0, 0.35f);
            }, TrickleDown.NoTrickleDown);

            return panel;
        }

        /// <summary>Hex-segment health bar with smooth fill and low-HP pulse.</summary>
        public static VisualElement CreateHealthBar(float current, float max)
        {
            var container = new VisualElement();
            container.AddToClassList("genshin-healthbar");
            container.style.flexDirection = FlexDirection.Row;

            int segments = 8; // hex segments
            float pct = Mathf.Clamp01(current / max);
            for (int i = 0; i < segments; i++)
            {
                var seg = new VisualElement();
                seg.AddToClassList("genshin-healthbar-segment");
                float segStart = (float)i / segments;
                float segEnd = (float)(i + 1) / segments;
                if (pct >= segEnd) seg.AddToClassList("genshin-healthbar-full");
                else if (pct > segStart) seg.AddToClassList("genshin-healthbar-partial");
                else seg.AddToClassList("genshin-healthbar-empty");

                container.Add(seg);
            }

            // Low HP pulse (<25%)
            if (pct < 0.25f)
            {
                container.schedule.Execute(() =>
                {
                    container.style.opacity = container.style.opacity.value < 0.6f ? 1f : 0.6f;
                }).Every(400);
            }

            return container;
        }

        /// <summary>Rhombus icon frame for skills/avatars with active glow.</summary>
        public static VisualElement CreateIconFrame(Sprite icon, bool isActive)
        {
            var frame = new VisualElement();
            frame.AddToClassList("genshin-icon-frame");
            if (isActive) frame.AddToClassList("genshin-icon-frame-active");

            var iconEl = new VisualElement();
            iconEl.style.backgroundImage = new StyleBackground(icon);
            iconEl.AddToClassList("genshin-icon-inner");

            var glow = new VisualElement();
            glow.AddToClassList("genshin-icon-glow");
            glow.pickingMode = PickingMode.Ignore;

            frame.Add(glow);
            frame.Add(iconEl);
            return frame;
        }

        /// <summary>Title text with outer glow effect.</summary>
        public static Label CreateGlowingText(string text, string sizeClass = "genshin-title-lg")
        {
            var label = new Label(text);
            label.AddToClassList("genshin-glow-text");
            label.AddToClassList(sizeClass);
            return label;
        }

        /// <summary>Responsive flex row.</summary>
        public static VisualElement FlexRow(params VisualElement[] children)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            foreach (var c in children) row.Add(c);
            return row;
        }
    }
}
