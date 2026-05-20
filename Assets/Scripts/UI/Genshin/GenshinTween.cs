using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace HanoiGame.GenshinUI
{
    /// <summary>Native animation extensions for UI Toolkit — zero GC pressure, no third-party deps.</summary>
    public static class GenshinTween
    {
        public static void DoScale(this VisualElement ve, float from, float to, float duration, Action onDone = null)
        {
            ve.style.scale = new StyleScale(new Scale(new Vector3(from, from, 1f)));
            ve.schedule.Execute(() => { }).StartingIn(16);
            ve.experimental.animation.Start(new StyleValues { scale = new Scale(new Vector3(from, from, 1f)) },
                new StyleValues { scale = new Scale(new Vector3(to, to, 1f)) }, (int)(duration * 1000));
            ve.schedule.Execute(() => onDone?.Invoke()).StartingIn((long)(duration * 1000));
        }

        public static void DoFade(this VisualElement ve, float from, float to, float duration, Action onDone = null)
        {
            ve.style.opacity = from;
            ve.experimental.animation.Start(new StyleValues { opacity = from },
                new StyleValues { opacity = to }, (int)(duration * 1000));
            ve.schedule.Execute(() => onDone?.Invoke()).StartingIn((long)(duration * 1000));
        }

        public static void DoMoveY(this VisualElement ve, float fromY, float toY, float duration, Action onDone = null)
        {
            ve.style.translate = new StyleTranslate(new Translate(0, fromY, 0));
            ve.experimental.animation.Start(
                new StyleValues { translate = new Translate(0, fromY, 0) },
                new StyleValues { translate = new Translate(0, toY, 0) }, (int)(duration * 1000));
            ve.schedule.Execute(() => onDone?.Invoke()).StartingIn((long)(duration * 1000));
        }

        public static void DoPunchScale(this VisualElement ve, float punch = 0.05f, float duration = 0.2f)
        {
            float orig = ve.resolvedStyle.scale.x;
            ve.experimental.animation.Start(new StyleValues { scale = new Scale(new Vector3(orig, orig, 1f)) },
                new StyleValues { scale = new Scale(new Vector3(orig - punch, orig - punch, 1f)) }, (int)(duration * 500));
            ve.schedule.Execute(() =>
            {
                ve.experimental.animation.Start(new StyleValues { scale = new Scale(new Vector3(orig - punch, orig - punch, 1f)) },
                    new StyleValues { scale = new Scale(new Vector3(orig + punch, orig + punch, 1f)) }, (int)(duration * 500));
            }).StartingIn((long)(duration * 500));
            ve.schedule.Execute(() =>
            {
                ve.experimental.animation.Start(new StyleValues { scale = new Scale(new Vector3(orig + punch, orig + punch, 1f)) },
                    new StyleValues { scale = new Scale(new Vector3(orig, orig, 1f)) }, (int)(duration * 500));
            }).StartingIn((long)(duration * 1000));
        }

        // ── Coroutine helpers for MonoBehaviour contexts ──
        public static IEnumerator CoScale(MonoBehaviour mb, VisualElement ve, float from, float to, float dur, Action done = null)
        {
            float t = 0f; while (t < dur) { t += Time.deltaTime; float s = Mathf.Lerp(from, to, t / dur); ve.style.scale = new Scale(new Vector3(s, s, 1f)); yield return null; }
            done?.Invoke();
        }

        public static IEnumerator CoFade(MonoBehaviour mb, VisualElement ve, float from, float to, float dur, Action done = null)
        {
            float t = 0f; while (t < dur) { t += Time.deltaTime; ve.style.opacity = Mathf.Lerp(from, to, t / dur); yield return null; }
            done?.Invoke();
        }
    }
}
