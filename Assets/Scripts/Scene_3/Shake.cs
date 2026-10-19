using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 祠堂背景 → 短暂黑屏 → 老人照片。
///
/// hallView / blackView / photoView 都可以是 UI Image 或 SpriteRenderer 所在的 GameObject。
/// blackView 建议做成全屏黑色 UI Image，并放在低于 DialogueSystem(500) 的 Canvas 上，
/// 这样黑屏不会盖住对话框。
/// </summary>
public class Shake : MonoBehaviour
{
    [Header("画面对象")]
    public GameObject hallView;
    public GameObject blackView;
    public CanvasGroup blackGroup;
    public GameObject photoView;

    [Header("时间")]
    [Tooltip("祠堂背景停留时间")]
    public float hallDuration = 2f;

    [Tooltip("黑屏停留时间")]
    public float blackDuration = 0.35f;

    [Tooltip("黑屏淡入淡出时间；没有 CanvasGroup 时直接硬切")]
    public float blackFade = 0.15f;

    [Header("照片出现时震动（可选）")]
    public bool shakeOnPhoto = true;
    public Transform shakeTarget;
    public float shakeDuration = 0.35f;
    public float shakeMagnitude = 12f;

    [Header("触发")]
    public bool playOnStart = true;

    [Header("结束")]
    public UnityEvent onFinished;
    public event Action Finished;

    public bool IsPlaying { get; private set; }

    Coroutine routine;

    void Start()
    {
        if (playOnStart) Play();
    }

    public void Play()
    {
        if (IsPlaying) return;

        if (routine != null)
            StopCoroutine(routine);

        IsPlaying = true;
        routine = StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        if (hallView != null) hallView.SetActive(true);
        if (photoView != null) photoView.SetActive(false);

        if (blackView != null)
        {
            blackView.SetActive(false);
            if (blackGroup != null) blackGroup.alpha = 0f;
        }

        if (hallDuration > 0f)
            yield return new WaitForSecondsRealtime(hallDuration);

        // 全屏黑幕
        if (blackView != null)
        {
            blackView.SetActive(true);

            if (blackGroup != null)
                yield return FadeGroup(blackGroup, 0f, 1f, blackFade);
        }

        if (blackDuration > 0f)
            yield return new WaitForSecondsRealtime(blackDuration);

        // 黑屏期间换照片
        if (photoView != null) photoView.SetActive(true);
        if (hallView != null) hallView.SetActive(false);

        if (blackView != null)
        {
            if (blackGroup != null)
                yield return FadeGroup(blackGroup, 1f, 0f, blackFade);

            blackView.SetActive(false);
        }

        if (shakeOnPhoto && shakeTarget != null)
            yield return ShakeRoutine();

        IsPlaying = false;
        routine = null;

        if (Finished != null) Finished();
        if (onFinished != null) onFinished.Invoke();
    }

    IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
    {
        group.alpha = from;

        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }

        group.alpha = to;
    }

    IEnumerator ShakeRoutine()
    {
        Vector3 original = shakeTarget.localPosition;
        float t = 0f;

        while (t < shakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float strength = 1f - Mathf.Clamp01(t / shakeDuration);
            Vector2 offset = UnityEngine.Random.insideUnitCircle * shakeMagnitude * strength;
            shakeTarget.localPosition = original + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        shakeTarget.localPosition = original;
    }

    void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        IsPlaying = false;
    }
}
