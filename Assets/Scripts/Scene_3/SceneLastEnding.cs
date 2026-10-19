using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// scene_last 的最后演出：标题图淡入，随后 GAME OVER 淡入。
/// 标题图直接复用场景里的 BackGround Image，不额外创建 UI。
/// </summary>
public class SceneLastEnding : MonoBehaviour
{
    [Header("标题")]
    public Graphic titleImage;
    public float titleFade = 1.6f;

    [Header("GAME OVER")]
    public TMP_Text gameOverText;
    public float gameOverDelay = 0.8f;
    public float gameOverFade = 0.8f;

    [Header("场景")]
    public string bgmName = "增加恐怖氛围可用";

    bool playing;

    void Start()
    {
        if (titleImage != null)
        {
            Color color = titleImage.color;
            color.a = 0f;
            titleImage.color = color;
        }

        if (gameOverText != null)
        {
            Color color = gameOverText.color;
            color.a = 0f;
            gameOverText.color = color;
        }
    }

    public void Play()
    {
        if (playing) return;
        playing = true;
        StartCoroutine(Routine());
    }

    IEnumerator Routine()
    {
        AudioManager.Instance.PlayMusic(bgmName);

        if (titleImage != null)
        {
            yield return FadeGraphic(titleImage, 0f, 1f, titleFade);
        }

        if (gameOverDelay > 0f)
            yield return new WaitForSecondsRealtime(gameOverDelay);

        if (gameOverText != null)
            yield return FadeGraphic(gameOverText, 0f, 1f, gameOverFade);
    }

    static IEnumerator FadeGraphic(Graphic graphic, float from, float to, float duration)
    {
        if (graphic == null) yield break;

        Color color = graphic.color;
        color.a = from;
        graphic.color = color;

        if (duration <= 0f)
        {
            color.a = to;
            graphic.color = color;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            graphic.color = color;
            yield return null;
        }

        color.a = to;
        graphic.color = color;
    }
}
