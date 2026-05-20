using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace HanoiGame.GenshinUI
{
    /// <summary>Animation extensions for UI Toolkit — Unity 2022.3 compatible (no StyleValues).</summary>
    public static class GenshinTween
    {
        public static void DoScale(this VisualElement ve, float from, float to, float duration, Action onDone = null)
        {
            ve.style.scale = new StyleScale(new Scale(new Vector3(from, from, 1f)));
            ve.StartCoroutine(CoScale(ve, from, to, duration, onDone));
        }

        static IEnumerator CoScale(VisualElement ve, float from, float to, float dur, Action done)
        {
            float t = 0f;
            while (t < dur) { t += Time.deltaTime; float s = Mathf.Lerp(from, to, t / dur); ve.style.scale = new Scale(new Vector3(s, s, 1f)); yield return null; }
            done?.Invoke();
        }

        public static void DoFade(this VisualElement ve, float from, float to, float duration, Action onDone = null)
        {
            ve.StartCoroutine(CoFade(ve, from, to, duration, onDone));
        }

        static IEnumerator CoFade(VisualElement ve, float from, float to, float dur, Action done)
        {
            float t = 0f;
            while (t < dur) { t += Time.deltaTime; ve.style.opacity = Mathf.Lerp(from, to, t / dur); yield return null; }
            done?.Invoke();
        }

        public static void DoMoveY(this VisualElement ve, float fromY, float toY, float duration, Action onDone = null)
        {
            ve.StartCoroutine(CoMoveY(ve, fromY, toY, duration, onDone));
        }

        static IEnumerator CoMoveY(VisualElement ve, float fromY, float toY, float dur, Action done)
        {
            float t = 0f;
            while (t < dur) { t += Time.deltaTime; ve.style.translate = new Translate(0, Mathf.Lerp(fromY, toY, t / dur), 0); yield return null; }
            done?.Invoke();
        }

        public static void DoPunchScale(this VisualElement ve, float punch = 0.05f, float duration = 0.2f)
        {
            ve.StartCoroutine(CoPunch(ve, punch, duration));
        }

        static IEnumerator CoPunch(VisualElement ve, float punch, float dur)
        {
            float orig = 1f;
            float t = 0f; float half = dur * 0.5f;
            while (t < half) { t += Time.deltaTime; float s = Mathf.Lerp(orig, orig - punch, t / half); ve.style.scale = new Scale(new Vector3(s, s, 1f)); yield return null; }
            t = 0f;
            while (t < half) { t += Time.deltaTime; float s = Mathf.Lerp(orig - punch, orig + punch, t / half); ve.style.scale = new Scale(new Vector3(s, s, 1f)); yield return null; }
            t = 0f;
            while (t < half) { t += Time.deltaTime; float s = Mathf.Lerp(orig + punch, orig, t / half); ve.style.scale = new Scale(new Vector3(s, s, 1f)); yield return null; }
        }

        // Helper to start coroutine on a panel (uses MonoBehaviour proxy)
        private static MonoBehaviour _runner;
        public static void SetRunner(MonoBehaviour mb) { _runner = mb; }

        static void StartCoroutine(this VisualElement ve, IEnumerator routine)
        {
            if (_runner != null) _runner.StartCoroutine(routine);
            else Debug.LogWarning("[GenshinTween] No MonoBehaviour runner set. Call GenshinTween.SetRunner(mb) first.");
        }
    }
}
