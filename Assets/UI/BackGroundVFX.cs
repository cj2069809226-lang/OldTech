using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>换图那一瞬间的故障特效。没挂故障 shader 也能跑，会自动用闪烁兜底</summary>
[RequireComponent(typeof(Image))]
public class BackGroundVFX : MonoBehaviour
{
    [SerializeField] private float duration = 0.4f;      // 特效总时长
    [SerializeField] private float peakRatio = 0.4f;      // 最强时刻（换图就发生在这里）
    [SerializeField] private string amountProperty = "_GlitchAmount";

    private Image _image;
    private Material _mat;              // 有故障 shader 时用
    private Color _baseColor = Color.white;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _baseColor = _image.color;
        if (_image.material != null && _image.material.HasProperty(amountProperty))
            _mat = _image.material;     // 拿到的是材质实例，可以放心改
    }

    /// <summary>播一次特效，并在"最强那一帧"回调 onPeak（外面就在那里换图）</summary>
    public IEnumerator Play(Action onPeak = null)
    {
        float t = 0f;
        bool peaked = false;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            SetAmount(Mathf.Sin(k * Mathf.PI));            // 强度 0 → 1 → 0

            if (!peaked && k >= peakRatio) { peaked = true; onPeak?.Invoke(); }
            yield return null;
        }

        ResetToIdle();
    }

    public void ResetToIdle()
    {
        SetAmount(0f);
        if (_image != null) _image.color = _baseColor;
    }

    private void SetAmount(float amount)
    {
        if (_mat != null) { _mat.SetFloat(amountProperty, amount); return; }

        // 兜底：没有 shader 时用随机压暗模拟信号不稳
        float f = UnityEngine.Random.value < amount * 0.7f ? 0.35f : 1f;
        _image.color = new Color(f, f, f, 1f);
    }
}