using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BookFlipController : MonoBehaviour
{
    [Header("Animator with a FlipRight trigger")]
    public Animator animator;

    [Header("Timing")]
    [Tooltip("Approx clip length used for fallback timing and optional crease animation.")]
    public float fallbackFlipDuration = 0.35f;

    [Header("Crease Shadow (UI Image down the middle)")]
    public Image creaseShadow;
    [Range(0f, 1f)] public float creaseMinAlpha = 0.06f;
    [Range(0f, 1f)] public float creaseMaxAlpha = 0.35f;
    public float creaseMinWidth = 20f;
    public float creaseMaxWidth = 80f;

    [Header("Fallback crease animation behavior")]
    [Tooltip("If true and no animation events are used, we animate the crease 0->1->0 during fallback.")]
    public bool useFallbackCreaseAnimation = true;

    [Tooltip("Also run the fallback crease animation even when an Animator is present (useful if you did not add events).")]
    public bool useFallbackCreaseWhenAnimatorPresent = false;

    // state
    bool midCalled;
    bool endCalled;
    Action _midAction;

    void OnEnable()
    {
        // Ensure crease starts in the "flat page" state.
        UpdateCrease(0f);
    }

    /// <summary>
    /// Plays the flip animation. At the midpoint, we call midAction
    /// so you can swap to the next question *while* the page is turning.
    /// </summary>
    public IEnumerator FlipToNext(Action midAction)
    {
        _midAction = midAction;
        midCalled = false;
        endCalled = false;

        // If we have an Animator, trigger it.
        if (animator)
        {
            animator.ResetTrigger("FlipRight");
            animator.SetTrigger("FlipRight");

            // Optionally animate the crease via fallback (if you didn't wire events).
            if (useFallbackCreaseWhenAnimatorPresent && useFallbackCreaseAnimation && creaseShadow)
            {
                // Run a symmetric crease animation and call midAction at halfway (guarded by midCalled flag).
                yield return StartCoroutine(AnimateCreaseSymmetric(fallbackFlipDuration, _midAction));
            }
            else
            {
                // Guarantee the mid callback even if no events are wired.
                yield return new WaitForSeconds(fallbackFlipDuration);
                if (!midCalled && _midAction != null) { _midAction.Invoke(); midCalled = true; }
                yield return new WaitForSeconds(0.05f);
            }

            if (!endCalled) endCalled = true;
            // Return crease to flat
            UpdateCrease(0f);
        }
        else
        {
            // No Animator? Perform a clean, symmetric crease animation and call midAction at halfway.
            yield return StartCoroutine(AnimateCreaseSymmetric(fallbackFlipDuration, _midAction));
        }
    }

    // ===== Animation Events (optional but nicest timing) =====
    // Add these as events on your flip clip.
    // Place "HandleFlipMid" at the visual middle of the flip.
    // Place "HandleFlipEnd" at the end of the flip.
    public void HandleFlipMid()
    {
        // When the page is vertical, crease looks strongest.
        UpdateCrease(1f);

        if (!midCalled && _midAction != null)
        {
            _midAction.Invoke();
            midCalled = true;
        }
    }

    public void HandleFlipEnd()
    {
        // Page lays flat again.
        UpdateCrease(0f);
        endCalled = true;
    }

    // Optional: drive crease via a float event (0..1) from the clip
    // so you can match the curve precisely in the Animation window.
    public void HandleFlipProgress(float t)
    {
        UpdateCrease(Mathf.Clamp01(t));
    }

    // ===== Crease helpers =====

    /// <summary>
    /// t = 0 -> flat page (thin, faint). t = 1 -> mid flip (wide, dark).
    /// </summary>
    public void UpdateCrease(float t)
    {
        if (!creaseShadow) return;

        // Alpha
        var c = creaseShadow.color;
        c.a = Mathf.Lerp(creaseMinAlpha, creaseMaxAlpha, t);
        creaseShadow.color = c;

        // Width
        var rt = creaseShadow.rectTransform;
        float targetW = Mathf.Lerp(creaseMinWidth, creaseMaxWidth, t);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetW);
    }

    IEnumerator AnimateCreaseSymmetric(float duration, Action midAction)
    {
        if (!creaseShadow || !useFallbackCreaseAnimation)
        {
            // Just do timing and mid-callback if we have no crease sprite.
            float half = Mathf.Max(0.01f, duration * 0.5f);
            yield return new WaitForSeconds(half);
            if (!midCalled && midAction != null) { midAction.Invoke(); midCalled = true; }
            yield return new WaitForSeconds(half);
            endCalled = true;
            yield break;
        }

        float halfDur = Mathf.Max(0.01f, duration * 0.5f);
        float t;

        // 0 -> 1 (page rising)
        t = 0f;
        while (t < halfDur)
        {
            float p = t / halfDur;
            UpdateCrease(p);
            t += Time.deltaTime;
            yield return null;
        }
        UpdateCrease(1f);

        // Midpoint callback
        if (!midCalled && midAction != null) { midAction.Invoke(); midCalled = true; }

        // 1 -> 0 (page settling)
        t = 0f;
        while (t < halfDur)
        {
            float p = 1f - (t / halfDur);
            UpdateCrease(p);
            t += Time.deltaTime;
            yield return null;
        }
        UpdateCrease(0f);

        endCalled = true;
    }
}
