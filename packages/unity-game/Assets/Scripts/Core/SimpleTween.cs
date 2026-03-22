using System;
using System.Collections;
using UnityEngine;

namespace RiceFactory.Core
{
    /// <summary>
    /// DOTween yerine kullanilan hafif tween utility.
    /// Coroutine tabanli — MonoBehaviour gerektirmeden TweenRunner uzerinden calisir.
    /// </summary>
    public static class SimpleTween
    {
        // ---------------------------------------------------------------
        // Ease Fonksiyonlari
        // ---------------------------------------------------------------

        public enum Ease
        {
            Linear,
            OutQuad,
            InQuad,
            InOutQuad,
            OutBack,
            InOutSine
        }

        public static float EaseLinear(float t) => t;

        public static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);

        public static float EaseInQuad(float t) => t * t;

        public static float EaseInOutQuad(float t) =>
            t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        public static float EaseInOutSine(float t) =>
            -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;

        public static float Evaluate(Ease ease, float t)
        {
            switch (ease)
            {
                case Ease.Linear:     return EaseLinear(t);
                case Ease.OutQuad:    return EaseOutQuad(t);
                case Ease.InQuad:     return EaseInQuad(t);
                case Ease.InOutQuad:  return EaseInOutQuad(t);
                case Ease.OutBack:    return EaseOutBack(t);
                case Ease.InOutSine:  return EaseInOutSine(t);
                default:              return EaseLinear(t);
            }
        }

        // ---------------------------------------------------------------
        // TweenRunner — Coroutine host
        // ---------------------------------------------------------------

        private static TweenRunner _runner;

        private static TweenRunner Runner
        {
            get
            {
                if (_runner == null)
                {
                    var go = new GameObject("[SimpleTween.Runner]");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    _runner = go.AddComponent<TweenRunner>();
                }
                return _runner;
            }
        }

        // ---------------------------------------------------------------
        // DOFade — CanvasGroup alpha animasyonu
        // ---------------------------------------------------------------

        /// <summary>
        /// CanvasGroup alpha degerini hedef degere animasyonla getirir.
        /// </summary>
        /// <returns>Baslatilan Coroutine (StopCoroutine ile durdurulabilir).</returns>
        public static Coroutine DOFade(CanvasGroup canvasGroup, float targetAlpha, float duration,
            Ease ease = Ease.Linear, Action onComplete = null)
        {
            if (canvasGroup == null) { onComplete?.Invoke(); return null; }
            return Runner.StartCoroutine(FadeRoutine(canvasGroup, targetAlpha, duration, ease, onComplete));
        }

        private static IEnumerator FadeRoutine(CanvasGroup cg, float target, float duration,
            Ease ease, Action onComplete)
        {
            float start = cg.alpha;
            float elapsed = 0f;

            if (duration <= 0f)
            {
                cg.alpha = target;
                onComplete?.Invoke();
                yield break;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cg.alpha = Mathf.LerpUnclamped(start, target, Evaluate(ease, t));
                yield return null;
            }

            cg.alpha = target;
            onComplete?.Invoke();
        }

        // ---------------------------------------------------------------
        // DOScale — Transform scale animasyonu (float overload)
        // ---------------------------------------------------------------

        /// <summary>
        /// Transform localScale degerini uniform olarak hedef degere animasyonla getirir.
        /// </summary>
        public static Coroutine DOScale(Transform target, float targetScale, float duration,
            Ease ease = Ease.Linear, Action onComplete = null)
        {
            return DOScale(target, Vector3.one * targetScale, duration, ease, onComplete);
        }

        // ---------------------------------------------------------------
        // DOScale — Transform scale animasyonu (Vector3 overload)
        // ---------------------------------------------------------------

        /// <summary>
        /// Transform localScale degerini hedef Vector3 degerine animasyonla getirir.
        /// </summary>
        public static Coroutine DOScale(Transform target, Vector3 targetScale, float duration,
            Ease ease = Ease.Linear, Action onComplete = null)
        {
            if (target == null) { onComplete?.Invoke(); return null; }
            return Runner.StartCoroutine(ScaleRoutine(target, targetScale, duration, ease, onComplete));
        }

        private static IEnumerator ScaleRoutine(Transform target, Vector3 targetScale, float duration,
            Ease ease, Action onComplete)
        {
            Vector3 start = target.localScale;
            float elapsed = 0f;

            if (duration <= 0f)
            {
                target.localScale = targetScale;
                onComplete?.Invoke();
                yield break;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float e = Evaluate(ease, t);
                target.localScale = Vector3.LerpUnclamped(start, targetScale, e);
                yield return null;
            }

            target.localScale = targetScale;
            onComplete?.Invoke();
        }

        // ---------------------------------------------------------------
        // DOFloat — Generic float animasyonu
        // ---------------------------------------------------------------

        /// <summary>
        /// Float degerini from'dan to'ya animasyonla getirir. Her frame setter cagirilir.
        /// </summary>
        public static Coroutine DOFloat(Action<float> setter, float from, float to, float duration,
            Ease ease = Ease.Linear, Action onComplete = null)
        {
            if (setter == null) { onComplete?.Invoke(); return null; }
            return Runner.StartCoroutine(FloatRoutine(setter, from, to, duration, ease, onComplete));
        }

        private static IEnumerator FloatRoutine(Action<float> setter, float from, float to, float duration,
            Ease ease, Action onComplete)
        {
            float elapsed = 0f;

            if (duration <= 0f)
            {
                setter(to);
                onComplete?.Invoke();
                yield break;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                setter(Mathf.LerpUnclamped(from, to, Evaluate(ease, t)));
                yield return null;
            }

            setter(to);
            onComplete?.Invoke();
        }

        // ---------------------------------------------------------------
        // DelayedCall — Gecikmeli cagri
        // ---------------------------------------------------------------

        /// <summary>
        /// Belirtilen sure sonra callback'i cagir.
        /// </summary>
        public static Coroutine DelayedCall(float delay, Action callback)
        {
            if (callback == null) return null;
            return Runner.StartCoroutine(DelayedCallRoutine(delay, callback));
        }

        private static IEnumerator DelayedCallRoutine(float delay, Action callback)
        {
            yield return new WaitForSeconds(delay);
            callback?.Invoke();
        }

        // ---------------------------------------------------------------
        // Kill / Stop
        // ---------------------------------------------------------------

        /// <summary>
        /// Belirli bir Coroutine'i durdurur (Kill yerine).
        /// </summary>
        public static void Kill(Coroutine coroutine)
        {
            if (coroutine != null && _runner != null)
            {
                _runner.StopCoroutine(coroutine);
            }
        }

        /// <summary>
        /// Tum aktif tween'leri durdurur.
        /// </summary>
        public static void KillAll()
        {
            if (_runner != null)
            {
                _runner.StopAllCoroutines();
            }
        }
    }

    /// <summary>
    /// SimpleTween icin Coroutine host. Otomatik olusturulur, dokunmayin.
    /// </summary>
    public class TweenRunner : MonoBehaviour { }
}
